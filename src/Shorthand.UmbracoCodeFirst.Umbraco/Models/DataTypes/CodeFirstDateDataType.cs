using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Date",
    PropertyEditors.UmbracoEditors.DateTime.Alias,
    EditorUiAlias = PropertyEditors.UmbracoEditors.DateTime.UiAlias,
    ConfigurationJson = """{"format":"yyyy-MM-dd"}""")]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstDateDataType;
