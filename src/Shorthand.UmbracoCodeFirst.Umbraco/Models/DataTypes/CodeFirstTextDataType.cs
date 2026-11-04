using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.PropertyEditors;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Text",
    UmbracoEditors.TextBox.Alias,
    EditorUiAlias = UmbracoEditors.TextBox.UiAlias,
    ConfigurationJson = """{"maxChars":255}""")]
public partial class CodeFirstTextDataType;
