namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class UCFDescriptionAttribute(string description) : Attribute {
    public string Description { get; } = description;
}
