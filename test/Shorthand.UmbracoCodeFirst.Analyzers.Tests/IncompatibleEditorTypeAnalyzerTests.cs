using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.IncompatibleEditorTypeAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class IncompatibleEditorTypeAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task KnownEditorAndCompatiblePropertyType_DoesNotReportDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDataType("textEditor", "Umbraco.TextBox")]
                public partial class TextEditor;
                [UCFDocumentType("article", "Article")]
                public partial class Article {
                    [UCFProperty("title", "textEditor")]
                    public string Title { get; } = null!;
                }
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source,
            [("umbraco-property-editors.json", EditorMetadata)]);
    }

    [Fact]
    public async Task IncompatibleEditorPropertyType_ReportsDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDataType("textEditor", "Umbraco.TextBox")]
                public partial class TextEditor;
                [UCFDocumentType("article", "Article")]
                public partial class Article {
                    [UCFProperty("title", "textEditor")]
                    public int [|Title|] { get; }
                }
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source,
            [("umbraco-property-editors.json", EditorMetadata)]);
    }
}
