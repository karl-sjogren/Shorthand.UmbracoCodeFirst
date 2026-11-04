using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Date",
    typeof(PropertyEditors.UmbracoEditors.DateTime),
    typeof(CodeFirstDateConfiguration))]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstDateDataType;
