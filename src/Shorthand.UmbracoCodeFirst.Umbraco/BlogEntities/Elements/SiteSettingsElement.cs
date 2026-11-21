using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.Elements;

[UCFElementType("Site settings", AllowInLibrary = true, Icon = UmbracoIcons.Settings)]
[UCFFolderPath("Elements")]
public partial class SiteSettingsElement {
    [UCFProperty("Site name", UmbracoBlogTextDataType.Alias, Mandatory = true)]
    public partial string SiteName { get; }

    [UCFProperty("Site tagline", UmbracoBlogTextDataType.Alias)]
    public partial string SiteTagline { get; }

    [UCFProperty("Github link", UmbracoBlogTextDataType.Alias)]
    public partial string GithubLink { get; }

    [UCFProperty("Stackoverflow link", UmbracoBlogTextDataType.Alias)]
    public partial string StackoverflowLink { get; }

    [UCFProperty("Email", CodeFirstEmailDataType.Alias)]
    public partial string Email { get; }
}
