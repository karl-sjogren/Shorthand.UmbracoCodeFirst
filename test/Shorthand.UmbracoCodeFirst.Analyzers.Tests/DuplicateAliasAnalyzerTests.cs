using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.DuplicateAliasAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class DuplicateAliasAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task DuplicateDefinitionAliases_ReportsDiagnosticAsync() {
        await VerifyCS.VerifyAnalyzerAsync(
            Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("product", "Product")]
                public partial class Product;
                [[|UCFDataType("product", "Product editor", "text")|]]
                public partial class ProductEditor;
                """));
    }

    [Fact]
    public async Task DuplicateDerivedDefinitionAliases_ReportsDiagnosticAsync() {
        await VerifyCS.VerifyAnalyzerAsync(
            Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("Product Page")]
                public partial class Product;
                [[|UCFDataType("Product Page", "text")|]]
                public partial class ProductEditor;
                """));
    }

    [Fact]
    public async Task DuplicateDerivedPropertyAliases_ReportsDiagnosticAsync() {
        await VerifyCS.VerifyAnalyzerAsync(
            Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("Article")]
                public partial class Article {
                    [UCFProperty("Product Page", "text")]
                    public string First { get; } = null!;
                    [[|UCFProperty("ProductPage", "text")|]]
                    public string Second { get; } = null!;
                }
                """));
    }

    [Fact]
    public async Task SharedDataTypeAcrossComposedTypes_DoesNotReportDiagnosticAsync() {
        await VerifyCS.VerifyAnalyzerAsync(
            Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("base", "Base")]
                public partial class Base {
                    [UCFProperty("title", "Title", "text")]
                    public string Title { get; } = null!;
                }
                [UCFDocumentType("article", "Article")]
                [UCFComposite(typeof(Base))]
                public partial class Article {
                    [UCFProperty("summary", "Summary", "text")]
                    public string Summary { get; } = null!;
                }
                """));
    }

    [Fact]
    public async Task SameCompositionReusedByMultipleDocumentTypes_DoesNotReportDiagnosticAsync() {
        await VerifyCS.VerifyAnalyzerAsync(
            Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("seo", "SEO")]
                public partial class Seo {
                    [UCFProperty("description", "Description", "text")]
                    public string Description { get; } = null!;
                }
                [UCFDocumentType("article", "Article")]
                [UCFComposite(typeof(Seo))]
                public partial class Article;
                [UCFDocumentType("product", "Product")]
                [UCFComposite(typeof(Seo))]
                public partial class Product;
                """));
    }

    [Fact]
    public async Task PropertyAliasConflictingWithComposition_ReportsDiagnosticAsync() {
        await VerifyCS.VerifyAnalyzerAsync(
            Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("seo", "SEO")]
                public partial class Seo {
                    [UCFProperty("description", "Description", "text")]
                    public string Description { get; } = null!;
                }
                [UCFDocumentType("article", "Article")]
                [UCFComposite(typeof(Seo))]
                public partial class Article {
                    [[|UCFProperty("description", "Other description", "text")|]]
                    public string OtherDescription { get; } = null!;
                }
                """));
    }

    [Fact]
    public async Task DuplicatePropertyAliasesWithinDocumentType_ReportsDiagnosticAsync() {
        await VerifyCS.VerifyAnalyzerAsync(
            Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("article", "Article")]
                public partial class Article {
                    [UCFProperty("title", "Title", "text")]
                    public string Title { get; } = null!;
                    [[|UCFProperty("title", "Other title", "text")|]]
                    public string OtherTitle { get; } = null!;
                }
                """));
    }
}
