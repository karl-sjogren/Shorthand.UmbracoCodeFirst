namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class RepeatedLayoutAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public void Analyze_RepeatedGroupAlias_ReportsSharedLayoutDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocumentType("product", "Product")]
            public partial class Product {
                [UCFProperty("name", "Name", "text")]
                [UCFGroup("details", "Details")]
                public partial string Name { get; }
                [UCFProperty("sku", "SKU", "text")]
                [UCFGroup("details", "Details")]
                public partial string Sku { get; }
            }
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.DuplicateLayout);
    }

    [Fact]
    public async Task CodeFix_RepeatedGroupAlias_CreatesDerivedAttributeAndReplacesUsagesAsync() {
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocumentType("product", "Product")]
            public partial class Product {
                [UCFGroup("details", "Details")]
                public partial string Name { get; }
                [UCFGroup("details", "Details")]
                public partial string Sku { get; }
            }
            """;

        var fixedSource = await ApplyCodeFixAsync(source);

        fixedSource.ShouldContain("public sealed class DetailsGroupAttribute : UCFGroupAttribute");
        fixedSource.ShouldContain("public DetailsGroupAttribute() : base(\"details\", \"Details\")");
        fixedSource.ShouldContain("[DetailsGroupAttribute]");
        fixedSource.ShouldNotContain("[UCFGroup(\"details\", \"Details\")]");
    }

    [Fact]
    public async Task CodeFix_RepeatedTabAliasWithSortOrder_CreatesDerivedAttributeAndReplacesUsagesAsync() {
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocumentType("product", "Product")]
            public partial class Product {
                [UCFTab("Details", 1)]
                public partial string Name { get; }
                [UCFTab("Details", 1)]
                public partial string Sku { get; }
            }
            """;

        var fixedSource = await ApplyCodeFixAsync(source);

        fixedSource.ShouldContain("public sealed class DetailsTabAttribute : UCFTabAttribute");
        fixedSource.ShouldContain("public DetailsTabAttribute() : base(\"Details\", 1)");
        fixedSource.ShouldContain("[DetailsTabAttribute]");
        fixedSource.ShouldNotContain("[UCFTab(\"Details\", 1)]");
    }
}
