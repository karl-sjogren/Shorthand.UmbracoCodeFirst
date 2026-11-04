namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFFolderPathAttribute(string path) : Attribute {
    public string Path { get; } = path;
}
