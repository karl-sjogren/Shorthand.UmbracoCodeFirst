using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.InvalidCompositionTargetAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class InvalidCompositionTargetAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task CompositionTargetWithoutDocumentType_ReportsDiagnosticAsync() {
        await VerifyCS.VerifyAnalyzerAsync(
            Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("product", "Product")]
                [[|UCFComposite(typeof(NotADocumentType))|]]
                public partial class Product;
                public partial class NotADocumentType;
                """));
    }
}
