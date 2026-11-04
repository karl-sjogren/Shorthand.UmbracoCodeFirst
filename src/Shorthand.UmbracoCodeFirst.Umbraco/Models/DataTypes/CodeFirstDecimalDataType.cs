using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Decimal",
    typeof(PropertyEditors.UmbracoEditors.Decimal),
    ConfigurationJson = """{"step":0.01}""")]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstDecimalDataType;
