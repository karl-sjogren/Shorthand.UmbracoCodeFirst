using Shorthand.UmbracoCodeFirst.Backoffice.Models;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Tests;

public class CodeFirstStatusMappingTests {
    [Fact]
    public void Map_DefinitionsAndReportStatuses_OrdersDefinitionsAndUsesStatuses() {
        // Arrange
        var registry = new TestRegistry([
            new(
                CodeFirstDefinitionKind.DocumentType,
                "productPage",
                "Product Page",
                null,
                [new(
                    "price",
                    "Price",
                    "decimal",
                    "The product price.",
                    "pricing",
                    "Pricing",
                    TabAlias: "commerce",
                    TabName: "Commerce",
                    SortOrder: 2,
                    Mandatory: true,
                    ValidationExpression: "^[0-9]+$",
                    ValidationMessage: "Numbers only",
                    LabelOnTop: true)],
                FolderPath: "Commerce/Pages"),
            new(
                CodeFirstDefinitionKind.DocumentType,
                "seoComposite",
                "SEO Composite",
                null,
                [],
                new CodeFirstTypeSettings(IsElement: true),
                FolderPath: "Shared/Elements"),
            new(
                CodeFirstDefinitionKind.DataType,
                "decimal",
                "Decimal",
                "Umbraco.Decimal",
                [],
                EditorUiAlias: "Umb.PropertyEditorUi.Decimal"),
            new(
                CodeFirstDefinitionKind.MediaType,
                "imageAsset",
                "Image Asset",
                null,
                [new("caption", "Caption", "text")]),
            new(
                CodeFirstDefinitionKind.MemberType,
                "memberProfile",
                "Member Profile",
                null,
                [new("displayName", "Display name", "text", Mandatory: true)]),
        ]);
        var reports = new SynchronizationReportStore();
        reports.Set(new SynchronizationReport(
            DateTimeOffset.UtcNow,
            [
                new(
                    CodeFirstDefinitionKind.DocumentType,
                    "productPage",
                    "Product Page",
                    SynchronizationStatus.Created,
                    null),
                new(
                    CodeFirstDefinitionKind.DocumentType,
                    "seoComposite",
                    "SEO Composite",
                    SynchronizationStatus.Failed,
                    "Element synchronization failed."),
            ]));

        // Act
        var result = CodeFirstStatusMapping.Map(
            registry,
            reports,
            new Dictionary<(CodeFirstDefinitionKind Kind, string Alias), string> {
                [(CodeFirstDefinitionKind.DocumentType, "productPage")] =
                    "/umbraco/section/settings/workspace/document-type/edit/product-key",
            });

        // Assert
        result.DefinitionCount.ShouldBe(5);
        result.DataTypeCount.ShouldBe(1);
        result.DocumentTypeCount.ShouldBe(1);
        result.ElementTypeCount.ShouldBe(1);
        result.MediaTypeCount.ShouldBe(1);
        result.MemberTypeCount.ShouldBe(1);
        result.HasReport.ShouldBeTrue();
        result.SkippedCount.ShouldBe(3);
        result.Definitions.Select(item => item.Alias)
            .ShouldBe(["decimal", "productPage", "seoComposite", "imageAsset", "memberProfile"]);
        result.Definitions.Single(item => item.Alias == "productPage").ShouldSatisfyAllConditions(
            item => item.Status.ShouldBe(nameof(SynchronizationStatus.Created)),
            item => item.PropertyCount.ShouldBe(1),
            item => item.BackofficeUrl.ShouldBe(
                "/umbraco/section/settings/workspace/document-type/edit/product-key"),
            item => item.Properties.Single().ShouldSatisfyAllConditions(
                property => property.Alias.ShouldBe("price"),
                property => property.Description.ShouldBe("The product price."),
                property => property.GroupAlias.ShouldBe("pricing"),
                property => property.TabAlias.ShouldBe("commerce"),
                property => property.Mandatory.ShouldBeTrue(),
                property => property.ValidationExpression.ShouldBe("^[0-9]+$"),
                property => property.ValidationMessage.ShouldBe("Numbers only"),
                property => property.LabelOnTop.ShouldBeTrue()));
        result.Definitions.Single(item => item.Alias == "decimal").Status
            .ShouldBe(nameof(SynchronizationStatus.Skipped));
        result.Definitions.Single(item => item.Alias == "decimal").Message
            .ShouldBe("No synchronization result is available for this definition.");
        result.DataTypes.Single().ShouldSatisfyAllConditions(
            item => item.Alias.ShouldBe("decimal"),
            item => item.Folder.ShouldBe("/"),
            item => item.EditorAlias.ShouldBe("Umbraco.Decimal"),
            item => item.EditorUiAlias.ShouldBe("Umb.PropertyEditorUi.Decimal"));
        result.DocumentTypes.Single().Alias.ShouldBe("productPage");
        result.DocumentTypes.Single().Folder.ShouldBe("Commerce/Pages");
        result.ElementTypes.Single().ShouldSatisfyAllConditions(
            item => item.Alias.ShouldBe("seoComposite"),
            item => item.IsElement.ShouldBeTrue(),
            item => item.Folder.ShouldBe("Shared/Elements"),
            item => item.Status.ShouldBe(nameof(SynchronizationStatus.Failed)),
            item => item.Message.ShouldBe("Element synchronization failed."));
        result.MediaTypes.Single().ShouldSatisfyAllConditions(
            item => item.Alias.ShouldBe("imageAsset"),
            item => item.PropertyCount.ShouldBe(1));
        result.MemberTypes.Single().ShouldSatisfyAllConditions(
            item => item.Alias.ShouldBe("memberProfile"),
            item => item.PropertyCount.ShouldBe(1),
            item => item.Properties.Single().Mandatory.ShouldBeTrue());
    }

