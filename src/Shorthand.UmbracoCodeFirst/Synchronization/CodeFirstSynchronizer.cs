using Microsoft.Extensions.Logging;
using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Synchronization;

public interface ICodeFirstDefinitionStore {
    Task<SynchronizationStatus> ApplyAsync(CodeFirstDefinition definition, CancellationToken cancellationToken);
}

public sealed class CodeFirstSynchronizer(
    ICodeFirstDefinitionRegistry registry,
    ICodeFirstDefinitionStore store,
    ISynchronizationReportStore reportStore,
    TimeProvider timeProvider,
    ILogger<CodeFirstSynchronizer> logger) {
    public async Task<SynchronizationReport> SynchronizeAsync(CancellationToken cancellationToken = default) {
        List<SynchronizationItem> items = [];

        IEnumerable<CodeFirstDefinition> definitions = OrderDefinitions(registry.Definitions);

        foreach(var definition in definitions) {
            try {
                var status = await SynchronizeDefinitionAsync(definition, cancellationToken);
                items.Add(new(definition.Kind, definition.Alias, definition.Name, status));
            } catch(Exception exception) {
                logger.LogError(exception, "Failed to synchronize code-first definition {Alias}", definition.Alias);
                items.Add(new(definition.Kind, definition.Alias, definition.Name, SynchronizationStatus.Failed, exception.Message));
            }
        }

        SynchronizationReport report = new(timeProvider.GetUtcNow(), items);
        reportStore.Set(report);
        return report;
    }

    private static IReadOnlyList<CodeFirstDefinition> OrderDefinitions(
        IReadOnlyList<CodeFirstDefinition> definitions) {
        List<CodeFirstDefinition> ordered = definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.DataType)
            .OrderBy(definition => definition.Alias, StringComparer.Ordinal)
            .ToList();
        Dictionary<string, CodeFirstDefinition> contentTypes = definitions
            .Where(definition => definition.Kind != CodeFirstDefinitionKind.DataType)
            .ToDictionary(definition => definition.Alias, StringComparer.Ordinal);
        HashSet<string> visiting = new(StringComparer.Ordinal);
        HashSet<string> visited = new(StringComparer.Ordinal);

        foreach(var definition in contentTypes.Values
                    .OrderBy(definition => definition.Alias, StringComparer.Ordinal)) {
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

            foreach(string compositionAlias in definition.CompositionAliases ?? []) {
                if(contentTypes.TryGetValue(compositionAlias, out var composition)) {
                    Visit(composition);
                }
            }

            visiting.Remove(definition.Alias);
            visited.Add(definition.Alias);
            ordered.Add(definition);
        }
    }

    private async Task<SynchronizationStatus> SynchronizeDefinitionAsync(
        CodeFirstDefinition definition,
        CancellationToken cancellationToken) =>
        await store.ApplyAsync(definition, cancellationToken);
}
