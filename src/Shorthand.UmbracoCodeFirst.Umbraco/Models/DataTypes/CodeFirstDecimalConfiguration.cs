namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

public sealed class CodeFirstDecimalConfiguration :
    PropertyEditors.UmbracoEditors.Decimal.Configuration {
    public CodeFirstDecimalConfiguration() {
        Step = 0.01m;
    }
}
