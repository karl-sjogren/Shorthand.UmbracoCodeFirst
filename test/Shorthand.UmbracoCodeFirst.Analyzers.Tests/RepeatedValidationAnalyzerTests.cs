namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class RepeatedValidationAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public void Analyze_RepeatedValidationConfiguration_ReportsSharedValidationDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocumentType("product", "Product")]
            public partial class Product {
                [UCFProperty("name", "Name", "text")]
                [UCFValidation("^[A-Z]+$", "Uppercase only")]
                public partial string Name { get; }
                [UCFProperty("sku", "SKU", "text")]
                [UCFValidation("^[A-Z]+$", "Uppercase only")]
                public partial string Sku { get; }
            }
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.DuplicateValidation);
    }
}
