namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFTemplateAttribute(string name) : Attribute {
    public string Name { get; } = name;
}
