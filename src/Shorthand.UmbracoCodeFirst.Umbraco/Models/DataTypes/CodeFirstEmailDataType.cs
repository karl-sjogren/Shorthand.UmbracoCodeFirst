using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.PropertyEditors;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "codeFirstEmail",
    "Code First Email",
    UmbracoEditors.EmailAddress.Alias,
    EditorUiAlias = UmbracoEditors.EmailAddress.UiAlias,
    ConfigurationJson = """{"inputType":"email"}""")]
public partial class CodeFirstEmailDataType;
