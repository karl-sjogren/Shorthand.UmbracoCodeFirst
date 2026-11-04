using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;

public sealed class IntegrationTestDefinitionRegistry : ICodeFirstDefinitionRegistry {
    private static IReadOnlyList<CodeFirstDefinition> BuildDefinitions() => [
        new(
            CodeFirstDefinitionKind.DataType,
            IntegrationTextDataType.Alias,
            "Integration Text",
            "Umbraco.TextBox",
            [],
            FolderPath: "/Integration/Editors"),
        new(
            CodeFirstDefinitionKind.DataType,
            IntegrationIntegerDataType.Alias,
            "Integration Integer",
            "Umbraco.Integer",
            [],
            EditorUiAlias: "Umb.PropertyEditorUi.Integer",
            FolderPath: "Integration/Editors"),
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
        new(
            CodeFirstDefinitionKind.DocumentType,
            "integrationRichDocument",
            "Integration Rich Document",
            null,
            [
                new(
                    "headline",
                    "Headline",
                    IntegrationTextDataType.Alias,
                    Description: "Initial headline description",
                    GroupAlias: "content",
                    GroupName: "Content",
                    GroupSortOrder: 0,
                    TabAlias: "details",
                    TabName: "Details",
                    TabSortOrder: 0,
                    SortOrder: 0,
                    Mandatory: true,
                    ValidationExpression: @"^.{3,}$",
                    ValidationMessage: "Headline must contain at least three characters."),
                new(
                    "count",
                    "Count",
                    IntegrationIntegerDataType.Alias,
                    GroupAlias: "content",
                    GroupName: "Content",
                    GroupSortOrder: 0,
                    SortOrder: 1,
                    LabelOnTop: true),
            ],
            Settings: new(
                Description: "Initial rich document description",
                Icon: "icon-document",
                AllowAsRoot: true),
            FolderPath: "Integration/Rich"),
        new(
            CodeFirstDefinitionKind.DocumentType,
            "integrationElement",
            "Integration Element",
            null,
            [
                new(
                    "elementText",
                    "Element text",
                    IntegrationTextDataType.Alias,
                    GroupAlias: "content",
                    GroupName: "Content",
                    GroupSortOrder: 0),
            ],
            Settings: new(
                Description: "An integration element.",
                Icon: "icon-element",
                IsElement: true,
                AllowInLibrary: true),
            FolderPath: "Integration/Elements"),
        new(
                CodeFirstDefinitionKind.MemberType,
                "integrationMember",
                "Integration Member",
                null,
                [
                    new(
                        "displayName",
                        "Display name",
                        IntegrationTextDataType.Alias,
                        GroupAlias: "profile",
                        GroupName: "Profile",
                        Mandatory: true),
                    new(
                        "biography",
                        "Biography",
                        IntegrationTextDataType.Alias,
                        GroupAlias: "profile",
                        GroupName: "Profile",
                        SortOrder: 1),
                ],
                Settings: new(Description: "Integration member type.", Icon: "icon-user"),
                FolderPath: "Integration/Members"),
        new(
                CodeFirstDefinitionKind.MediaType,
                "integrationMedia",
                "Integration Media",
                null,
                [
                    new(
                        "caption",
                        "Caption",
                        IntegrationTextDataType.Alias,
                        GroupAlias: "image",
                        GroupName: "Image"),
                    new(
                        "altText",
                        "Alt text",
                        IntegrationTextDataType.Alias,
                        GroupAlias: "image",
                        GroupName: "Image",
                        SortOrder: 1),
                ],
                Settings: new(
                    Description: "Integration media type.",
                    Icon: "icon-picture",
                    AllowAsRoot: true),
                FolderPath: "Integration/Media"),
    ];

    public IReadOnlyList<CodeFirstDefinition> Definitions { get; }

    public static IntegrationTestDefinitionRegistry CreateUpdated() =>
        new([
            ..new IntegrationTestDefinitionRegistry().Definitions
                .Where(definition =>
                    definition.Alias is not "integrationRichDocument" and not "integrationElement"),
            new(
                CodeFirstDefinitionKind.DocumentType,
                "integrationRichDocument",
                "Updated Rich Document",
                null,
                [
                    new(
                        "headline",
                        "Updated headline",
                        IntegrationTextDataType.Alias,
                        Description: "Updated headline description",
                        GroupAlias: "content",
                        GroupName: "Updated Content",
                        GroupSortOrder: 1,
                        TabAlias: "details",
                        TabName: "Details",
                        SortOrder: 0,
                        Mandatory: false,
                        ValidationExpression: @"^.{5,}$",
                        ValidationMessage: "Headline must contain at least five characters."),
                    new(
                        "count",
                        "Count",
                        IntegrationIntegerDataType.Alias,
                        GroupAlias: "content",
                        GroupName: "Updated Content",
                        GroupSortOrder: 1,
                        SortOrder: 1,
                        LabelOnTop: false),
                ],
                Settings: new(
                    Description: "Updated rich document description",
                    Icon: "icon-folder",
                    AllowAsRoot: false),
                FolderPath: "Integration/Updated"),
        ]);

    private IntegrationTestDefinitionRegistry(IReadOnlyList<CodeFirstDefinition> definitions) {
        Definitions = definitions;
    }

    public IntegrationTestDefinitionRegistry()
        : this(BuildDefinitions()) { }
}
