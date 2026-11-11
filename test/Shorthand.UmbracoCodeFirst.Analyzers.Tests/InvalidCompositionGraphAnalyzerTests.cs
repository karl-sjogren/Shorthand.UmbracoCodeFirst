using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.InvalidCompositionGraphAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class InvalidCompositionGraphAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task SelfComposition_ReportsDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("product", "Product")]
                [[|UCFComposite(typeof(Product))|]]
                public partial class Product;
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source,
            VerifyCS.Diagnostic(CodeFirstDiagnosticIds.InvalidCompositionGraph)
                .WithSpan(3, 2, 3, 31)
                .WithArguments("Product", "Product"));
    }
}
