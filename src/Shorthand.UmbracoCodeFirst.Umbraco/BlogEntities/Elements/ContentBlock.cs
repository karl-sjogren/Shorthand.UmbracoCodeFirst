using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.Elements;

[UCFElementType("Content block", AllowInLibrary = true, Icon = UmbracoIcons.Document)]
public partial class ContentBlock {
    [UCFProperty("Content",
        UmbracoBlogRichTextDataType.Alias,
        LabelOnTop = true)]
    public partial string Content { get; }
}
