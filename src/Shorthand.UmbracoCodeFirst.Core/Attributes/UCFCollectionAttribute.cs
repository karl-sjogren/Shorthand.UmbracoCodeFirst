namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFCollectionAttribute(Type collectionConfiguration) : Attribute {
    public Type CollectionConfiguration { get; } = collectionConfiguration;
}
