namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

internal sealed class CodeFirstBooleanConfiguration :
    PropertyEditors.UmbracoEditors.Boolean.Configuration {
    public CodeFirstBooleanConfiguration() {
        Default = false;
    }
}
