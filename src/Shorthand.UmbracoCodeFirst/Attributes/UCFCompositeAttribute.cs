namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class UCFCompositeAttribute(Type contentType) : Attribute {
    public Type ContentType { get; } = contentType;
}
