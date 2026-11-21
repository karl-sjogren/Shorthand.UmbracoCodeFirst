namespace Shorthand.UmbracoCodeFirst.Attributes;

/// <summary>Declares a code-first Umbraco template on a Razor view in the configured views directory.</summary>
/// <param name="name">The non-empty display name of the template.</param>
/// <remarks>The relative path supplies a flattened template alias and a hierarchical generated constant.</remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFTemplateAttribute(string name) : Attribute {
    /// <summary>Gets the display name of the template.</summary>
    public string Name { get; } = name;
}
