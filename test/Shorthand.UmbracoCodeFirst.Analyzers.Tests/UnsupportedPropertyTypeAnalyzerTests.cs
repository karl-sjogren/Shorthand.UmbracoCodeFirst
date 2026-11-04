namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class UnsupportedPropertyTypeAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public void Analyze_UnsupportedPropertyType_ReportsUnsupportedPropertyDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocumentType("product", "Product")]
            public partial class Product {
                [UCFProperty("value", "Value", "text")]
                public partial void Value { get; }
            }
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(
            diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.UnsupportedPropertyType);
    }
}
