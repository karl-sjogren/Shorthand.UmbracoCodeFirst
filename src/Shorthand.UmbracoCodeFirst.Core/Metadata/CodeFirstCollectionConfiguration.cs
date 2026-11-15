using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shorthand.UmbracoCodeFirst.Metadata;

public abstract class CodeFirstCollectionConfiguration {
    public bool List { get; set; } = true;

    public bool Grid { get; set; } = true;

    public string? ListViewAlias { get; set; }

    public int PageSize { get; set; } = 10;

    public string OrderBy { get; set; } = CodeFirstCollectionSystemFields.SortOrder;

    public string OrderDirection { get; set; } = "desc";

    public IReadOnlyList<CodeFirstCollectionColumn> Columns { get; set; } = [
        new(CodeFirstCollectionSystemFields.SortOrder, "Sort"),
        new(CodeFirstCollectionSystemFields.UpdateDate, "Last edited"),
        new(CodeFirstCollectionSystemFields.Creator, "Created by"),
    ];

    public static CodeFirstCollectionSettings FromType(Type type, CodeFirstDefinitionKind kind) {
        try {
            return CreateSettings(type, kind);
        } catch(Exception exception) {
            return new CodeFirstCollectionSettings(
                "{}", false, false, 0,
                exception.InnerException?.Message ?? exception.Message);
        }
    }

    private static CodeFirstCollectionSettings CreateSettings(Type type, CodeFirstDefinitionKind kind) {
        if(!typeof(CodeFirstCollectionConfiguration).IsAssignableFrom(type)
            || type.IsAbstract || type.GetConstructor(Type.EmptyTypes) is null) {
            throw new ArgumentException(
                $"Collection configuration '{type.FullName}' must be a concrete {nameof(CodeFirstCollectionConfiguration)} with a public parameterless constructor.",
                nameof(type));
        }

        if(kind is not CodeFirstDefinitionKind.DocumentType and not CodeFirstDefinitionKind.MediaType) {
            throw new ArgumentException("Collections are supported only on document and media types.", nameof(kind));
        }

        var configuration = (CodeFirstCollectionConfiguration)Activator.CreateInstance(type)!;
        if(!configuration.List && !configuration.Grid) {
            throw new ArgumentException("A collection must have at least one layout enabled.", nameof(type));
        }

        if(configuration.ListViewAlias is not null && string.IsNullOrWhiteSpace(configuration.ListViewAlias)) {
            throw new ArgumentException("A custom list view alias cannot be empty.", nameof(type));
        }

        if(configuration.PageSize <= 0 || string.IsNullOrWhiteSpace(configuration.OrderBy)
            || configuration.OrderDirection is not ("asc" or "desc")) {
            throw new ArgumentException("A collection requires a positive page size, an order field, and an order direction of 'asc' or 'desc'.", nameof(type));
        }

        if(configuration.Columns is null || configuration.Columns.Count == 0
            || configuration.Columns.Any(column => column is null
                || string.IsNullOrWhiteSpace(column.Alias) || string.IsNullOrWhiteSpace(column.Header))
            || configuration.Columns.Select(column => column.Alias).Distinct(StringComparer.OrdinalIgnoreCase).Count() != configuration.Columns.Count
            || (configuration.OrderBy != CodeFirstCollectionSystemFields.Name && !configuration.Columns.Any(column => column.Alias == configuration.OrderBy))) {
            throw new ArgumentException("Collection columns must have unique aliases and headings, and include the order field unless it is 'name'.", nameof(type));
        }

        var entity = kind == CodeFirstDefinitionKind.DocumentType ? "Document" : "Media";
        var layouts = new List<object>();
        if(configuration.List) {
            layouts.Add(new { name = "Table", icon = "icon-list", collectionView = configuration.ListViewAlias ?? $"Umb.CollectionView.{entity}.Table" });
        }

        if(configuration.Grid) {
            layouts.Add(new { name = "Grid", icon = "icon-grid", collectionView = $"Umb.CollectionView.{entity}.Grid" });
        }

        var json = JsonSerializer.Serialize(new {
            includeProperties = configuration.Columns.Select(column => new {
                alias = column.Alias,
                header = column.Header,
                isSystem = column.IsSystem ? 1 : 0,
                nameTemplate = column.NameTemplate,
            }),
            layouts,
            pageSize = configuration.PageSize,
            orderBy = configuration.OrderBy,
            orderDirection = configuration.OrderDirection,
        }, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

        return new CodeFirstCollectionSettings(json, configuration.List, configuration.Grid, configuration.PageSize);
    }
}

public abstract class CodeFirstCollectionConfiguration<T> : CodeFirstCollectionConfiguration {
    protected static string Alias<TProperty>(Expression<Func<T, TProperty>> selector) =>
        CodeFirstHelper<T>.Alias(selector);
}

public sealed record CodeFirstCollectionColumn(
    string Alias,
    string Header,
    string? NameTemplate = null) {
    public bool IsSystem => Alias is
        CodeFirstCollectionSystemFields.Name
        or CodeFirstCollectionSystemFields.ContentTypeAlias
        or CodeFirstCollectionSystemFields.CreateDate
        or CodeFirstCollectionSystemFields.SortOrder
        or CodeFirstCollectionSystemFields.UpdateDate
        or CodeFirstCollectionSystemFields.Creator
        or CodeFirstCollectionSystemFields.Updater
        or CodeFirstCollectionSystemFields.Published;
}

public sealed record CodeFirstCollectionSettings(
    string Json,
    bool List,
    bool Grid,
    int PageSize,
    string? Error = null) {
    public static string GetDataTypeAlias(CodeFirstDefinitionKind kind, string alias) =>
        $"collection:{kind}:{alias}";
}
