namespace Shorthand.UmbracoCodeFirst.Metadata;

public enum CodeFirstDefinitionKind {
    DataType,
    DocumentType,
    MediaType,
    MemberType,
    Template,
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
    [property: StringSyntax(StringSyntaxAttribute.Regex)]
    [param: StringSyntax(StringSyntaxAttribute.Regex)]
    string? ValidationExpression = null,
    string? ValidationMessage = null,
    bool LabelOnTop = false,
    string? DataTypeKey = null) {
    public string? EffectiveGroupAlias => GroupAlias is null
        ? TabAlias
        : TabAlias is null ? GroupAlias : $"{TabAlias}/{GroupAlias}";
}

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
    CodeFirstCollectionSettings? Collection = null);

public sealed record CodeFirstDefinition(
    CodeFirstDefinitionKind Kind,
    string Alias,
    string Name,
    string? EditorAlias,
    IReadOnlyList<CodeFirstPropertyDefinition> Properties,
    CodeFirstTypeSettings? Settings = null,
    IReadOnlyList<CodeFirstGroupDefinition>? Groups = null,
    [property: StringSyntax(StringSyntaxAttribute.Json)]
    [param: StringSyntax(StringSyntaxAttribute.Json)]
    string? ConfigurationJson = null,
    string? EditorUiAlias = null,
    IReadOnlyList<string>? CompositionAliases = null,
    string? FolderPath = null,
    CodeFirstEditorConfiguration? EditorConfiguration = null,
    string? TemplateContentHash = null) {
    public string? EffectiveConfigurationJson =>
        EditorConfiguration?.Json ?? ConfigurationJson;

    public IReadOnlyList<CodeFirstGroupDefinition> EffectiveGroups {
        get {
            var groups = Properties
                .Where(property => property.GroupAlias is not null)
                .GroupBy(property => property.EffectiveGroupAlias!, StringComparer.Ordinal)
                .Select(group => new CodeFirstGroupDefinition(
                    group.Key,
                    group.First().GroupName ?? group.Key,
                    group.First().GroupSortOrder));

            if(Groups is not null) {
                var explicitAliases = Groups.Select(group => group.Alias).ToHashSet(StringComparer.Ordinal);
                return Groups.Concat(groups
                    .Where(group => group.Alias.Contains('/', StringComparison.Ordinal) && !explicitAliases.Contains(group.Alias))
                    .OrderBy(group => group.SortOrder)
                    .ThenBy(group => group.Alias, StringComparer.Ordinal))
                    .ToArray();
            }

            return groups
                .OrderBy(group => group.SortOrder)
                .ThenBy(group => group.Alias, StringComparer.Ordinal)
                .ToArray();
        }
    }

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