    [Fact]
    public void Map_NoReport_ExplainsMissingResult() {
        // Arrange
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DocumentType, "article", "Article", null, []),
        ]);

        // Act
        var result = CodeFirstStatusMapping.Map(registry, new SynchronizationReportStore());

        // Assert
        result.HasReport.ShouldBeFalse();
        result.SkippedCount.ShouldBe(0);
        result.Definitions.Single().Status.ShouldBe("Pending");
        result.Definitions.Single().Message.ShouldBe("No synchronization report is available yet.");
    }

    [Fact]
    public void Map_SkippedResult_PreservesStoreMessage() {
        // Arrange
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DocumentType, "article", "Article", null, []),
        ]);
        var reports = new SynchronizationReportStore();
        reports.Set(new SynchronizationReport(DateTimeOffset.UtcNow, [
            new(CodeFirstDefinitionKind.DocumentType, "article", "Article", SynchronizationStatus.Skipped, "Managed externally."),
        ]));

        // Act
        var result = CodeFirstStatusMapping.Map(registry, reports);

        // Assert
        result.SkippedCount.ShouldBe(1);
        result.Definitions.Single().Message.ShouldBe("Managed externally.");
    }

    [Fact]
    public void Map_DuplicateAliases_MapsEachResultWithoutCrashing() {
        // Arrange
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "codeEditor", "First", "Umbraco.Plain.String", []),
            new(CodeFirstDefinitionKind.DataType, "codeEditor", "Second", "Umbraco.Plain.String", []),
            new(CodeFirstDefinitionKind.DocumentType, "codeEditor", "Document", null, []),
        ]);
        var reports = new SynchronizationReportStore();
        reports.Set(new SynchronizationReport(DateTimeOffset.UtcNow, [
            new(CodeFirstDefinitionKind.DataType, "codeEditor", "Second", SynchronizationStatus.Failed, "Second failed."),
            new(CodeFirstDefinitionKind.DocumentType, "codeEditor", "Document", SynchronizationStatus.Created),
            new(CodeFirstDefinitionKind.DataType, "codeEditor", "First", SynchronizationStatus.Failed, "First failed."),
        ]));

        // Act
        var result = CodeFirstStatusMapping.Map(registry, reports);

        // Assert
        result.DataTypeCount.ShouldBe(2);
        result.FailedCount.ShouldBe(2);
        result.DataTypes.Single(item => item.Name == "First").Message.ShouldBe("First failed.");
        result.DataTypes.Single(item => item.Name == "Second").Message.ShouldBe("Second failed.");
        result.DocumentTypes.Single().Status.ShouldBe(nameof(SynchronizationStatus.Created));
    }

    [Fact]
    public void Map_DuplicateAliasesWithSameNameAndIncompleteReport_DoesNotReuseResult() {
        // Arrange
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "codeEditor", "Code Editor", "Umbraco.Plain.String", []),
            new(CodeFirstDefinitionKind.DataType, "codeEditor", "Code Editor", "Umbraco.Plain.String", []),
        ]);
        var reports = new SynchronizationReportStore();
        reports.Set(new SynchronizationReport(DateTimeOffset.UtcNow, [
            new(CodeFirstDefinitionKind.DataType, "codeEditor", "Code Editor", SynchronizationStatus.Failed, "Conflict."),
        ]));

        // Act
        var result = CodeFirstStatusMapping.Map(registry, reports);

        // Assert
        result.DataTypes.Select(item => item.Status).ShouldBe(["Failed", "Skipped"]);
        result.DataTypes[0].Message.ShouldBe("Conflict.");
        result.DataTypes[1].Message.ShouldBe("No synchronization result is available for this definition.");
    }

    [Fact]
    public void Map_OrchestrationError_ExposesErrorWithoutDefinitions() {
        // Arrange
        var reports = new SynchronizationReportStore();
        reports.Set(new SynchronizationReport(DateTimeOffset.UtcNow, []) { Error = "Coordination unavailable." });

        // Act
        var result = CodeFirstStatusMapping.Map(new TestRegistry([]), reports);

        // Assert
        result.HasReport.ShouldBeTrue();
        result.SynchronizationError.ShouldBe("Coordination unavailable.");
    }

    private sealed class TestRegistry(IReadOnlyList<CodeFirstDefinition> definitions)
        : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = definitions;
    }
}
