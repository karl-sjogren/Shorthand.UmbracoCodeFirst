using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.RepeatedValidationAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class RepeatedValidationAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task RepeatedValidationConfiguration_ReportsDiagnosticAsync() {
        await VerifyCS.VerifyAnalyzerAsync(
            Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("product", "Product")]
                public partial class Product {
                    [UCFProperty("name", "Name", "text")]
                    [UCFValidation("^[A-Z]+$", "Uppercase only")]
                    public string Name { get; } = null!;
                    [UCFProperty("sku", "SKU", "text")]
                    [[|UCFValidation("^[A-Z]+$", "Uppercase only")|]]
                    public string Sku { get; } = null!;
                }
                """));
    }
}
