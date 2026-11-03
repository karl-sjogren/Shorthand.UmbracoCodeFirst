using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.ContentTypes;

[UCFContentType(
    "happySparkles",
    "Happy Sparkles",
    Icon = "icon-document")]
public partial class HappySparkles {
    [UCFProperty(
        "sparkles",
        "Sparkles",
        "codeFirstText",
        SortOrder = 0,
        Mandatory = true,
        LabelOnTop = true)]
    [UCFDescription("The happy sparkles!")]
    [UCFTab("happySparkles", "Content")]
    public partial string Sparkles { get; }
}
