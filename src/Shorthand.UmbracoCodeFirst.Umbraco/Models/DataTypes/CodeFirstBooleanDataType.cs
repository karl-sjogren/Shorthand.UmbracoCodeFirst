using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Boolean",
    typeof(PropertyEditors.UmbracoEditors.Boolean),
    ConfigurationJson = """{"default":false}""")]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstBooleanDataType;
