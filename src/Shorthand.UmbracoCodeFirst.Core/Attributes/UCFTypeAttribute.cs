namespace Shorthand.UmbracoCodeFirst.Attributes;

/// <summary>Provides common settings for code-first document, element, media, and member types.</summary>
public abstract class UCFTypeAttribute : Attribute {
    /// <summary>Initializes a type with an alias derived from its name.</summary>
    /// <param name="name">The display name of the type.</param>
    protected UCFTypeAttribute(string name) {
        Name = name;
    }

    /// <summary>Initializes a type with an explicit alias.</summary>
    /// <param name="alias">The Umbraco alias of the type.</param>
    /// <param name="name">The display name of the type.</param>
    protected UCFTypeAttribute(string alias, string name) {
        Alias = alias;
        Name = name;
    }

    /// <summary>Gets the explicit alias, or <see langword="null"/> when it is derived from <see cref="Name"/>.</summary>
    public string? Alias { get; }

    /// <summary>Gets the display name of the type.</summary>
    public string Name { get; }

    /// <summary>Gets or sets the Umbraco icon alias for the type.</summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets the content variation flags as a string, such as <c>Culture</c> or <c>Invariant</c>.</summary>
    /// <remarks>When unset, the existing variation settings are left unchanged.</remarks>
    public string? Variations { get; set; }
}
