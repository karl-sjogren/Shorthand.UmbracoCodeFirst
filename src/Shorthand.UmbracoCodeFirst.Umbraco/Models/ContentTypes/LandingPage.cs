using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.ContentTypes;

[UCFContentType(
    "landingPage",
    "Landing Page",
    Icon = "icon-home",
    AllowAsRoot = true)]
[UCFDescription("A page used as a site landing page.")]
public partial class LandingPage {
    [UCFProperty(
        "heading",
        "Heading",
        "codeFirstText",
        SortOrder = 0,
        Mandatory = true)]
    [UCFTab("content", "Content")]
    public partial string Heading { get; }
}
