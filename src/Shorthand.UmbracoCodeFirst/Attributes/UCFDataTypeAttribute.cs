namespace Shorthand.UmbracoCodeFirst.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class UCFDataTypeAttribute : Attribute {
    public UCFDataTypeAttribute(string name, string editorAlias, string editorUiAlias) {
        Name = name;
        EditorAlias = editorAlias;
        EditorUiAlias = editorUiAlias;
    }

    public UCFDataTypeAttribute(string name, Type editor) {
        Name = name;
        EditorAlias = ReadEditorValue(editor, "Alias");
        EditorUiAlias = ReadEditorValue(editor, "UiAlias");
        Editor = editor;
    }

    public UCFDataTypeAttribute(string name, Type editor, Type configurationType)
        : this(name, editor) {
        ConfigurationType = configurationType;
    }

    public string? Alias { get; set; }

    public string Name { get; }

    public string EditorAlias { get; }

    public string EditorUiAlias { get; }

    public Type? Editor { get; }

    public string? ConfigurationJson { get; set; }

    public Type? ConfigurationType { get; set; }

    private static string ReadEditorValue(Type editor, string name) =>
        editor.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            ?.GetRawConstantValue() as string
        ?? throw new ArgumentException($"The editor type must define a public constant string named '{name}'.", nameof(editor));
}
