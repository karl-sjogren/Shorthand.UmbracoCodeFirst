using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.Documents;

[UCFDocument(
    "Blog Index",
    Icon = UmbracoIcons.List,
    AllowedChildAliases = new[] { BlogPost.ModelTypeAlias })]
[UCFCollection(typeof(BlogIndexCollectionConfiguration))]
public partial class BlogIndex;

internal sealed class BlogIndexCollectionConfiguration : CodeFirstCollectionConfiguration<BlogPost> {
    public BlogIndexCollectionConfiguration() {
        Grid = false;
        ListViewAlias = "Shorthand.UmbracoCodeFirst.CollectionView.BlogIndex";
        PageSize = 25;
        OrderBy = Alias(post => post.PublishDate);
        OrderDirection = "desc";
        Columns = [
            new(Alias(post => post.MainHeading), "Title"),
            new(Alias(post => post.PublishDate), "Publish date"),
        ];
    }
}
