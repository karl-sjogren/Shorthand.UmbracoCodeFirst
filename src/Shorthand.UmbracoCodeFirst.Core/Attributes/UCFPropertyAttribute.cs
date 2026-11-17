namespace Shorthand.UmbracoCodeFirst.Attributes;

/// <summary>Declares a property on a code-first document, element, media, or member type.</summary>
/// <remarks>Specify exactly one code-first data type alias or existing data type key.</remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class UCFPropertyAttribute : Attribute {
    /// <summary>Initializes a property whose data type will be specified by <see cref="DataTypeKey"/>.</summary>
    /// <param name="name">The display name of the property.</param>
    public UCFPropertyAttribute(string name) {
        Name = name;
        DataTypeAlias = string.Empty;
    }

    /// <summary>Initializes a property with a code-first data type alias and an alias derived from its name.</summary>
    /// <param name="name">The display name of the property.</param>
    /// <param name="dataTypeAlias">The code-first alias of the property's data type.</param>
    public UCFPropertyAttribute(string name, string dataTypeAlias) {
        Name = name;
        DataTypeAlias = dataTypeAlias;
    }

    /// <summary>Initializes a property with explicit property and code-first data type aliases.</summary>
    /// <param name="alias">The Umbraco alias of the property.</param>
    /// <param name="name">The display name of the property.</param>
    /// <param name="dataTypeAlias">The code-first alias of the property's data type.</param>
    public UCFPropertyAttribute(string alias, string name, string dataTypeAlias) {
        Alias = alias;
        Name = name;
        DataTypeAlias = dataTypeAlias;
    }

    /// <summary>Gets or sets the explicit property alias; when unset, it is derived from <see cref="Name"/>.</summary>
    public string? Alias { get; set; }

    /// <summary>Gets the display name of the property.</summary>
    public string Name { get; }

    /// <summary>Gets the code-first data type alias, or an empty string when a data type key is used.</summary>
    public string DataTypeAlias { get; }

    /// <summary>Gets or sets the GUID string of an existing Umbraco data type instead of a code-first alias.</summary>
    public string? DataTypeKey { get; set; }

    /// <summary>Gets or sets the property's sort order within its group or tab.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets whether the property requires a value.</summary>
    public bool Mandatory { get; set; }

    /// <summary>Gets or sets whether the property label is displayed above the editor.</summary>
    public bool LabelOnTop { get; set; }
}
