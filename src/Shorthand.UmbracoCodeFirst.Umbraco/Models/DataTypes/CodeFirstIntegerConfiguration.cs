namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

public sealed class CodeFirstIntegerConfiguration :
    PropertyEditors.UmbracoEditors.Integer.Configuration {
    public CodeFirstIntegerConfiguration() {
        Min = 0;
        Max = 1000000;
    }
}
