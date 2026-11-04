namespace Shorthand.UmbracoCodeFirst.Metadata;

public enum CodeFirstDefinitionKind {
    DataType,
    DocumentType,
    MediaType,
    MemberType,
}

public sealed record CodeFirstPropertyDefinition(
    string Alias,
    string Name,
    string DataTypeAlias,
    string? Description = null,
    string? GroupAlias = null,
    string? GroupName = null,
    int GroupSortOrder = 0,
    string? TabAlias = null,
    string? TabName = null,
    int TabSortOrder = 0,
    int SortOrder = 0,
    bool Mandatory = false,
    string? ValidationExpression = null,
    string? ValidationMessage = null,
    bool LabelOnTop = false);

public sealed record CodeFirstGroupDefinition(
    string Alias,
    string Name,
    int SortOrder);

public sealed record CodeFirstTypeSettings(
    string? Description = null,
    string? Icon = null,
    bool IsElement = false,
    bool AllowAsRoot = false,
    bool AllowInLibrary = false,
    string? DefaultTemplateAlias = null,
    IReadOnlyList<string>? AllowedTemplateAliases = null,
    IReadOnlyList<string>? AllowedChildAliases = null,
    string? Variations = null,
    bool IsListView = false);

public sealed record CodeFirstDefinition(
    CodeFirstDefinitionKind Kind,
    string Alias,
    string Name,
    string? EditorAlias,
    IReadOnlyList<CodeFirstPropertyDefinition> Properties,
    CodeFirstTypeSettings? Settings = null,
    IReadOnlyList<CodeFirstGroupDefinition>? Groups = null,
    string? ConfigurationJson = null,
    string? EditorUiAlias = null,
    IReadOnlyList<string>? CompositionAliases = null,
    string? FolderPath = null) {
    public IReadOnlyList<CodeFirstGroupDefinition> EffectiveGroups =>
        Groups ?? Properties
            .Where(property => property.GroupAlias is not null)
            .GroupBy(property => property.GroupAlias!, StringComparer.Ordinal)
            .Select(group => new CodeFirstGroupDefinition(
                group.Key,
                group.First().GroupName ?? group.Key,
                group.First().GroupSortOrder))
            .OrderBy(group => group.SortOrder)
            .ThenBy(group => group.Alias, StringComparer.Ordinal)
            .ToArray();

    public IReadOnlyList<CodeFirstGroupDefinition> EffectiveTabs =>
        Properties
            .Where(property => property.TabAlias is not null)
            .GroupBy(property => property.TabAlias!, StringComparer.Ordinal)
            .Select(tab => new CodeFirstGroupDefinition(
                tab.Key,
                tab.First().TabName ?? tab.Key,
                tab.First().TabSortOrder))
            .OrderBy(tab => tab.SortOrder)
            .ThenBy(tab => tab.Alias, StringComparer.Ordinal)
            .ToArray();
}
