namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class PropertyOutsideDocumentTypeAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public void Analyze_PropertyOutsideDocumentType_ReportsContainingTypeDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            public partial class Product {
                [UCFProperty("name", "Name", "text")]
                public partial string Name { get; }
            }
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(
            diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.PropertyOutsideDocumentType);
    }
}
