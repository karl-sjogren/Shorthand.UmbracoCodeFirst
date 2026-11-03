namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFContentTypeAttribute : Attribute {
    public UCFContentTypeAttribute(string name) {
        Name = name;
    }

    public UCFContentTypeAttribute(string alias, string name) {
        Alias = alias;
        Name = name;
    }

    public string? Alias { get; }

    public string Name { get; }

    public string? Icon { get; set; }

    public bool IsElement { get; set; }

    public bool AllowAsRoot { get; set; }

    public bool AllowInLibrary { get; set; }

    public string? DefaultTemplateAlias { get; set; }

    public string[] AllowedTemplateAliases { get; set; } = [];

    public string[] AllowedChildAliases { get; set; } = [];

    public string? Variations { get; set; }

    public bool IsListView { get; set; }
}
