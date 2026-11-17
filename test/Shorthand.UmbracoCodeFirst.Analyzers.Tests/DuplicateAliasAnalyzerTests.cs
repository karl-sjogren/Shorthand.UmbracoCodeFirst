using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.DuplicateAliasAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class DuplicateAliasAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task SameGroupAliasInDifferentTabs_DoesNotReportDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocument("Page")]
                public partial class Page {
                    [UCFTab("First")]
                    [UCFGroup("details", "First details")]
                    public string First { get; } = null!;
                    [UCFTab("Second")]
                    [UCFGroup("details", "Second details")]
                    public string Second { get; } = null!;
                    [UCFGroup("details", "Root details")]
                    public string Root { get; } = null!;
                }
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ConflictingGroupMetadataInSameTab_ReportsDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocument("Page")]
                public partial class Page {
                    [UCFTab("Content")]
                    [UCFGroup("details", "Details")]
                    public string First { get; } = null!;
                    [UCFTab("Content")]
                    [[|UCFGroup("details", "Other details")|]]
                    public string Second { get; } = null!;
                }
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DerivedAndDirectTabAliases_ScopeConflictsTogetherAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                public sealed class ContentTabAttribute() : UCFTabAttribute("content", "Content");
                [UCFDocument("Page")]
                public partial class Page {
                    [ContentTab]
                    [UCFGroup("details", "Details")]
                    public string First { get; } = null!;
                    [UCFTab("content", "Content")]
                    [[|UCFGroup("details", "Other details")|]]
                    public string Second { get; } = null!;
                }
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DuplicateDefinitionAliases_ReportsDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocument("product", "Product")]
                public partial class Product;
                [[|UCFDataType("product", "Product editor", "text")|]]
                public partial class ProductEditor;
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source);
    }

    [Fact]
    public async Task DuplicateDerivedDefinitionAliases_ReportsDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocument("Product Page")]
                public partial class Product;
                [[|UCFDataType("Product Page", "text")|]]
                public partial class ProductEditor;
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source);
    }

    [Theory]
    [InlineData("UCFDataType(\"Code Editor\", typeof(Editor), typeof(Configuration))")]
    [InlineData("UCFDataType(\"Different\", \"schema\", \"ui\", Alias = \"codeEditor\")")]
    public async Task DuplicateDataTypeAliases_WithCurrentConstructors_ReportsDiagnosticAsync(string secondAttribute) {
        // Arrange
        var source = $$"""
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDataType("Code Editor", typeof(Editor), typeof(Configuration))]
            public partial class First;
            [[|{{secondAttribute}}|]]
            public partial class Second;
            public class Editor;
            public class Configuration;
            namespace Shorthand.UmbracoCodeFirst.Attributes {
                public sealed class UCFDataTypeAttribute : global::System.Attribute {
                    public UCFDataTypeAttribute(string name, global::System.Type editor, global::System.Type configuration) { }
                    public UCFDataTypeAttribute(string name, string editor, string ui) { }
                    public string? Alias { get; set; }
                }
            }
            """;

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DataTypeAliases_WithDistinctNamedAliases_DoNotReportDiagnosticAsync() {
        // Arrange
        var source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDataType("Code Editor", typeof(Editor), typeof(Configuration), Alias = "first")]
            public partial class First;
            [UCFDataType("Code Editor", typeof(Editor), typeof(Configuration), Alias = "second")]
            public partial class Second;
            public class Editor;
            public class Configuration;
            namespace Shorthand.UmbracoCodeFirst.Attributes {
                public sealed class UCFDataTypeAttribute : global::System.Attribute {
                    public UCFDataTypeAttribute(string name, global::System.Type editor, global::System.Type configuration) { }
                    public string? Alias { get; set; }
                }
            }
            """;

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DuplicateDerivedPropertyAliases_ReportsDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocument("Article")]
                public partial class Article {
                    [UCFProperty("Product Page", "text")]
                    public string First { get; } = null!;
                    [[|UCFProperty("ProductPage", "text")|]]
                    public string Second { get; } = null!;
                }
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source);
    }

    [Fact]
    public async Task SharedDataTypeAcrossComposedTypes_DoesNotReportDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocument("base", "Base")]
                public partial class Base {
                    [UCFProperty("title", "Title", "text")]
                    public string Title { get; } = null!;
                }
                [UCFDocument("article", "Article")]
                [UCFComposite(typeof(Base))]
                public partial class Article {
                    [UCFProperty("summary", "Summary", "text")]
                    public string Summary { get; } = null!;
                }
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source);
    }

    [Fact]
    public async Task SameCompositionReusedByMultipleDocumentTypes_DoesNotReportDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocument("seo", "SEO")]
                public partial class Seo {
                    [UCFProperty("description", "Description", "text")]
                    public string Description { get; } = null!;
                }
                [UCFDocument("article", "Article")]
                [UCFComposite(typeof(Seo))]
                public partial class Article;
                [UCFDocument("product", "Product")]
                [UCFComposite(typeof(Seo))]
                public partial class Product;
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source);
    }

    [Fact]
    public async Task PropertyAliasConflictingWithComposition_ReportsDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocument("seo", "SEO")]
                public partial class Seo {
                    [UCFProperty("description", "Description", "text")]
                    public string Description { get; } = null!;
                }
                [UCFDocument("article", "Article")]
                [UCFComposite(typeof(Seo))]
                public partial class Article {
                    [[|UCFProperty("description", "Other description", "text")|]]
                    public string OtherDescription { get; } = null!;
                }
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source);
    }

    [Fact]
    public async Task DuplicatePropertyAliasesWithinDocumentType_ReportsDiagnosticAsync() {
        // Arrange
        var source = Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocument("article", "Article")]
                public partial class Article {
                    [UCFProperty("title", "Title", "text")]
                    public string Title { get; } = null!;
                    [[|UCFProperty("title", "Other title", "text")|]]
                    public string OtherTitle { get; } = null!;
                }
                """);

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(
            source);
    }
}
