using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Text",
    typeof(PropertyEditors.UmbracoEditors.TextBox),
    typeof(CodeFirstTextConfiguration))]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstTextDataType;
