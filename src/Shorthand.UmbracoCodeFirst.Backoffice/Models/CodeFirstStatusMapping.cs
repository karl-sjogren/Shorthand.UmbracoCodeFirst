using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Models;

internal static class CodeFirstStatusMapping {
    public static CodeFirstStatusResponse Map(
            ICodeFirstDefinitionRegistry registry,
            ISynchronizationReportStore reportStore,
            IReadOnlyDictionary<(CodeFirstDefinitionKind Kind, string Alias),
            string>? backofficeUrls = null) {
        var report = reportStore.Current;
        var results = report.Items.ToDictionary(item => (item.Kind, item.Alias));

        IReadOnlyList<CodeFirstStatusItem> definitions = registry.Definitions
            .OrderBy(definition => definition.Kind)
            .ThenBy(definition => definition.Alias)
            .Select(definition => {
                results.TryGetValue((definition.Kind, definition.Alias), out var result);
                return new CodeFirstStatusItem(
                    definition.Kind.ToString(),
                    definition.Alias,
                    definition.Name,
                    string.IsNullOrWhiteSpace(definition.FolderPath) ? "/" : definition.FolderPath,
                    definition.Settings?.Description,
                    definition.Settings?.Icon,
                    definition.Settings?.AllowAsRoot == true,
                    definition.Settings?.AllowInLibrary == true,
                    definition.Settings?.DefaultTemplateAlias,
                    definition.Settings?.AllowedTemplateAliases ?? [],
                    definition.Settings?.AllowedChildAliases ?? [],
                    definition.Settings?.Variations,
                    definition.Settings?.IsListView == true,
                    definition.CompositionAliases ?? [],
                    definition.EffectiveGroups
                        .Select(group => new CodeFirstStatusLayoutItem(group.Alias, group.Name, group.SortOrder))
                        .ToArray(),
                    definition.EffectiveTabs
                        .Select(tab => new CodeFirstStatusLayoutItem(tab.Alias, tab.Name, tab.SortOrder))
                        .ToArray(),
                    definition.EditorAlias,
                    definition.EditorUiAlias,
                    definition.Settings?.IsElement == true,
                    definition.Properties.Count,
                    result?.Status.ToString() ?? nameof(SynchronizationStatus.Skipped),
                    result is null
                        ? report.CompletedAt == DateTimeOffset.MinValue
                            ? "No synchronization report is available yet."
                            : "No synchronization result is available for this definition."
                        : result.Message,
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
            definitions.Count(definition => definition.Kind == nameof(CodeFirstDefinitionKind.DocumentType) && definition.IsElement == false),
            definitions.Count(definition => definition.Kind == nameof(CodeFirstDefinitionKind.DocumentType) && definition.IsElement),
            definitions.Count(definition => definition.Kind == nameof(CodeFirstDefinitionKind.MediaType)),
            definitions.Count(definition => definition.Kind == nameof(CodeFirstDefinitionKind.MemberType)),
            report.CreatedCount,
            report.UpdatedCount,
            report.UnchangedCount,
            definitions.Count(definition => definition.Status == nameof(SynchronizationStatus.Skipped)),
            report.FailedCount,
            report.CompletedAt,
            definitions.Where(definition => definition.Kind == nameof(CodeFirstDefinitionKind.DataType)).ToArray(),
            definitions.Where(definition => definition.Kind == nameof(CodeFirstDefinitionKind.DocumentType) && definition.IsElement == false).ToArray(),
            definitions.Where(definition => definition.Kind == nameof(CodeFirstDefinitionKind.DocumentType) && definition.IsElement).ToArray(),
            definitions.Where(definition => definition.Kind == nameof(CodeFirstDefinitionKind.MediaType)).ToArray(),
            definitions.Where(definition => definition.Kind == nameof(CodeFirstDefinitionKind.MemberType)).ToArray());
    }
}
