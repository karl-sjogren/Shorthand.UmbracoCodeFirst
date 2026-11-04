namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class DuplicateAliasAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public void Analyze_DuplicateDefinitionAliases_ReportsDuplicateAliasDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocumentType("product", "Product")]
            public partial class Product;
            [UCFDataType("product", "Product editor", "text")]
            public partial class ProductEditor;
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.DuplicateAlias);
    }

    [Fact]
    public void Analyze_DuplicateDerivedDefinitionAliases_ReportsDuplicateAliasDiagnostic() {
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocumentType("Product Page")]
            public partial class Product;
            [UCFDataType("Product Page", "text")]
            public partial class ProductEditor;
            """;

        var diagnostics = Analyze(source);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.DuplicateAlias);
    }

    [Fact]
    public void Analyze_DuplicateDerivedPropertyAliases_ReportsDuplicateAliasDiagnostic() {
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocumentType("Article")]
            public partial class Article {
                [UCFProperty("Product Page", "text")]
                public partial string First { get; }
                [UCFProperty("ProductPage", "text")]
                public partial string Second { get; }
            }
            """;

        var diagnostics = Analyze(source);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.DuplicateAlias);
    }

    [Fact]
    public void Analyze_SharedDataTypeAcrossComposedTypes_DoesNotReportDuplicatePropertyAliasDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocumentType("base", "Base")]
            public partial class Base {
                [UCFProperty("title", "Title", "text")]
                public partial string Title { get; }
            }
            [UCFDocumentType("article", "Article")]
            [UCFComposite(typeof(Base))]
            public partial class Article {
                [UCFProperty("summary", "Summary", "text")]
                public partial string Summary { get; }
            }
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldNotContain(
            diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.DuplicateAlias);
    }

    [Fact]
    public void Analyze_SameCompositionReusedByMultipleDocumentTypes_DoesNotReportDuplicatePropertyAliasDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocumentType("seo", "SEO")]
            public partial class Seo {
                [UCFProperty("description", "Description", "text")]
                public partial string Description { get; }
            }
            [UCFDocumentType("article", "Article")]
            [UCFComposite(typeof(Seo))]
            public partial class Article;
            [UCFDocumentType("product", "Product")]
            [UCFComposite(typeof(Seo))]
            public partial class Product;
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldNotContain(
            diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.DuplicateAlias);
    }

    [Fact]
    public void Analyze_PropertyAliasConflictingWithComposition_ReportsDuplicateAliasDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocumentType("seo", "SEO")]
            public partial class Seo {
                [UCFProperty("description", "Description", "text")]
                public partial string Description { get; }
            }
            [UCFDocumentType("article", "Article")]
            [UCFComposite(typeof(Seo))]
            public partial class Article {
                [UCFProperty("description", "Other description", "text")]
                public partial string OtherDescription { get; }
            }
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(
            diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.DuplicateAlias);
    }

    [Fact]
    public void Analyze_DuplicatePropertyAliasesWithinDocumentType_ReportsDuplicateAliasDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocumentType("article", "Article")]
            public partial class Article {
                [UCFProperty("title", "Title", "text")]
                public partial string Title { get; }
                [UCFProperty("title", "Other title", "text")]
                public partial string OtherTitle { get; }
            }
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(
            diagnostic => diagnostic.Id == CodeFirstDiagnosticIds.DuplicateAlias);
    }
}
