namespace Shorthand.UmbracoCodeFirst.Attributes;

/// <summary>Places a code-first property on an Umbraco property tab.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class UCFTabAttribute : Attribute {
    /// <summary>Initializes a tab with an alias derived from its name.</summary>
    /// <param name="name">The display name of the tab.</param>
    public UCFTabAttribute(string name) {
        Name = name;
    }

    /// <summary>Initializes a tab with an explicit alias.</summary>
    /// <param name="alias">The Umbraco alias of the tab.</param>
    /// <param name="name">The display name of the tab.</param>
    public UCFTabAttribute(string alias, string name) {
        Alias = alias;
        Name = name;
    }

    /// <summary>Initializes a tab with an alias derived from its name and a sort order.</summary>
    /// <param name="name">The display name of the tab.</param>
    /// <param name="sortOrder">The sort order of the tab.</param>
    public UCFTabAttribute(string name, int sortOrder) {
        Name = name;
        SortOrder = sortOrder;
    }

    /// <summary>Initializes a tab with an explicit alias and sort order.</summary>
    /// <param name="alias">The Umbraco alias of the tab.</param>
    /// <param name="name">The display name of the tab.</param>
    /// <param name="sortOrder">The sort order of the tab.</param>
    public UCFTabAttribute(string alias, string name, int sortOrder) {
        Alias = alias;
        Name = name;
        SortOrder = sortOrder;
    }

    /// <summary>Gets the explicit tab alias, or <see langword="null"/> when it is derived from <see cref="Name"/>.</summary>
    public string? Alias { get; }

    /// <summary>Gets the display name of the tab.</summary>
    public string Name { get; }

    /// <summary>Gets or sets the sort order of the tab.</summary>
    public int SortOrder { get; set; }
}
