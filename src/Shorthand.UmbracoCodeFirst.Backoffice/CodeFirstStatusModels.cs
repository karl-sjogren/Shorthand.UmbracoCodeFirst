using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;

namespace Shorthand.UmbracoCodeFirst.Backoffice;

public sealed record CodeFirstStatusResponse(
    int DefinitionCount,
    int DataTypeCount,
    int ContentTypeCount,
    int CreatedCount,
    int UpdatedCount,
    int UnchangedCount,
    int FailedCount,
    DateTimeOffset CompletedAt,
    IReadOnlyList<CodeFirstStatusItem> DataTypes,
    IReadOnlyList<CodeFirstStatusItem> ContentTypes) {
    public IReadOnlyList<CodeFirstStatusItem> Definitions =>
        DataTypes.Concat(ContentTypes).ToArray();
}

public sealed record CodeFirstStatusItem(
    string Kind,
    string Alias,
    string Name,
    string? EditorAlias,
    string? EditorUiAlias,
    int PropertyCount,
    string Status,
    string? Message,
    string? BackofficeUrl,
    IReadOnlyList<CodeFirstStatusProperty> Properties);

public sealed record CodeFirstStatusProperty(
    string Alias,
    string Name,
    string DataTypeAlias,
    string? Description,
    string? GroupAlias,
    string? GroupName,
    string? TabAlias,
    string? TabName,
    int SortOrder,
    bool Mandatory,
    string? ValidationExpression,
    string? ValidationMessage,
    bool LabelOnTop);

public static class CodeFirstStatusMapping {
    public static CodeFirstStatusResponse Map(
        ICodeFirstDefinitionRegistry registry,
        ISynchronizationReportStore reportStore,
        IReadOnlyDictionary<(CodeFirstDefinitionKind Kind, string Alias), string>? backofficeUrls = null) {
        var report = reportStore.Current;
        var results =
            report.Items.ToDictionary(item => (item.Kind, item.Alias));

        IReadOnlyList<CodeFirstStatusItem> definitions = registry.Definitions
            .OrderBy(definition => definition.Kind)
            .ThenBy(definition => definition.Alias)
            .Select(definition => {
                results.TryGetValue((definition.Kind, definition.Alias), out var result);
                return new CodeFirstStatusItem(
                    definition.Kind.ToString(),
                    definition.Alias,
                    definition.Name,
                    definition.EditorAlias,
                    definition.EditorUiAlias,
                    definition.Properties.Count,
                    result?.Status.ToString() ?? SynchronizationStatus.Skipped.ToString(),
                    result?.Message,
                    backofficeUrls?.GetValueOrDefault((definition.Kind, definition.Alias)),
                    definition.Properties
                        .OrderBy(property => property.SortOrder)
                        .ThenBy(property => property.Alias)
                        .Select(property => new CodeFirstStatusProperty(
                            property.Alias,
                            property.Name,
                            property.DataTypeAlias,
                            property.Description,
                            property.GroupAlias,
                            property.GroupName,
                            property.TabAlias,
                            property.TabName,
                            property.SortOrder,
                            property.Mandatory,
                            property.ValidationExpression,
                            property.ValidationMessage,
                            property.LabelOnTop))
                        .ToArray());
            })
            .ToArray();

        return new CodeFirstStatusResponse(
            definitions.Count,
            definitions.Count(definition => definition.Kind == nameof(CodeFirstDefinitionKind.DataType)),
            definitions.Count(definition => definition.Kind == nameof(CodeFirstDefinitionKind.DocumentType)),
            report.CreatedCount,
            report.UpdatedCount,
            report.UnchangedCount,
            report.FailedCount,
            report.CompletedAt,
            definitions.Where(definition => definition.Kind == nameof(CodeFirstDefinitionKind.DataType)).ToArray(),
            definitions.Where(definition => definition.Kind == nameof(CodeFirstDefinitionKind.DocumentType)).ToArray());
    }
}
