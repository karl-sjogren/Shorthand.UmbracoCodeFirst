using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;

[UCFDocumentType("integrationNestedComposition", "Integration Nested Composition")]
[UCFComposite(typeof(IntegrationCompositionBase))]
public partial class IntegrationNestedComposition {
    [UCFProperty("nestedValue", "Nested value", IntegrationTextDataType.Alias)]
    [UCFGroup("nested", "Nested", 1)]
    public string NestedValue { get; } = string.Empty;
}
