namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFDocumentAttribute : UCFTypeAttribute {
    public UCFDocumentAttribute(string name)
        : base(name) { }

    public UCFDocumentAttribute(string alias, string name)
        : base(alias, name) { }

    public bool AllowAsRoot { get; set; }

    public string? DefaultTemplateAlias { get; set; }

    public string[] AllowedTemplateAliases { get; set; } = [];

    public string[] AllowedChildAliases { get; set; } = [];
}
