namespace Shorthand.UmbracoCodeFirst.Attributes;

/// <summary>Configures an Umbraco collection for a code-first document or media type.</summary>
/// <param name="collectionConfiguration">
/// A concrete <see cref="Metadata.CodeFirstCollectionConfiguration"/> type with a public parameterless constructor.
/// </param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFCollectionAttribute(Type collectionConfiguration) : Attribute {
    /// <summary>Gets the CLR type containing the collection configuration.</summary>
    public Type CollectionConfiguration { get; } = collectionConfiguration;
}
