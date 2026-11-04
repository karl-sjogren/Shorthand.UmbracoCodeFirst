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
                    Mandatory: true)]),
            new(
                CodeFirstDefinitionKind.DataType,
                "decimal",
                "Decimal",
                "Umbraco.Decimal",
                [],
                EditorUiAlias: "Umb.PropertyEditorUi.Decimal"),
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
        result.DefinitionCount.ShouldBe(2);
        result.DataTypeCount.ShouldBe(1);
        result.DocumentTypeCount.ShouldBe(1);
        result.Definitions.Select(item => item.Alias).ShouldBe(["decimal", "productPage"]);
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
                property => property.Mandatory.ShouldBeTrue()));
        result.Definitions.Single(item => item.Alias == "decimal").Status
            .ShouldBe(nameof(SynchronizationStatus.Skipped));
        result.DataTypes.Single().ShouldSatisfyAllConditions(
            item => item.Alias.ShouldBe("decimal"),
            item => item.EditorAlias.ShouldBe("Umbraco.Decimal"),
            item => item.EditorUiAlias.ShouldBe("Umb.PropertyEditorUi.Decimal"));
        result.DocumentTypes.Single().Alias.ShouldBe("productPage");
    }

    private sealed class TestRegistry(IReadOnlyList<CodeFirstDefinition> definitions)
        : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = definitions;
    }
}
