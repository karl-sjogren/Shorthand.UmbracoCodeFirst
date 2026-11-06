namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

internal sealed class CodeFirstDateConfiguration :
    PropertyEditors.UmbracoEditors.DateTime.Configuration {
    public CodeFirstDateConfiguration() {
        Format = "yyyy-MM-dd";
    }
}
