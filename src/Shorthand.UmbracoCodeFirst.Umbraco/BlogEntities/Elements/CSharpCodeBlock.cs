using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.Elements;

[UCFElementType("C# Code block", AllowInLibrary = true, Icon = UmbracoIcons.Code)]
public partial class CSharpCodeBlock {
    [UCFProperty("Code",
        UmbracoBlogCSharpCodeEditorDataType.Alias,
        LabelOnTop = true)]
    public partial string Code { get; }
}
