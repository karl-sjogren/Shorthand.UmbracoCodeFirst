namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class UCFCompositeAttribute(Type documentType) : Attribute {
    public Type DocumentType { get; } = documentType;
}
