using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.IncompatibleEditorTypeAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class IncompatibleEditorTypeAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task StringEnumerableInterface_IsCompatibleWithStringCollectionEditorAsync() {
        // Arrange
        const string metadata = """
            {"editors":[{"alias":"Umbraco.Tags","returnType":"System.Collections.Generic.IEnumerable<string>"}]}
            """;
        var source = Source("""
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDataType("tags", "Umbraco.Tags")]
            public partial class TagsEditor;
            [UCFDocument("article", "Article")]
            public partial class Article {
                [UCFProperty("tags", "tags")]
                public System.Collections.Generic.IEnumerable<string> Tags { get; } = null!;
            }
            """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, [("umbraco-property-editors.json", metadata)]);
    }

    [Fact]
    public async Task KnownEditorAndCompatiblePropertyType_DoesNotReportDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDataType("textEditor", "Umbraco.TextBox")]
                public partial class TextEditor;
                [UCFDocument("article", "Article")]
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
                [UCFDocument("article", "Article")]
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
