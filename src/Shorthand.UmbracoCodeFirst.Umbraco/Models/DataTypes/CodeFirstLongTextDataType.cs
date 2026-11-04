using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Long Text",
    typeof(PropertyEditors.UmbracoEditors.TextArea),
    typeof(CodeFirstLongTextConfiguration))]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstLongTextDataType;
