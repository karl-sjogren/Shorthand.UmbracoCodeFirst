namespace Shorthand.UmbracoCodeFirst.Attributes;

/// <summary>Places a code-first definition in a type-specific Umbraco folder.</summary>
/// <param name="path">The folder path relative to the type root, with segments separated by <c>/</c>.</param>
/// <remarks>An empty path or <c>/</c> selects the root; missing folders are created during synchronization.</remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFFolderPathAttribute(string path) : Attribute {
    /// <summary>Gets the folder path relative to the type root.</summary>
    public string Path { get; } = path;
}
