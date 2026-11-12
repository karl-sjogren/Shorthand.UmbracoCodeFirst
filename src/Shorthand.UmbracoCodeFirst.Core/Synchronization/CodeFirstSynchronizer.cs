using Microsoft.Extensions.Logging;
using System.Text.Json;
using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Synchronization;

public sealed class CodeFirstSynchronizer(
        ICodeFirstDefinitionRegistry registry,
        ICodeFirstDefinitionStore store,
        ISynchronizationReportStore reportStore,
        TimeProvider timeProvider,
        ILogger<CodeFirstSynchronizer> logger) : ICodeFirstSynchronizer {
    public async Task<SynchronizationReport> SynchronizeAsync(CancellationToken cancellationToken = default) {
        List<SynchronizationItem> items = [];
        List<(CodeFirstDefinition Definition, int Index)> deferred = [];

        var duplicates = registry.Definitions
            .Where(definition => definition.Kind != CodeFirstDefinitionKind.DataType)
            .GroupBy(definition => definition.Alias, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);
        var duplicateDataTypes = registry.Definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.DataType)
            .GroupBy(definition => definition.Alias, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);
        foreach(var definition in registry.Definitions.Where(definition =>
            definition.Kind != CodeFirstDefinitionKind.DataType && duplicates.Contains(definition.Alias))) {
            items.Add(new(definition.Kind, definition.Alias, definition.Name, SynchronizationStatus.Failed,
                $"Multiple code-first definitions have alias '{definition.Alias}'."));
        }

        foreach(var definition in registry.Definitions.Where(definition =>
            definition.Kind == CodeFirstDefinitionKind.DataType && duplicateDataTypes.Contains(definition.Alias))) {
            items.Add(new(definition.Kind, definition.Alias, definition.Name, SynchronizationStatus.Failed,
                $"Multiple code-first data types have alias '{definition.Alias}'. Give each data type a unique name or set its Alias explicitly."));
        }

        IEnumerable<CodeFirstDefinition> definitions = OrderDefinitions(registry.Definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.DataType
                ? !duplicateDataTypes.Contains(definition.Alias)
                : !duplicates.Contains(definition.Alias))
            .ToArray());

        foreach(var definition in definitions) {
            try {
                var blockConfiguration = definition.EditorConfiguration;
                var hasTypedBlocks = definition.Kind == CodeFirstDefinitionKind.DataType
                    && (blockConfiguration?.BlockTypes is not null || blockConfiguration?.BlockGroups is not null
                        || blockConfiguration?.TypedSettings is not null);
                if(hasTypedBlocks) {
                    ValidateBlockReferences(definition, registry.Definitions);
                    if(store is not ICodeFirstBlockDefinitionStore blockStore) {
                        throw new InvalidOperationException("The definition store does not support typed block configuration.");
                    }

                    if(!CodeFirstBlockReferences.GetReferences(blockConfiguration!).Any()) {
                        var directResult = await SynchronizeDefinitionAsync(definition, cancellationToken);
                        items.Add(ToItem(definition, directResult));
                        continue;
                    }

                    var prepared = await blockStore.PrepareBlockDataTypeAsync(definition, cancellationToken);
                    items.Add(ToItem(definition, prepared));
                    if(prepared.Status is not SynchronizationStatus.Skipped and not SynchronizationStatus.Failed) {
                        deferred.Add((definition, items.Count - 1));
                    }

                    continue;
                }

                var result = await SynchronizeDefinitionAsync(definition, cancellationToken);
                items.Add(ToItem(definition, result));
            } catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested) {
                throw;
            } catch(Exception exception) {
                logger.LogError(exception, "Failed to synchronize code-first definition {Alias}", definition.Alias);
                items.Add(new(definition.Kind, definition.Alias, definition.Name, SynchronizationStatus.Failed, exception.Message));
            }
        }

        foreach(var (definition, index) in deferred) {
            try {
                foreach(var reference in CodeFirstBlockReferences.GetReferences(definition.EditorConfiguration!)) {
                    var alias = CodeFirstBlockReferences.GetAlias(reference);
                    var dependency = items.Single(item =>
                        item.Kind == CodeFirstDefinitionKind.DocumentType && item.Alias == alias);
                    if(dependency.Status is SynchronizationStatus.Failed or SynchronizationStatus.Skipped) {
                        throw new InvalidOperationException(
                            $"Block element '{alias}' for data type '{definition.Alias}' was not synchronized ({dependency.Status}).");
                    }
                }

                var result = await SynchronizeDefinitionAsync(definition, cancellationToken);
                if(items[index].Status == SynchronizationStatus.Created
                    && result.Status is SynchronizationStatus.Updated or SynchronizationStatus.Unchanged) {
                    result = result with { Status = SynchronizationStatus.Created };
                }

                if(result.Status is SynchronizationStatus.Created or SynchronizationStatus.Updated or SynchronizationStatus.Unchanged
                    && result.Message is null) {
                    result = result with { Message = items[index].Message };
                }

                items[index] = ToItem(definition, result);
            } catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested) {
                throw;
            } catch(Exception exception) {
                logger.LogError(exception, "Failed to synchronize code-first definition {Alias}", definition.Alias);
                items[index] = new(definition.Kind, definition.Alias, definition.Name, SynchronizationStatus.Failed, exception.Message);
            }
        }

        SynchronizationReport report = new(timeProvider.GetUtcNow(), items);
        reportStore.Set(report);
        return report;
    }

    private static void ValidateBlockReferences(
        CodeFirstDefinition definition,
        IReadOnlyList<CodeFirstDefinition> definitions) {
        var configuration = definition.EditorConfiguration!;
        foreach(var json in new[] { configuration.Json, definition.ConfigurationJson }.Where(value => !string.IsNullOrWhiteSpace(value))) {
            using var document = JsonDocument.Parse(json!);
            if(configuration.BlockTypes is not null && document.RootElement.TryGetProperty("blocks", out _)) {
                throw new InvalidOperationException($"Data type '{definition.Alias}' specifies both typed blocks and raw blocks JSON.");
            }

            if(configuration.BlockGroups is not null && document.RootElement.TryGetProperty("blockGroups", out _)) {
                throw new InvalidOperationException($"Data type '{definition.Alias}' specifies both typed groups and raw block groups JSON.");
            }

            foreach(var setting in configuration.TypedSettings ?? new Dictionary<string, JsonElement>()) {
                if(document.RootElement.TryGetProperty(setting.Key, out _)) {
                    throw new InvalidOperationException($"Data type '{definition.Alias}' specifies both typed and raw '{setting.Key}' configuration.");
                }
            }
        }

        foreach(var reference in CodeFirstBlockReferences.GetReferences(configuration)) {
            var alias = CodeFirstBlockReferences.GetAlias(reference);
            var candidates = definitions.Where(candidate =>
                candidate.Kind == CodeFirstDefinitionKind.DocumentType && candidate.Alias == alias).ToArray();
            if(candidates.Length != 1 || candidates[0].Settings?.IsElement != true) {
                throw new InvalidOperationException(
                    $"Block reference '{alias}' for data type '{definition.Alias}' must identify one code-first element type.");
            }
        }
    }

    private static IReadOnlyList<CodeFirstDefinition> OrderDefinitions(IReadOnlyList<CodeFirstDefinition> definitions) {
        var ordered = definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.DataType)
            .OrderBy(definition => definition.Alias, StringComparer.Ordinal)
            .ToList();

        var documentTypes = definitions
            .Where(definition => definition.Kind != CodeFirstDefinitionKind.DataType)
            .ToDictionary(definition => definition.Alias, StringComparer.Ordinal);

        HashSet<string> visiting = new(StringComparer.Ordinal);
        HashSet<string> visited = new(StringComparer.Ordinal);

        foreach(var definition in documentTypes.Values.OrderBy(definition => definition.Alias, StringComparer.Ordinal)) {
            Visit(definition);
        }

        return ordered;

        void Visit(CodeFirstDefinition definition) {
            if(visited.Contains(definition.Alias)) {
                return;
            }

            if(!visiting.Add(definition.Alias)) {
                return;
            }

            foreach(var compositionAlias in definition.CompositionAliases ?? []) {
                if(documentTypes.TryGetValue(compositionAlias, out var composition)) {
                    Visit(composition);
                }
            }

            foreach(var childAlias in definition.Settings?.AllowedChildAliases ?? []) {
                if(documentTypes.TryGetValue(childAlias, out var child)) {
                    Visit(child);
                }
            }

            visiting.Remove(definition.Alias);
            visited.Add(definition.Alias);
            ordered.Add(definition);
        }
    }

    private static SynchronizationItem ToItem(CodeFirstDefinition definition, SynchronizationResult result) =>
        new(definition.Kind, definition.Alias, definition.Name, result.Status,
            result.Status == SynchronizationStatus.Skipped && string.IsNullOrWhiteSpace(result.Message)
                ? "The definition store skipped this definition without providing a reason."
                : result.Message);

    private async Task<SynchronizationResult> SynchronizeDefinitionAsync(
            CodeFirstDefinition definition,
            CancellationToken cancellationToken) {
        return await store.ApplyAsync(definition, cancellationToken);
    }
}
