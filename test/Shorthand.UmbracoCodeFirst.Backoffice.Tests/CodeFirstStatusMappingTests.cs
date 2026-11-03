using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Tests;

public class CodeFirstStatusMappingTests {
    [Fact]
    public void Map_DefinitionsAndReportStatuses_OrdersDefinitionsAndUsesStatuses() {
        // Arrange
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DocumentType, "productPage", "Product Page", null, [new("price", "Price", "decimal")]),
            new(CodeFirstDefinitionKind.DataType, "decimal", "Decimal", "Umbraco.Decimal", []),
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
        CodeFirstStatusResponse result = CodeFirstStatusMapping.Map(registry, reports);

        // Assert
        result.DefinitionCount.ShouldBe(2);
        result.Definitions.Select(item => item.Alias).ShouldBe(["decimal", "productPage"]);
        result.Definitions.Single(item => item.Alias == "productPage").ShouldSatisfyAllConditions(
            item => item.Status.ShouldBe(nameof(SynchronizationStatus.Created)),
            item => item.PropertyCount.ShouldBe(1));
        result.Definitions.Single(item => item.Alias == "decimal").Status
            .ShouldBe(nameof(SynchronizationStatus.Skipped));
    }

    private sealed class TestRegistry(IReadOnlyList<CodeFirstDefinition> definitions)
        : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = definitions;
    }
}
