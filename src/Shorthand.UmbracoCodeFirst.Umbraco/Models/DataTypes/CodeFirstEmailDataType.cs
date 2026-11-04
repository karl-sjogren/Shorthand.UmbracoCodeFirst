using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Email",
    typeof(PropertyEditors.UmbracoEditors.EmailAddress),
    typeof(CodeFirstEmailConfiguration))]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstEmailDataType;
