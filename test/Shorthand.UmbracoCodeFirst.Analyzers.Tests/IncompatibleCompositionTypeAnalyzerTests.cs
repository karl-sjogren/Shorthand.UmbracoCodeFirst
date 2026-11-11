using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.IncompatibleCompositionTypeAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class IncompatibleCompositionTypeAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task ElementComposingDocumentType_ReportsDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFElementType("shared")]
                [UCFComposite(typeof(Document))]
                public partial class Element;
                [UCFDocumentType("document")]
                public partial class Document;
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source,
            VerifyCS.Diagnostic(CodeFirstDiagnosticIds.IncompatibleCompositionType)
                .WithSpan(3, 2, 3, 32)
                .WithArguments("Element", "Document"));
    }

    [Fact]
    public async Task DocumentComposingElementType_ReportsDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("document")]
                [UCFComposite(typeof(Element))]
                public partial class Document;
                [UCFElementType("shared")]
                public partial class Element;
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source,
            VerifyCS.Diagnostic(CodeFirstDiagnosticIds.IncompatibleCompositionType)
                .WithSpan(3, 2, 3, 31)
                .WithArguments("Document", "Element"));
    }

    [Fact]
    public async Task MatchingCompositionKinds_DoesNotReportDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFElementType("base")]
                public partial class Base;
                [UCFElementType("derived")]
                [UCFComposite(typeof(Base))]
                public partial class Derived;
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source);
    }
}
