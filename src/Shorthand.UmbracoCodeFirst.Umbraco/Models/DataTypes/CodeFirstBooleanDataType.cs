using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.PropertyEditors;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "codeFirstBoolean",
    "Code First Boolean",
    UmbracoEditors.Boolean.Alias,
    EditorUiAlias = UmbracoEditors.Boolean.UiAlias,
    ConfigurationJson = """{"default":false}""")]
public partial class CodeFirstBooleanDataType;
