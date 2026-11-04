using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Integer",
    PropertyEditors.UmbracoEditors.Integer.Alias,
    EditorUiAlias = PropertyEditors.UmbracoEditors.Integer.UiAlias,
    ConfigurationJson = """{"min":0,"max":1000000}""")]
public partial class CodeFirstIntegerDataType;
