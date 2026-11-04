using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Integer",
    typeof(PropertyEditors.UmbracoEditors.Integer),
    typeof(CodeFirstIntegerConfiguration))]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstIntegerDataType;
