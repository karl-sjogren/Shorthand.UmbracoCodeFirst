using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.PropertyEditors;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "codeFirstInteger",
    "Code First Integer",
    UmbracoEditors.Integer.Alias,
    EditorUiAlias = UmbracoEditors.Integer.UiAlias,
    ConfigurationJson = """{"min":0,"max":1000000}""")]
public partial class CodeFirstIntegerDataType;
