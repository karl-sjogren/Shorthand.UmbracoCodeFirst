using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.ContentTypes;

[UCFContentType(
    "Landing Page",
    Icon = "icon-home",
    AllowAsRoot = true)]
[UCFDescription("A page used as a site landing page.")]
public partial class LandingPage {
    [UCFProperty(
        "Heading",
        "codeFirstText",
        SortOrder = 0,
        Mandatory = true)]
    [UCFTab("Content")]
    public partial string Heading { get; }
}
