using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Decimal",
    typeof(PropertyEditors.UmbracoEditors.Decimal),
    typeof(CodeFirstDecimalConfiguration))]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstDecimalDataType;
