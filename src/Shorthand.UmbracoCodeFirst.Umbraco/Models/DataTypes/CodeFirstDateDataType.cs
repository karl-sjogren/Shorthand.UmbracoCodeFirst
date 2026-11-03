using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.PropertyEditors;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "codeFirstDate",
    "Code First Date",
    UmbracoEditors.DateTime.Alias,
    EditorUiAlias = UmbracoEditors.DateTime.UiAlias,
    ConfigurationJson = """{"format":"yyyy-MM-dd"}""")]
public partial class CodeFirstDateDataType;
