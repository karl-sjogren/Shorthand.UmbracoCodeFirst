using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Long Text",
    PropertyEditors.UmbracoEditors.TextArea.Alias,
    EditorUiAlias = PropertyEditors.UmbracoEditors.TextArea.UiAlias,
    ConfigurationJson = """{"maxChars":5000,"rows":8}""")]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstLongTextDataType;
