using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.EditorAliasAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class EditorAliasAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task UnknownEditorAlias_ReportsDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDataType("customEditor", "Umbraco.MissingEditor")]
                public partial class [|CustomEditor|];
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source,
            [("umbraco-property-editors.json", EditorMetadata)]);
    }

    [Fact]
    public async Task KnownEditorAlias_DoesNotReportDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDataType("text", "Umbraco.TextBox")]
                public partial class TextDataType;
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source,
            [("umbraco-property-editors.json", EditorMetadata)]);
    }
}
