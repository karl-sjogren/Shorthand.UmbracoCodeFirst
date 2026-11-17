using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.DefaultTemplateNotAllowedAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class DefaultTemplateNotAllowedAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task MissingDefaultFromAllowedTemplates_ReportsDiagnosticAsync() {
        // Arrange
        var source = Source("""
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocument("Landing", DefaultTemplateAlias = {|#0:"landingPage"|})]
            public class Missing;
            [UCFDocument("Landing", DefaultTemplateAlias = {|#1:"landingPage"|}, AllowedTemplateAliases = new string[0])]
            public class Empty;
            [UCFDocument("Landing", AllowedTemplateAliases = new[] { "other" }, DefaultTemplateAlias = {|#2:"landingPage"|})]
            public class Different;
            """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source,
            VerifyCS.Diagnostic(CodeFirstDiagnosticIds.DefaultTemplateNotAllowed).WithLocation(0).WithArguments("landingPage"),
            VerifyCS.Diagnostic(CodeFirstDiagnosticIds.DefaultTemplateNotAllowed).WithLocation(1).WithArguments("landingPage"),
            VerifyCS.Diagnostic(CodeFirstDiagnosticIds.DefaultTemplateNotAllowed).WithLocation(2).WithArguments("landingPage"));
    }

    [Fact]
    public async Task AllowedDefaultAndNoDefault_DoNotReportDiagnosticsAsync() {
        // Arrange
        var source = Source("""
            using Shorthand.UmbracoCodeFirst.Attributes;
            public static class Templates {
                public const string Landing = "landingPage";
            }
            [UCFDocument("Landing", DefaultTemplateAlias = Templates.Landing,
                AllowedTemplateAliases = new[] { "other", Templates.Landing })]
            public class Matching;
            [UCFDocument("Landing", DefaultTemplateAlias = "LandingPage",
                AllowedTemplateAliases = new[] { "landingPage" })]
            public class DifferentCase;
            [UCFDocument("Landing", AllowedTemplateAliases = new[] { "other" })]
            public class Unset;
            [UCFDocument("Landing", DefaultTemplateAlias = null)]
            public class Null;
            [Other.UCFDocument(DefaultTemplateAlias = "missing")]
            public class Unrelated;
            namespace Other {
                public class UCFDocumentAttribute : global::System.Attribute {
                    public string? DefaultTemplateAlias { get; set; }
                }
            }
            """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NullAllowedTemplates_ReportsDiagnosticAsync() {
        // Arrange
        var source = Source("""
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocument("Landing", DefaultTemplateAlias = {|#0:"landingPage"|}, AllowedTemplateAliases = null!)]
            public class Page;
            """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source,
            VerifyCS.Diagnostic(CodeFirstDiagnosticIds.DefaultTemplateNotAllowed).WithLocation(0).WithArguments("landingPage"));
    }
}
