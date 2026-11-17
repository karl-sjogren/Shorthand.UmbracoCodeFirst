namespace Shorthand.UmbracoCodeFirst.Attributes;

/// <summary>Declares a code-first Umbraco member type on a partial class.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFMemberAttribute : UCFTypeAttribute {
    /// <summary>Initializes a member type with an alias derived from its name.</summary>
    /// <param name="name">The display name of the member type.</param>
    public UCFMemberAttribute(string name)
        : base(name) { }

    /// <summary>Initializes a member type with an explicit alias.</summary>
    /// <param name="alias">The Umbraco alias of the member type.</param>
    /// <param name="name">The display name of the member type.</param>
    public UCFMemberAttribute(string alias, string name)
        : base(alias, name) { }
}
