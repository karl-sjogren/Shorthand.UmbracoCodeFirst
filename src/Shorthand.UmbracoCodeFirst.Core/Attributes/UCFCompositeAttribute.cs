namespace Shorthand.UmbracoCodeFirst.Attributes;

/// <summary>Adds a code-first document or element type as a composition of another type of the same kind.</summary>
/// <param name="documentType">The CLR type of the document or element type to compose.</param>
/// <remarks>Apply this attribute more than once to declare multiple compositions.</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class UCFCompositeAttribute(Type documentType) : Attribute {
    /// <summary>Gets the CLR type of the composed document type.</summary>
    public Type DocumentType { get; } = documentType;
}
