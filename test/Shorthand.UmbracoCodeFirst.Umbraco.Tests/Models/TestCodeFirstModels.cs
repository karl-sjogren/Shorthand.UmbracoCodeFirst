using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;

[UCFDataType("integrationText", "Integration Text", "Umbraco.TextBox")]
public partial class IntegrationTextDataType {
    public const string Alias = "integrationText";
}

[UCFContentType("integrationCompositionBase", "Integration Composition Base")]
public partial class IntegrationCompositionBase {
    [UCFProperty("sharedTitle", "Shared title", IntegrationTextDataType.Alias)]
    [UCFGroup("shared", "Shared", 0)]
    public string SharedTitle { get; } = string.Empty;
}

[UCFContentType("integrationNestedComposition", "Integration Nested Composition")]
[UCFComposite(typeof(IntegrationCompositionBase))]
public partial class IntegrationNestedComposition {
    [UCFProperty("nestedValue", "Nested value", IntegrationTextDataType.Alias)]
    [UCFGroup("nested", "Nested", 1)]
    public string NestedValue { get; } = string.Empty;
}

[UCFContentType("integrationMetadataComposition", "Integration Metadata Composition")]
public partial class IntegrationMetadataComposition {
    [UCFProperty("metadataValue", "Metadata value", IntegrationTextDataType.Alias)]
    [UCFGroup("metadata", "Metadata", 0)]
    public string MetadataValue { get; } = string.Empty;
}

[UCFContentType("integrationComposedPage", "Integration Composed Page")]
[UCFComposite(typeof(IntegrationNestedComposition))]
[UCFComposite(typeof(IntegrationMetadataComposition))]
public partial class IntegrationComposedPage {
    [UCFProperty("pageTitle", "Page title", IntegrationTextDataType.Alias)]
    [UCFTab("page", "Page", 0)]
    public string PageTitle { get; } = string.Empty;
}

[UCFContentType("integrationSiblingPage", "Integration Sibling Page")]
[UCFComposite(typeof(IntegrationCompositionBase))]
public partial class IntegrationSiblingPage {
    [UCFProperty("siblingValue", "Sibling value", IntegrationTextDataType.Alias)]
    [UCFTab("page", "Page", 0)]
    public string SiblingValue { get; } = string.Empty;
}

public sealed class IntegrationTestDefinitionRegistry : ICodeFirstDefinitionRegistry {
    public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = [
        new(
            CodeFirstDefinitionKind.DataType,
            IntegrationTextDataType.Alias,
            "Integration Text",
            "Umbraco.TextBox",
            []),
        new(
            CodeFirstDefinitionKind.DocumentType,
            "integrationCompositionBase",
            "Integration Composition Base",
            null,
            [
                new(
                    "sharedTitle",
                    "Shared title",
                    IntegrationTextDataType.Alias,
                    GroupAlias: "shared",
                    GroupName: "Shared"),
            ]),
        new(
            CodeFirstDefinitionKind.DocumentType,
            "integrationNestedComposition",
            "Integration Nested Composition",
            null,
            [
                new(
                    "nestedValue",
                    "Nested value",
                    IntegrationTextDataType.Alias,
                    GroupAlias: "nested",
                    GroupName: "Nested"),
            ],
            CompositionAliases: ["integrationCompositionBase"]),
        new(
            CodeFirstDefinitionKind.DocumentType,
            "integrationMetadataComposition",
            "Integration Metadata Composition",
            null,
            [
                new(
                    "metadataValue",
                    "Metadata value",
                    IntegrationTextDataType.Alias,
                    GroupAlias: "metadata",
                    GroupName: "Metadata"),
            ]),
        new(
            CodeFirstDefinitionKind.DocumentType,
            "integrationComposedPage",
            "Integration Composed Page",
            null,
            [
                new(
                    "pageTitle",
                    "Page title",
                    IntegrationTextDataType.Alias,
                    TabAlias: "page",
                    TabName: "Page"),
            ],
            CompositionAliases: [
                "integrationNestedComposition",
                "integrationMetadataComposition"]),
        new(
            CodeFirstDefinitionKind.DocumentType,
            "integrationSiblingPage",
            "Integration Sibling Page",
            null,
            [
                new(
                    "siblingValue",
                    "Sibling value",
                    IntegrationTextDataType.Alias,
                    TabAlias: "page",
                    TabName: "Page"),
            ],
            CompositionAliases: ["integrationCompositionBase"]),
    ];
}
