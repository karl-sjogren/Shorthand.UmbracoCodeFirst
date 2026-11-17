using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;

[UCFDocument("integrationMetadataComposition", "Integration Metadata Composition")]
public partial class IntegrationMetadataComposition {
    [UCFProperty("metadataValue", "Metadata value", IntegrationTextDataType.Alias)]
    [UCFGroup("metadata", "Metadata", 0)]
    public string MetadataValue { get; } = string.Empty;
}
