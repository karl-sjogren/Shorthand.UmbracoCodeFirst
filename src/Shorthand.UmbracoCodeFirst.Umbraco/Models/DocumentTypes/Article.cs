using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DocumentTypes;

[UCFDocumentType(
    "Article",
    Icon = "icon-document",
    AllowAsRoot = true)]
[UCFComposite(typeof(HappySparkles))]
[UCFComposite(typeof(SeoComposite))]
[UCFDescription("An article managed by the code-first sample model.")]
public partial class Article {
    [UCFProperty(
        "Title!",
        CodeFirstTextDataType.Alias,
        SortOrder = 0,
        Mandatory = true,
        LabelOnTop = true)]
    [UCFDescription("The article headline.")]
    [UCFTab("Content")]
    public partial string Title { get; }

    [UCFProperty(
        "Body text",
        CodeFirstLongTextDataType.Alias,
        SortOrder = 1)]
    [UCFTab("Content")]
    public partial string BodyText { get; }
}
