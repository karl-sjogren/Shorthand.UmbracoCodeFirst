using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Integer",
    typeof(PropertyEditors.UmbracoEditors.Integer),
    ConfigurationJson = """{"min":0,"max":1000000}""")]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstIntegerDataType;
