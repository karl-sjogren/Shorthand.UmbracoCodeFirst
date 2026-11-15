using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;

[UCFDataType(
    "Blog Publish Date",
    typeof(UmbracoEditors.DateTime),
    typeof(UmbracoBlogPublishDateConfiguration))]
[UCFFolderPath("CodeFirst")]
public partial class UmbracoBlogPublishDateDataType;

internal sealed class UmbracoBlogPublishDateConfiguration : UmbracoEditors.DateTime.Configuration {
    public UmbracoBlogPublishDateConfiguration() {
        Format = "yyyy-MM-dd HH:mm";
    }
}
