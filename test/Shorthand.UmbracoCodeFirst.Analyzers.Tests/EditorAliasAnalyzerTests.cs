using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.EditorAliasAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class EditorAliasAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task UnknownEditorAlias_ReportsDiagnosticAsync() {
        await VerifyCS.VerifyAnalyzerAsync(
            Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDataType("customEditor", "Umbraco.MissingEditor")]
                public partial class [|CustomEditor|];
                """),
            [("umbraco-property-editors.json", EditorMetadata)]);
    }
}
