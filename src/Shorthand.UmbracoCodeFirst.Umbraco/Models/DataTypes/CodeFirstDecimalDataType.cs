using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.PropertyEditors;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "codeFirstDecimal",
    "Code First Decimal",
    UmbracoEditors.Decimal.Alias,
    EditorUiAlias = UmbracoEditors.Decimal.UiAlias,
    ConfigurationJson = """{"step":0.01}""")]
public partial class CodeFirstDecimalDataType;
