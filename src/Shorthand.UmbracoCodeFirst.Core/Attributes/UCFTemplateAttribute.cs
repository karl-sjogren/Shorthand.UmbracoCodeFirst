namespace Shorthand.UmbracoCodeFirst.Attributes;

/// <summary>Declares a code-first Umbraco template on a root-level Razor view.</summary>
/// <param name="name">The non-empty display name of the template.</param>
/// <remarks>The view filename, without its extension, supplies the template alias.</remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFTemplateAttribute(string name) : Attribute {
    /// <summary>Gets the display name of the template.</summary>
    public string Name { get; } = name;
}
