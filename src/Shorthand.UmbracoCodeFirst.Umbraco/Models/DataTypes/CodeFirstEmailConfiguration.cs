namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

public sealed class CodeFirstEmailConfiguration :
    PropertyEditors.UmbracoEditors.EmailAddress.Configuration {
    public CodeFirstEmailConfiguration() {
        InputType = "email";
    }
}
