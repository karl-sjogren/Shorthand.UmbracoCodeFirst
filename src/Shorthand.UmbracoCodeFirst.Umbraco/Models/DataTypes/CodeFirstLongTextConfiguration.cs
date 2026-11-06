namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

internal sealed class CodeFirstLongTextConfiguration :
    PropertyEditors.UmbracoEditors.TextArea.Configuration {
    public CodeFirstLongTextConfiguration() {
        MaxChars = 5000;
        Rows = 8;
    }
}
