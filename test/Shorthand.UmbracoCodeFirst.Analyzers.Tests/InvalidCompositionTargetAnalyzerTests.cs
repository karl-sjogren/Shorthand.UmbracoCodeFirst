namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class InvalidCompositionTargetAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public void Analyze_CompositionTargetWithoutDocumentType_ReportsInvalidTargetDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocumentType("product", "Product")]
            [UCFComposite(typeof(NotADocumentType))]
            public partial class Product;
            public partial class NotADocumentType;
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(
            diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.InvalidCompositionTarget);
    }
}
