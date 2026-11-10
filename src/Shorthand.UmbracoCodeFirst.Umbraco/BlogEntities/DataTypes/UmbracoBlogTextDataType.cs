using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;

[UCFDataType(
    "Text",
    typeof(UmbracoEditors.TextBox),
    typeof(UmbracoBlogTextConfiguration))]
[UCFFolderPath("CodeFirst")]
public partial class UmbracoBlogTextDataType;

internal sealed class UmbracoBlogTextConfiguration : UmbracoEditors.TextBox.Configuration {
    public UmbracoBlogTextConfiguration() {
        MaxChars = 255;
    }
}
