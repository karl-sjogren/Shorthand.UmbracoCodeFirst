namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

internal sealed class CodeFirstEmailConfiguration :
    PropertyEditors.UmbracoEditors.EmailAddress.Configuration {
    public CodeFirstEmailConfiguration() {
        InputType = "email";
    }
}
