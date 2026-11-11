using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.UnsupportedPropertyTypeAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class UnsupportedPropertyTypeAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task UnsupportedPropertyType_ReportsDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("product", "Product")]
                public partial class Product {
                    [UCFProperty("value", "Value", "text")]
                    public void [|Value|] { get; }
                }
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source);
    }
}
