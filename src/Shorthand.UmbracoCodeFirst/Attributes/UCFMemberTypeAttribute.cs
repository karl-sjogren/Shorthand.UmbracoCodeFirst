namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFMemberTypeAttribute : UCFTypeAttribute {
    public UCFMemberTypeAttribute(string name)
        : base(name) { }

    public UCFMemberTypeAttribute(string alias, string name)
        : base(alias, name) { }
}
