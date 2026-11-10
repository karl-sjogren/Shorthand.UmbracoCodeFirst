using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;

[UCFDataType(
    "Blog Tags",
    typeof(UmbracoEditors.Tags),
    typeof(UmbracoBlogTagsConfiguration))]
[UCFFolderPath("CodeFirst")]
public partial class UmbracoBlogTagsDataType;

internal sealed class UmbracoBlogTagsConfiguration : UmbracoEditors.Tags.Configuration {
    public UmbracoBlogTagsConfiguration() {
        Group = "Blog Tags";
    }
}
