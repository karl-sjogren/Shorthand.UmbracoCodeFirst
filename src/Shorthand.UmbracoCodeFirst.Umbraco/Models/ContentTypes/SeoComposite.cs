using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.ContentTypes;

[UCFContentType(
    "seoComposite",
    "SEO Composite",
    Icon = "icon-document")]
public partial class SeoComposite {
    [UCFProperty(
        "seoCompositeTitle",
        "SEO Title",
        "codeFirstText",
        SortOrder = 0,
        Mandatory = true,
        LabelOnTop = true)]
    [UCFGroup("seoCompositeGroup", "SEO")]
    public partial string SeoCompositeTitle { get; }
}
