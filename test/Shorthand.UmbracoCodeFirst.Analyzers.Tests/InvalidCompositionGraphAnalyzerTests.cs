namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class InvalidCompositionGraphAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public void Analyze_SelfComposition_ReportsInvalidCompositionGraphDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocumentType("product", "Product")]
            [UCFComposite(typeof(Product))]
            public partial class Product;
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(
            diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.InvalidCompositionGraph);
    }
}
