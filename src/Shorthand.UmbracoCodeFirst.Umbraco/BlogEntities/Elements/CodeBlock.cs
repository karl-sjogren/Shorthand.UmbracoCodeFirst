using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.Elements;

[UCFElementType("Code block", AllowInLibrary = true, Icon = UmbracoIcons.Code)]
public partial class CodeBlock {
    [UCFProperty("Language",
        UmbracoBlogCodingLanguageDataType.Alias)]
    public partial string Language { get; }

    [UCFProperty("Code",
        UmbracoBlogCodeEditorDataType.Alias,
        LabelOnTop = true)]
    public partial string Code { get; }
}
