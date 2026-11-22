using Shorthand.UmbracoCodeFirst.PropertyEditors;

namespace Shorthand.UmbracoCodeFirst.Metadata;

public abstract class CodeFirstRichTextEditorConfigurationDefinition : CodeFirstBlockEditorConfigurationDefinition {
    protected CodeFirstRichTextEditorConfigurationDefinition() {
        ToolbarRows = [[
            [TiptapToolbar.SourceEditor],
            [TiptapToolbar.Bold, TiptapToolbar.Italic, TiptapToolbar.Underline],
            [TiptapToolbar.TextAlignLeft, TiptapToolbar.TextAlignCenter, TiptapToolbar.TextAlignRight],
            [TiptapToolbar.BulletList, TiptapToolbar.OrderedList],
            [TiptapToolbar.Blockquote, TiptapToolbar.HorizontalRule],
            [TiptapToolbar.Link, TiptapToolbar.Unlink],
            [TiptapToolbar.MediaPicker, TiptapToolbar.EmbeddedMedia],
        ]];
    }

    /// <summary>The aliases of enabled rich text extensions.</summary>
    public List<string>? Extensions { get; set; } = [
        TiptapExtension.RichTextEssentials,
        TiptapExtension.Anchor,
        TiptapExtension.Block,
        TiptapExtension.Blockquote,
        TiptapExtension.Bold,
        TiptapExtension.BulletList,
        TiptapExtension.CodeBlock,
        TiptapExtension.Embed,
        TiptapExtension.Figure,
        TiptapExtension.Heading,
        TiptapExtension.HorizontalRule,
        TiptapExtension.HtmlAttributeClass,
        TiptapExtension.HtmlAttributeDataset,
        TiptapExtension.HtmlAttributeId,
        TiptapExtension.HtmlAttributeStyle,
        TiptapExtension.HtmlTagDiv,
        TiptapExtension.HtmlTagSpan,
        TiptapExtension.Image,
        TiptapExtension.Italic,
        TiptapExtension.Link,
        TiptapExtension.MediaUpload,
        TiptapExtension.OrderedList,
        TiptapExtension.Strike,
        TiptapExtension.Subscript,
        TiptapExtension.Superscript,
        TiptapExtension.Table,
        TiptapExtension.TextAlign,
        TiptapExtension.TextDirection,
        TiptapExtension.TextIndent,
        TiptapExtension.TrailingNode,
        TiptapExtension.Underline,
    ];

    /// <summary>The maximum image size configured for the rich text editor.</summary>
    public int? MaxImageSize { get; set; } = 500;

    /// <summary>The size of the editing overlay.</summary>
    public string? OverlaySize { get; set; } = "medium";
}
