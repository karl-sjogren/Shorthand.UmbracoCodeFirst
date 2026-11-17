namespace Shorthand.UmbracoCodeFirst.Attributes;

/// <summary>Declares a code-first Umbraco data type on a partial class.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFDataTypeAttribute : Attribute {
    /// <summary>Initializes a data type with explicit editor schema and UI aliases.</summary>
    /// <param name="name">The display name of the data type.</param>
    /// <param name="editorAlias">The property editor schema alias.</param>
    /// <param name="editorUiAlias">The property editor UI alias.</param>
    public UCFDataTypeAttribute(string name, string editorAlias, string editorUiAlias) {
        Name = name;
        EditorAlias = editorAlias;
        EditorUiAlias = editorUiAlias;
    }

    /// <summary>Initializes a data type using aliases defined by an editor type.</summary>
    /// <param name="name">The display name of the data type.</param>
    /// <param name="editor">A type exposing public constant strings named <c>Alias</c> and <c>UiAlias</c>.</param>
    /// <exception cref="ArgumentException">The editor type does not expose both required constants.</exception>
    public UCFDataTypeAttribute(string name, Type editor) {
        Name = name;
        EditorAlias = ReadEditorValue(editor, "Alias");
        EditorUiAlias = ReadEditorValue(editor, "UiAlias");
        Editor = editor;
    }

    /// <summary>Initializes a data type using an editor type and a configuration type.</summary>
    /// <param name="name">The display name of the data type.</param>
    /// <param name="editor">A type exposing public constant strings named <c>Alias</c> and <c>UiAlias</c>.</param>
    /// <param name="configurationType">The type describing the editor configuration.</param>
    /// <exception cref="ArgumentException">The editor type does not expose both required constants.</exception>
    public UCFDataTypeAttribute(string name, Type editor, Type configurationType)
        : this(name, editor) {
        ConfigurationType = configurationType;
    }

    /// <summary>Gets or sets the code-first alias; when unset, it is derived from <see cref="Name"/>.</summary>
    /// <remarks>This is a code-first identity, not a persisted Umbraco data type alias.</remarks>
    public string? Alias { get; set; }

    /// <summary>Gets the display name of the data type.</summary>
    public string Name { get; }

    /// <summary>Gets the property editor schema alias.</summary>
    public string EditorAlias { get; }

    /// <summary>Gets the property editor UI alias.</summary>
    public string EditorUiAlias { get; }

    /// <summary>Gets the editor type used to obtain the aliases, or <see langword="null"/> when aliases were supplied directly.</summary>
    public Type? Editor { get; }

    /// <summary>Gets or sets a JSON object merged into the data type configuration.</summary>
    /// <remarks><see cref="ConfigurationType"/> takes precedence when both are specified.</remarks>
    [StringSyntax(StringSyntaxAttribute.Json)]
    public string? ConfigurationJson { get; set; }

    /// <summary>Gets or sets a type describing the data type's editor configuration.</summary>
    /// <remarks>This takes precedence over <see cref="ConfigurationJson"/> when both are specified.</remarks>
    public Type? ConfigurationType { get; set; }

    private static string ReadEditorValue(Type editor, string name) =>
        editor.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            ?.GetRawConstantValue() as string
        ?? throw new ArgumentException($"The editor type must define a public constant string named '{name}'.", nameof(editor));
}
