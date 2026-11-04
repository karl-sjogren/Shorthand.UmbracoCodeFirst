using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;

[UCFDocumentType("integrationCompositionBase", "Integration Composition Base")]
public partial class IntegrationCompositionBase {
    [UCFProperty("sharedTitle", "Shared title", IntegrationTextDataType.Alias)]
    [UCFGroup("shared", "Shared", 0)]
    public string SharedTitle { get; } = string.Empty;
}
