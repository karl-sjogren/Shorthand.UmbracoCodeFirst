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

        IEnumerable<CodeFirstDefinition> definitions = registry.Definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.DataType)
            .OrderBy(definition => definition.Alias, StringComparer.Ordinal)
            .Concat(registry.Definitions
                .Where(definition => definition.Kind != CodeFirstDefinitionKind.DataType)
                .OrderBy(definition => definition.Kind)
                .ThenBy(definition => definition.Alias, StringComparer.Ordinal));

        foreach(CodeFirstDefinition definition in definitions) {
            try {
                SynchronizationStatus status = await SynchronizeDefinitionAsync(definition, cancellationToken);
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

    private async Task<SynchronizationStatus> SynchronizeDefinitionAsync(
        CodeFirstDefinition definition,
        CancellationToken cancellationToken) =>
        await store.ApplyAsync(definition, cancellationToken);
}
