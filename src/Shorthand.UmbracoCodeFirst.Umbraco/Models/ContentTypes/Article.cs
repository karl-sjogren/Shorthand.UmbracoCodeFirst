using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.ContentTypes;

[UCFContentType(
    "article",
    "Article",
    Icon = "icon-document",
    AllowAsRoot = true)]
[UCFComposite(typeof(HappySparkles))]
[UCFComposite(typeof(SeoComposite))]
[UCFDescription("An article managed by the code-first sample model.")]
public partial class Article {
    [UCFProperty(
        "title",
        "Title",
        "codeFirstText",
        SortOrder = 0,
        Mandatory = true,
        LabelOnTop = true)]
    [UCFDescription("The article headline.")]
    [UCFTab("content", "Content")]
    public partial string Title { get; }

    [UCFProperty(
        "Body text",
        "codeFirstLongText",
        SortOrder = 1)]
    [UCFTab("Content")]
    public partial string BodyText { get; }
}
