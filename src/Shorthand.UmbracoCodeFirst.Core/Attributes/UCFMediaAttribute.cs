namespace Shorthand.UmbracoCodeFirst.Attributes;

/// <summary>Declares a code-first Umbraco media type on a partial class.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFMediaAttribute : UCFTypeAttribute {
    /// <summary>Initializes a media type with an alias derived from its name.</summary>
    /// <param name="name">The display name of the media type.</param>
    public UCFMediaAttribute(string name)
        : base(name) { }

    /// <summary>Initializes a media type with an explicit alias.</summary>
    /// <param name="alias">The Umbraco alias of the media type.</param>
    /// <param name="name">The display name of the media type.</param>
    public UCFMediaAttribute(string alias, string name)
        : base(alias, name) { }

    /// <summary>Gets or sets whether media of this type may be created at the media root.</summary>
    public bool AllowAsRoot { get; set; }

    /// <summary>Gets or sets the aliases of media types allowed as children, in display order.</summary>
    public string[] AllowedChildAliases { get; set; } = [];
}
