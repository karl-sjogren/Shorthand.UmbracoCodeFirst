using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;

namespace Shorthand.UmbracoCodeFirst.Backoffice;

public sealed record CodeFirstStatusResponse(
    int DefinitionCount,
    int CreatedCount,
    int UpdatedCount,
    int UnchangedCount,
    int FailedCount,
    DateTimeOffset CompletedAt,
    IReadOnlyList<CodeFirstStatusItem> Definitions);

public sealed record CodeFirstStatusItem(
    string Kind,
    string Alias,
    string Name,
    int PropertyCount,
    string Status,
    string? Message);

public static class CodeFirstStatusMapping {
    public static CodeFirstStatusResponse Map(
        ICodeFirstDefinitionRegistry registry,
        ISynchronizationReportStore reportStore) {
        SynchronizationReport report = reportStore.Current;
        Dictionary<(CodeFirstDefinitionKind Kind, string Alias), SynchronizationItem> results =
            report.Items.ToDictionary(item => (item.Kind, item.Alias));

        IReadOnlyList<CodeFirstStatusItem> definitions = registry.Definitions
            .OrderBy(definition => definition.Kind)
            .ThenBy(definition => definition.Alias)
            .Select(definition => {
                results.TryGetValue((definition.Kind, definition.Alias), out SynchronizationItem? result);
                return new CodeFirstStatusItem(
                    definition.Kind.ToString(),
                    definition.Alias,
                    definition.Name,
                    definition.Properties.Count,
                    result?.Status.ToString() ?? SynchronizationStatus.Skipped.ToString(),
                    result?.Message);
            })
            .ToArray();

        return new CodeFirstStatusResponse(
            definitions.Count,
            report.CreatedCount,
            report.UpdatedCount,
            report.UnchangedCount,
            report.FailedCount,
            report.CompletedAt,
            definitions);
    }
}
