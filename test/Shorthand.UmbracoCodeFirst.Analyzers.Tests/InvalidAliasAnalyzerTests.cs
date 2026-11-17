using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.InvalidAliasAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class InvalidAliasAnalyzerTests : AnalyzerTestBase {
    [Theory]
    [InlineData("UCFDocument")]
    [InlineData("UCFMedia")]
    [InlineData("UCFMember")]
    public async Task InvalidAlias_ReportsDiagnosticAsync(string attributeName) {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [[|ATTRIBUTE("invalid_alias", "Product")|]]
                public partial class Product;
                """.Replace("ATTRIBUTE", attributeName, StringComparison.Ordinal));

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source);
    }

    [Fact]
    public async Task ValidAliasCharacters_DoNotReportDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocument("product-page.é", "Product")]
                public partial class Product;
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source);
    }
}
