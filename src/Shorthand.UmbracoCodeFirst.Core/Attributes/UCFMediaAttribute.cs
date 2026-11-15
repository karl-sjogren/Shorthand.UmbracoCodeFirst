namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFMediaAttribute : UCFTypeAttribute {
    public UCFMediaAttribute(string name)
        : base(name) { }

    public UCFMediaAttribute(string alias, string name)
        : base(alias, name) { }

    public bool AllowAsRoot { get; set; }

    public string[] AllowedChildAliases { get; set; } = [];
}
