using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;

public sealed class IntegrationTestDefinitionRegistry : ICodeFirstDefinitionRegistry {
    public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = [
        new(
            CodeFirstDefinitionKind.DataType,
            IntegrationTextDataType.Alias,
            "Integration Text",
            "Umbraco.TextBox",
            [],
            FolderPath: "/Integration/Editors"),
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
            ],
            FolderPath: "Integration/Pages"),
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
            CompositionAliases: ["integrationCompositionBase"],
            FolderPath: "Integration/Pages"),
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
            ],
            FolderPath: "Integration/Pages"),
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
                "integrationMetadataComposition"],
            FolderPath: "Integration/Pages"),
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
            CompositionAliases: ["integrationCompositionBase"],
            FolderPath: "Integration/Pages"),
    ];
}
