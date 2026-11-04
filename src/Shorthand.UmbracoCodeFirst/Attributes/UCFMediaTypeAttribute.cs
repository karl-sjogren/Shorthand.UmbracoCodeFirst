namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFMediaTypeAttribute : UCFTypeAttribute {
    public UCFMediaTypeAttribute(string name)
        : base(name) { }

    public UCFMediaTypeAttribute(string alias, string name)
        : base(alias, name) { }

    public bool AllowAsRoot { get; set; }

    public string[] AllowedChildAliases { get; set; } = [];

    public bool IsListView { get; set; }
}
