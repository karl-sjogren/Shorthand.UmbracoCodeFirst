namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

public sealed class CodeFirstTextConfiguration :
    PropertyEditors.UmbracoEditors.TextBox.Configuration {
    public CodeFirstTextConfiguration() {
        MaxChars = 255;
    }
}
