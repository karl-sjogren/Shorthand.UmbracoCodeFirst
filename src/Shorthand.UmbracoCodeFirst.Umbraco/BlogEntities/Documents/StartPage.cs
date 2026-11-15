using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.Documents;

[UCFDocument(
    "Start Page",
    Icon = UmbracoIcons.Star,
    AllowAsRoot = true,
    AllowedChildAliases = new[] { BlogPost.ModelTypeAlias })]
[UCFCollection(typeof(StartPageCollectionConfiguration))]
[UCFComposite(typeof(SeoComposite2))]
public partial class StartPage {
    [UCFProperty(
        "Main heading",
        UmbracoBlogTextDataType.Alias,
        SortOrder = 0,
        Mandatory = true)]
    [UCFDescription("The main heading for the start page.")]
    [ContentTab]
    public partial string MainHeading { get; }
}

internal sealed class StartPageCollectionConfiguration : CodeFirstCollectionConfiguration {
    public StartPageCollectionConfiguration() {
        OrderBy = CodeFirstCollectionSystemFields.UpdateDate;
        PageSize = 25;
    }
}
