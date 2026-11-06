using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DocumentTypes;

[UCFDocumentType(
    "seoComposite",
    "SEO Composite",
    Icon = UmbracoIcons.Document)]
[UCFFolderPath("Compositions")]
public partial class SeoComposite {
    [UCFProperty(
        "seoCompositeTitle",
        "SEO Title",
        CodeFirstTextDataType.Alias,
        SortOrder = 0,
        Mandatory = true,
        LabelOnTop = true)]
    [UCFGroup("seoCompositeGroup", "SEO")]
    public partial string SeoCompositeTitle { get; }
}
