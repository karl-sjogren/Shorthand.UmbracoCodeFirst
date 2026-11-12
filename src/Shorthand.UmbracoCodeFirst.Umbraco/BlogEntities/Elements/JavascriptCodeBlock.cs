using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.Elements;

[UCFElementType("Javascript Code block", AllowInLibrary = true, Icon = UmbracoIcons.Code)]
public partial class JavascriptCodeBlock {
    [UCFProperty("Code",
        UmbracoBlogJavascriptCodeEditorDataType.Alias,
        LabelOnTop = true)]
    public partial string Code { get; }
}
