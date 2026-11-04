using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DocumentTypes;

[UCFDocumentType(
    "Happy Sparkles",
    Icon = "icon-document")]
public partial class HappySparkles {
    [UCFProperty(
        "Sparkles",
        CodeFirstTextDataType.Alias,
        SortOrder = 0,
        Mandatory = true,
        LabelOnTop = true)]
    [UCFDescription("The happy sparkles!")]
    [UCFTab("happySparkles", "Content")]
    public partial string Sparkles { get; }
}
