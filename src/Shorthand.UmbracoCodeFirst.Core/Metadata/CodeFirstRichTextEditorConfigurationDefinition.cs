namespace Shorthand.UmbracoCodeFirst.Metadata;

public abstract class CodeFirstRichTextEditorConfigurationDefinition : CodeFirstBlockEditorConfigurationDefinition {
    protected CodeFirstRichTextEditorConfigurationDefinition() {
        ToolbarRows = [[
            ["Umb.Tiptap.Toolbar.SourceEditor"],
            ["Umb.Tiptap.Toolbar.Bold", "Umb.Tiptap.Toolbar.Italic", "Umb.Tiptap.Toolbar.Underline"],
            ["Umb.Tiptap.Toolbar.TextAlignLeft", "Umb.Tiptap.Toolbar.TextAlignCenter", "Umb.Tiptap.Toolbar.TextAlignRight"],
            ["Umb.Tiptap.Toolbar.BulletList", "Umb.Tiptap.Toolbar.OrderedList"],
            ["Umb.Tiptap.Toolbar.Blockquote", "Umb.Tiptap.Toolbar.HorizontalRule"],
            ["Umb.Tiptap.Toolbar.Link", "Umb.Tiptap.Toolbar.Unlink"],
            ["Umb.Tiptap.Toolbar.MediaPicker", "Umb.Tiptap.Toolbar.EmbeddedMedia"],
        ]];
    }

    /// <summary>The aliases of enabled rich text extensions.</summary>
    public List<string>? Extensions { get; set; } = [
        "Umb.Tiptap.RichTextEssentials",
        "Umb.Tiptap.Anchor",
        "Umb.Tiptap.Block",
        "Umb.Tiptap.Blockquote",
        "Umb.Tiptap.Bold",
        "Umb.Tiptap.BulletList",
        "Umb.Tiptap.CodeBlock",
        "Umb.Tiptap.Embed",
        "Umb.Tiptap.Figure",
        "Umb.Tiptap.Heading",
        "Umb.Tiptap.HorizontalRule",
        "Umb.Tiptap.HtmlAttributeClass",
        "Umb.Tiptap.HtmlAttributeDataset",
        "Umb.Tiptap.HtmlAttributeId",
        "Umb.Tiptap.HtmlAttributeStyle",
        "Umb.Tiptap.HtmlTagDiv",
        "Umb.Tiptap.HtmlTagSpan",
        "Umb.Tiptap.Image",
        "Umb.Tiptap.Italic",
        "Umb.Tiptap.Link",
        "Umb.Tiptap.MediaUpload",
        "Umb.Tiptap.OrderedList",
        "Umb.Tiptap.Strike",
        "Umb.Tiptap.Subscript",
        "Umb.Tiptap.Superscript",
        "Umb.Tiptap.Table",
        "Umb.Tiptap.TextAlign",
        "Umb.Tiptap.TextDirection",
        "Umb.Tiptap.TextIndent",
        "Umb.Tiptap.TrailingNode",
        "Umb.Tiptap.Underline",
    ];

    /// <summary>The maximum image size configured for the rich text editor.</summary>
    public int? MaxImageSize { get; set; } = 500;

    /// <summary>The size of the editing overlay.</summary>
    public string? OverlaySize { get; set; } = "medium";
}
