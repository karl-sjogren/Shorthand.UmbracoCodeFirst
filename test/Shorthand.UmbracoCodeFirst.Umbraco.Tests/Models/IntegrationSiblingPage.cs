using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;

[UCFDocumentType("integrationSiblingPage", "Integration Sibling Page")]
[UCFComposite(typeof(IntegrationCompositionBase))]
public partial class IntegrationSiblingPage {
    [UCFProperty("siblingValue", "Sibling value", IntegrationTextDataType.Alias)]
    [UCFTab("page", "Page", 0)]
    public string SiblingValue { get; } = string.Empty;
}
