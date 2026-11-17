namespace Shorthand.UmbracoCodeFirst.Attributes;

/// <summary>Places a code-first property in an Umbraco property group.</summary>
/// <remarks>When a tab is also specified, the group is placed inside that tab.</remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class UCFGroupAttribute : Attribute {
    /// <summary>Initializes a group with an alias derived from its name.</summary>
    /// <param name="name">The display name of the group.</param>
    public UCFGroupAttribute(string name) {
        Name = name;
    }

    /// <summary>Initializes a group with an explicit alias.</summary>
    /// <param name="alias">The Umbraco alias of the group.</param>
    /// <param name="name">The display name of the group.</param>
    public UCFGroupAttribute(string alias, string name) {
        Alias = alias;
        Name = name;
    }

    /// <summary>Initializes a group with an explicit alias and sort order.</summary>
    /// <param name="alias">The Umbraco alias of the group.</param>
    /// <param name="name">The display name of the group.</param>
    /// <param name="sortOrder">The sort order of the group within its parent.</param>
    public UCFGroupAttribute(string alias, string name, int sortOrder) {
        Alias = alias;
        Name = name;
        SortOrder = sortOrder;
    }

    /// <summary>Gets the explicit group alias, or <see langword="null"/> when it is derived from <see cref="Name"/>.</summary>
    public string? Alias { get; }

    /// <summary>Gets the display name of the group.</summary>
    public string Name { get; }

    /// <summary>Gets or sets the sort order of the group within its parent.</summary>
    public int SortOrder { get; set; }
}
