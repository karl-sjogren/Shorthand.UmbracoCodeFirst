namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

public sealed class CodeFirstDateConfiguration :
    PropertyEditors.UmbracoEditors.DateTime.Configuration {
    public CodeFirstDateConfiguration() {
        Format = "yyyy-MM-dd";
    }
}
