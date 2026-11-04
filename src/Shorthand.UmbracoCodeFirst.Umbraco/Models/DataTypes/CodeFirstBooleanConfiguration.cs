namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

public sealed class CodeFirstBooleanConfiguration :
    PropertyEditors.UmbracoEditors.Boolean.Configuration {
    public CodeFirstBooleanConfiguration() {
        Default = false;
    }
}
