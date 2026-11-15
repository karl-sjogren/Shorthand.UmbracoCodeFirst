namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFMemberAttribute : UCFTypeAttribute {
    public UCFMemberAttribute(string name)
        : base(name) { }

    public UCFMemberAttribute(string alias, string name)
        : base(alias, name) { }
}
