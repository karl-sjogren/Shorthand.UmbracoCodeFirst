namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class EditorAliasAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public void Analyze_UnknownEditorAlias_ReportsUnknownEditorDiagnostic() {
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDataType("customEditor", "Umbraco.MissingEditor")]
            public partial class CustomEditor;
            """;

        var diagnostics = Analyze(source, _editorMetadata);

        diagnostics.ShouldContain(
            diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.UnknownEditorAlias);
    }
}
