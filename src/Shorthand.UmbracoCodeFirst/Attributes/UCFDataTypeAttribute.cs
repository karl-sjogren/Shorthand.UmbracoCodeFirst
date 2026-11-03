namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFDataTypeAttribute : Attribute {
    public UCFDataTypeAttribute(string name, string editorAlias) {
        Name = name;
        EditorAlias = editorAlias;
    }

    public UCFDataTypeAttribute(string alias, string name, string editorAlias) {
        Alias = alias;
        Name = name;
        EditorAlias = editorAlias;
    }

    public string? Alias { get; }

    public string Name { get; }

    public string EditorAlias { get; }

    public string? EditorUiAlias { get; set; }

    public string? ConfigurationJson { get; set; }
}
