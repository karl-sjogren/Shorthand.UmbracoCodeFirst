namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

internal sealed class CodeFirstIntegerConfiguration :
    PropertyEditors.UmbracoEditors.Integer.Configuration {
    public CodeFirstIntegerConfiguration() {
        Min = 0;
        Max = 1000000;
    }
}
