using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;

[UCFDataType(
    "Rich Text",
    typeof(UmbracoEditors.RichTextEditor))]
[UCFFolderPath("CodeFirst")]
public partial class UmbracoBlogRichTextDataType;

internal sealed class UmbracoBlogRichTextConfiguration : UmbracoEditors.RichTextEditor.Configuration {
    public UmbracoBlogRichTextConfiguration() {
    }
}
