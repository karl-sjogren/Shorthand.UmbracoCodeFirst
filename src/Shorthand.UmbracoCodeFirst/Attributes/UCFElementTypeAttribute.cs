namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFElementTypeAttribute : UCFTypeAttribute {
    public UCFElementTypeAttribute(string name)
        : base(name) { }

    public UCFElementTypeAttribute(string alias, string name)
        : base(alias, name) { }

    public bool AllowInLibrary { get; set; }
}
