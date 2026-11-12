using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.Elements;

[UCFElementType("Shell Code block", AllowInLibrary = true, Icon = UmbracoIcons.Code)]
public partial class ShellCodeBlock {
    [UCFProperty("Code",
        UmbracoBlogShellCodeEditorDataType.Alias,
        LabelOnTop = true)]
    public partial string Code { get; }
}
