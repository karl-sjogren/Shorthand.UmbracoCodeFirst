using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

[UCFDataType(
    "Code First Decimal",
    PropertyEditors.UmbracoEditors.Decimal.Alias,
    EditorUiAlias = PropertyEditors.UmbracoEditors.Decimal.UiAlias,
    ConfigurationJson = """{"step":0.01}""")]
[UCFFolderPath("CodeFirst")]
public partial class CodeFirstDecimalDataType;
