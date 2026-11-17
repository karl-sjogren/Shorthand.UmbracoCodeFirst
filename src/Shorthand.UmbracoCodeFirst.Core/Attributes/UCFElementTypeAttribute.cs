namespace Shorthand.UmbracoCodeFirst.Attributes;

/// <summary>Declares a code-first Umbraco element type on a partial class.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFElementTypeAttribute : UCFTypeAttribute {
    /// <summary>Initializes an element type with an alias derived from its name.</summary>
    /// <param name="name">The display name of the element type.</param>
    public UCFElementTypeAttribute(string name)
        : base(name) { }

    /// <summary>Initializes an element type with an explicit alias.</summary>
    /// <param name="alias">The Umbraco alias of the element type.</param>
    /// <param name="name">The display name of the element type.</param>
    public UCFElementTypeAttribute(string alias, string name)
        : base(alias, name) { }

    /// <summary>Gets or sets whether elements of this type are allowed in the element library.</summary>
    public bool AllowInLibrary { get; set; }
}
