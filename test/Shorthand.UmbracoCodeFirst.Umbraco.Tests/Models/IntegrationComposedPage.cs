using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;

[UCFDocumentType("integrationComposedPage", "Integration Composed Page")]
[UCFComposite(typeof(IntegrationNestedComposition))]
[UCFComposite(typeof(IntegrationMetadataComposition))]
public partial class IntegrationComposedPage {
    [UCFProperty("pageTitle", "Page title", IntegrationTextDataType.Alias)]
    [UCFTab("page", "Page", 0)]
    public string PageTitle { get; } = string.Empty;
}
