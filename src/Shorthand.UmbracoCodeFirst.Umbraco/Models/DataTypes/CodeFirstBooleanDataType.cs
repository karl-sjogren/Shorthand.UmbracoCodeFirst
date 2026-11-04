using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Boolean",
    PropertyEditors.UmbracoEditors.Boolean.Alias,
    EditorUiAlias = PropertyEditors.UmbracoEditors.Boolean.UiAlias,
    ConfigurationJson = """{"default":false}""")]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstBooleanDataType;
