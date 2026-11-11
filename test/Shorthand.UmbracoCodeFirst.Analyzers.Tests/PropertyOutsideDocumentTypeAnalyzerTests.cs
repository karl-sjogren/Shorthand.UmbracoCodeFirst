using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.PropertyOutsideDocumentTypeAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class PropertyOutsideDocumentTypeAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task PropertyOutsideDocumentType_ReportsDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                public partial class Product {
                    [UCFProperty("name", "Name", "text")]
                    public string? [|Name|] { get; }
                }
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source);
    }
}
