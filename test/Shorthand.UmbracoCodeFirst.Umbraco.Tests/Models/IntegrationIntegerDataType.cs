using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;

[UCFDataType(
    "Integration Integer",
    typeof(PropertyEditors.UmbracoEditors.Integer),
    Alias = "integrationInteger")]
public partial class IntegrationIntegerDataType {
    public const string Alias = "integrationInteger";
}
