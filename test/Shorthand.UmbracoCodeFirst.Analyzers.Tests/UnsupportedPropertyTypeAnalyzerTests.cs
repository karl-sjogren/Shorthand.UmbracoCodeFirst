using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.UnsupportedPropertyTypeAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class UnsupportedPropertyTypeAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task UnsupportedPropertyType_ReportsDiagnosticAsync() {
        await VerifyCS.VerifyAnalyzerAsync(
            Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("product", "Product")]
                public partial class Product {
                    [UCFProperty("value", "Value", "text")]
                    public void [|Value|] { get; }
                }
                """));
    }
}
