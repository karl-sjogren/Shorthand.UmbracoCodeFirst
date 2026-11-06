using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DocumentTypes;

[UCFDocumentType(
    "Landing Page",
    Icon = UmbracoIcons.Home,
    AllowAsRoot = true)]
[UCFDescription("A page used as a site landing page.")]
public partial class LandingPage {
    [UCFProperty(
        "Heading",
        CodeFirstTextDataType.Alias,
        SortOrder = 0,
        Mandatory = true)]
    [UCFTab("Content")]
    public partial string Heading { get; }
}
