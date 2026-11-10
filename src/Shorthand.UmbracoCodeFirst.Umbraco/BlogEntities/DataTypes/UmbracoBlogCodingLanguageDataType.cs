using System.Text.Json;
using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;

[UCFDataType(
    "Coding language",
    typeof(UmbracoEditors.Dropdown),
    typeof(UmbracoBlogCodingLanguageConfiguration))]
[UCFFolderPath("CodeFirst")]
public partial class UmbracoBlogCodingLanguageDataType;

internal sealed class UmbracoBlogCodingLanguageConfiguration : UmbracoEditors.Dropdown.Configuration {
    public UmbracoBlogCodingLanguageConfiguration() {
        Items = JsonSerializer.SerializeToElement(new[] {
            "csharp",
            "javascript",
            "typescript",
            "bash",
            "pwsh",
            "plaintext"
        });
    }
}
