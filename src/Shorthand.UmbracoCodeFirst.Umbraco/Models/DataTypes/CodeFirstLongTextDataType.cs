using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.PropertyEditors;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "codeFirstLongText",
    "Code First Long Text",
    UmbracoEditors.TextArea.Alias,
    EditorUiAlias = UmbracoEditors.TextArea.UiAlias,
    ConfigurationJson = """{"maxChars":5000,"rows":8}""")]
public partial class CodeFirstLongTextDataType;
