using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Email",
    typeof(PropertyEditors.UmbracoEditors.EmailAddress),
    ConfigurationJson = """{"inputType":"email"}""")]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstEmailDataType;
