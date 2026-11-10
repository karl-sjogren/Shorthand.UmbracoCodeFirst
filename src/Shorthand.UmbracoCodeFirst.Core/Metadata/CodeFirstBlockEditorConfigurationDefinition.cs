using System.Text.Json.Serialization;

namespace Shorthand.UmbracoCodeFirst.Metadata;

public abstract class CodeFirstBlockEditorConfigurationDefinition : CodeFirstEditorConfigurationDefinition {
    [JsonIgnore]
    public IReadOnlyList<CodeFirstBlockType>? BlockTypes { get; set; }

    [JsonIgnore]
    public IReadOnlyList<CodeFirstBlockGroup>? Groups { get; set; }

    [JsonIgnore]
    public CodeFirstBlockValidationRange? ValidationRange { get; set; }

    [JsonIgnore]
    public IReadOnlyList<IReadOnlyList<IReadOnlyList<string>>>? ToolbarRows { get; set; }

    [JsonIgnore]
    public IReadOnlyList<IReadOnlyList<string>>? StatusbarGroups { get; set; }

    [JsonIgnore]
    public CodeFirstRichTextDimensions? EditorDimensions { get; set; }

    [JsonIgnore]
    public IReadOnlyList<string>? StylesheetPaths { get; set; }
}

public sealed class CodeFirstBlockValidationRange {
    public int? Min { get; set; }
    public int? Max { get; set; }
}

public sealed class CodeFirstRichTextDimensions {
    public int? Width { get; set; }
    public int? Height { get; set; }
}

public sealed record CodeFirstElementReference {
    public CodeFirstElementReference(string alias) => Alias = alias;

    public CodeFirstElementReference(Type modelType) => ModelType = modelType;

    public string? Alias { get; }

    public Type? ModelType { get; }
}

public sealed class CodeFirstBlockType {
    public CodeFirstBlockType(string contentAlias) => Content = new(contentAlias);

    public CodeFirstBlockType(Type contentType) => Content = new(contentType);

    public CodeFirstElementReference Content { get; }
    public CodeFirstElementReference? Settings { get; set; }
    public string? Label { get; set; }
    public string? Thumbnail { get; set; }
    public string? IconColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? EditorSize { get; set; }
    public bool? ForceHideContentEditorInOverlay { get; set; }
    public bool? DisplayInline { get; set; }
    public bool? InlineEditing { get; set; }
    public bool? AllowAtRoot { get; set; }
    public bool? AllowInAreas { get; set; }
    public int? AreaGridColumns { get; set; }
    public int? RowMinSpan { get; set; }
    public int? RowMaxSpan { get; set; }
    public IReadOnlyList<int>? ColumnSpans { get; set; }
    public string? GroupAlias { get; set; }
    public IReadOnlyList<CodeFirstBlockArea>? Areas { get; set; }
}

public sealed class CodeFirstBlockArea(string alias) {
    public string Alias { get; } = alias;
    public Guid? Key { get; set; }
    public int? ColumnSpan { get; set; }
    public int? RowSpan { get; set; }
    public int? MinAllowed { get; set; }
    public int? MaxAllowed { get; set; }
    public string? CreateLabel { get; set; }
    public IReadOnlyList<CodeFirstBlockAreaPermission>? SpecifiedAllowance { get; set; }
}

public sealed class CodeFirstBlockAreaPermission {
    public CodeFirstElementReference? Element { get; set; }
    public string? GroupAlias { get; set; }
    public int? MinAllowed { get; set; }
    public int? MaxAllowed { get; set; }
}

public sealed class CodeFirstBlockGroup(string alias, string name) {
    public string Alias { get; } = alias;
    public string Name { get; } = name;
    public Guid? Key { get; set; }
}
