namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

public sealed class CodeFirstLongTextConfiguration :
    PropertyEditors.UmbracoEditors.TextArea.Configuration {
    public CodeFirstLongTextConfiguration() {
        MaxChars = 5000;
        Rows = 8;
    }
}
