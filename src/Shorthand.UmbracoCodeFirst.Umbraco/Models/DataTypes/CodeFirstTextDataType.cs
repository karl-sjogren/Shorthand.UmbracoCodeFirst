using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Text",
    PropertyEditors.UmbracoEditors.TextBox.Alias,
    EditorUiAlias = PropertyEditors.UmbracoEditors.TextBox.UiAlias,
    ConfigurationJson = """{"maxChars":255}""")]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstTextDataType;
