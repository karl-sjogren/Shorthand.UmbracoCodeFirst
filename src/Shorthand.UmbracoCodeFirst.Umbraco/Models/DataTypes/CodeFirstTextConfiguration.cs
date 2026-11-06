namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

internal sealed class CodeFirstTextConfiguration :
    PropertyEditors.UmbracoEditors.TextBox.Configuration {
    public CodeFirstTextConfiguration() {
        MaxChars = 255;
    }
}
