namespace Shorthand.UmbracoCodeFirst.Attributes;

/// <summary>Specifies the description of a code-first type or property.</summary>
/// <param name="description">The description displayed in Umbraco.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class UCFDescriptionAttribute(string description) : Attribute {
    /// <summary>Gets the description displayed in Umbraco.</summary>
    public string Description { get; } = description;
}
