using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Long Text",
    typeof(PropertyEditors.UmbracoEditors.TextArea),
    ConfigurationJson = """{"maxChars":5000,"rows":8}""")]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstLongTextDataType;
