using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Email",
    PropertyEditors.UmbracoEditors.EmailAddress.Alias,
    EditorUiAlias = PropertyEditors.UmbracoEditors.EmailAddress.UiAlias,
    ConfigurationJson = """{"inputType":"email"}""")]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstEmailDataType;
