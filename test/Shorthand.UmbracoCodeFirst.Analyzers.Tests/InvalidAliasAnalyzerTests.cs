namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class InvalidAliasAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public void Analyze_InvalidAlias_ReportsInvalidAliasDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocumentType("invalid_alias", "Product")]
            public partial class Product;
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.InvalidAlias);
    }

    [Fact]
    public void Analyze_ValidAliasCharacters_DoesNotReportInvalidAliasDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocumentType("product-page.é", "Product")]
            public partial class Product;
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.InvalidAlias);
    }
}
