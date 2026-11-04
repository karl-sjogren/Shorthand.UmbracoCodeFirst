namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class IncompatibleEditorTypeAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public void Analyze_KnownEditorAndCompatiblePropertyType_DoesNotReportEditorDiagnostic() {
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDataType("textEditor", "Umbraco.TextBox")]
            public partial class TextEditor;
            [UCFDocumentType("article", "Article")]
            public partial class Article {
                [UCFProperty("title", "textEditor")]
                public partial string Title { get; }
            }
            """;

        var diagnostics = Analyze(source, _editorMetadata);

        diagnostics.ShouldNotContain(diagnostic =>
            diagnostic.Id == CodeFirstDiagnosticIds.UnknownEditorAlias
            || diagnostic.Id == CodeFirstDiagnosticIds.IncompatibleEditorType);
    }

    [Fact]
    public void Analyze_IncompatibleEditorPropertyType_ReportsEditorTypeDiagnostic() {
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDataType("textEditor", "Umbraco.TextBox")]
            public partial class TextEditor;
            [UCFDocumentType("article", "Article")]
            public partial class Article {
                [UCFProperty("title", "textEditor")]
                public partial int Title { get; }
            }
            """;

        var diagnostics = Analyze(source, _editorMetadata);

        diagnostics.ShouldContain(
            diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.IncompatibleEditorType);
    }
}
