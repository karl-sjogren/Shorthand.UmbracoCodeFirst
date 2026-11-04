using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;

[UCFDataType(
    "Integration Text",
    typeof(PropertyEditors.UmbracoEditors.TextBox),
    Alias = "integrationText")]
public partial class IntegrationTextDataType {
    public const string Alias = "integrationText";
}
