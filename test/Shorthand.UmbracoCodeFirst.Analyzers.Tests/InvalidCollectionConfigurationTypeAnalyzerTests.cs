using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.InvalidCollectionConfigurationTypeAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class InvalidCollectionConfigurationTypeAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task UnrelatedTypes_ReportDiagnosticsAsync() {
        // Arrange
        var source = Source("""
            using Shorthand.UmbracoCodeFirst.Attributes;
            using Shorthand.UmbracoCodeFirst.Metadata;
            [UCFCollection({|#0:typeof(string)|})]
            public class Page;
            [UCFCollection({|#1:typeof(int[])|})]
            public class Media;
            [UCFCollection({|#2:typeof(CodeFirstCollectionConfiguration)|})]
            public class Other;
            [UCFCollection({|#3:typeof(Impostor.CodeFirstCollectionConfiguration)|})]
            public class Another;
            namespace Impostor { public class CodeFirstCollectionConfiguration; }
            """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source,
            VerifyCS.Diagnostic(CodeFirstDiagnosticIds.InvalidCollectionConfigurationType).WithLocation(0).WithArguments("string"),
            VerifyCS.Diagnostic(CodeFirstDiagnosticIds.InvalidCollectionConfigurationType).WithLocation(1).WithArguments("int[]"),
            VerifyCS.Diagnostic(CodeFirstDiagnosticIds.InvalidCollectionConfigurationType).WithLocation(2)
                .WithArguments("Shorthand.UmbracoCodeFirst.Metadata.CodeFirstCollectionConfiguration"),
            VerifyCS.Diagnostic(CodeFirstDiagnosticIds.InvalidCollectionConfigurationType).WithLocation(3)
                .WithArguments("Impostor.CodeFirstCollectionConfiguration"));
    }

    [Fact]
    public async Task DirectAndIndirectSubclasses_DoNotReportDiagnosticsAsync() {
        // Arrange
        var source = Source("""
            using Shorthand.UmbracoCodeFirst.Attributes;
            using Shorthand.UmbracoCodeFirst.Metadata;
            public class Direct : CodeFirstCollectionConfiguration;
            public class Indirect : Direct;
            [UCFCollection(typeof(Direct))]
            public class Page;
            [UCFCollection(collectionConfiguration: typeof(Indirect))]
            public class Media;
            """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task GenericConfigurationSubclass_DoesNotReportDiagnosticAsync() {
        // Arrange
        var source = Source("""
            using Shorthand.UmbracoCodeFirst.Attributes;
            using Shorthand.UmbracoCodeFirst.Metadata;
            namespace Shorthand.UmbracoCodeFirst.Metadata {
                public abstract class CodeFirstCollectionConfiguration<T> : CodeFirstCollectionConfiguration;
            }
            public class GenericCollection : CodeFirstCollectionConfiguration<Page>;
            [UCFCollection(typeof(GenericCollection))]
            public class Page;
            """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task UnrelatedAttribute_IsIgnoredAsync() {
        // Arrange
        var source = Source("""
            [Other.UCFCollection(typeof(string))]
            public class Page;
            namespace Other {
                public class UCFCollectionAttribute(global::System.Type type) : global::System.Attribute;
            }
            """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
