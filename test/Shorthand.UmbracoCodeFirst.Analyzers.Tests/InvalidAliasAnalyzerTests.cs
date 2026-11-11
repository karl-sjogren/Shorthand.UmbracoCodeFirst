using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.InvalidAliasAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class InvalidAliasAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task InvalidAlias_ReportsDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [[|UCFDocumentType("invalid_alias", "Product")|]]
                public partial class Product;
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source);
    }

    [Fact]
    public async Task ValidAliasCharacters_DoNotReportDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("product-page.é", "Product")]
                public partial class Product;
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source);
    }
}
