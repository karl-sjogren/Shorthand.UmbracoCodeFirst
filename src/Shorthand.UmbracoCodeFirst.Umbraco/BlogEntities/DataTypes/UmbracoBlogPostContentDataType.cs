using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.Elements;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;

[UCFDataType(
    "Blog Post Content",
    typeof(UmbracoEditors.BlockList),
    typeof(UmbracoBlogPostContentConfiguration))]
[UCFFolderPath("CodeFirst")]
public partial class UmbracoBlogPostContentDataType;

internal sealed class UmbracoBlogPostContentConfiguration : UmbracoEditors.BlockList.Configuration {
    public UmbracoBlogPostContentConfiguration() {
        BlockTypes = [
            new(typeof(CodeBlock)) { Label = "Code block", IconColor = "#1b4e89" },
            new(ContentBlock.ModelTypeAlias) { Label = "Content block", BackgroundColor = "#eeeeee" },
        ];
        UseLiveEditing = true;
        CreateModalSize = "medium";
    }
}
