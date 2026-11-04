using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Date",
    typeof(PropertyEditors.UmbracoEditors.DateTime),
    ConfigurationJson = """{"format":"yyyy-MM-dd"}""")]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstDateDataType;
