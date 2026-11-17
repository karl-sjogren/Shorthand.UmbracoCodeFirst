namespace Shorthand.UmbracoCodeFirst.Attributes;

/// <summary>Declares a code-first Umbraco document type on a partial class.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFDocumentAttribute : UCFTypeAttribute {
    /// <summary>Initializes a document type with an alias derived from its name.</summary>
    /// <param name="name">The display name of the document type.</param>
    public UCFDocumentAttribute(string name)
        : base(name) { }

    /// <summary>Initializes a document type with an explicit alias.</summary>
    /// <param name="alias">The Umbraco alias of the document type.</param>
    /// <param name="name">The display name of the document type.</param>
    public UCFDocumentAttribute(string alias, string name)
        : base(alias, name) { }

    /// <summary>Gets or sets whether content of this type may be created at the content root.</summary>
    public bool AllowAsRoot { get; set; }

    /// <summary>Gets or sets the default template alias, which must also be included in <see cref="AllowedTemplateAliases"/>.</summary>
    public string? DefaultTemplateAlias { get; set; }

    /// <summary>Gets or sets the aliases of templates allowed for this document type.</summary>
    public string[] AllowedTemplateAliases { get; set; } = [];

    /// <summary>Gets or sets the aliases of document types allowed as children, in display order.</summary>
    public string[] AllowedChildAliases { get; set; } = [];
}
