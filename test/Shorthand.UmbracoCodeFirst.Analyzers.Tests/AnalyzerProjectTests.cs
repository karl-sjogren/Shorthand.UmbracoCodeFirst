using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class AnalyzerProjectTests {
    [Fact]
    public void AnalyzerDescriptors_HaveDocumentationLinks_ReturnFeatureBranchUris() {
        // Arrange
        const string documentationBaseUrl =
            "https://github.com/karl-sjogren/Shorthand.UmbracoCodeFirst/blob/" +
            "feature/initial-implementation/docs/analyzers";
        var descriptors = new CodeFirstAttributeAnalyzer()
            .SupportedDiagnostics
            .ToArray();

        // Act
        var links = descriptors.ToDictionary(
            descriptor => descriptor.Id,
            descriptor => descriptor.HelpLinkUri);

        // Assert
        links.ShouldBe(new Dictionary<string, string> {
            ["UCF100"] = $"{documentationBaseUrl}/UCF100.md",
            ["UCF101"] = $"{documentationBaseUrl}/UCF101.md",
            ["UCF102"] = $"{documentationBaseUrl}/UCF102.md",
            ["UCF103"] = $"{documentationBaseUrl}/UCF103.md",
            ["UCF104"] = $"{documentationBaseUrl}/UCF104.md",
            ["UCF105"] = $"{documentationBaseUrl}/UCF105.md",
            ["UCF106"] = $"{documentationBaseUrl}/UCF106.md",
            ["UCF107"] = $"{documentationBaseUrl}/UCF107.md",
            ["UCF108"] = $"{documentationBaseUrl}/UCF108.md",
            ["UCF109"] = $"{documentationBaseUrl}/UCF109.md",
        });
    }

    [Fact]
    public void AnalyzerAssembly_LoadProjectAssembly_LoadsSuccessfully() {
        // Arrange
        const string expectedName = "Shorthand.UmbracoCodeFirst.Analyzers.Tests";

        // Act
        var assembly = typeof(AnalyzerProjectTests).Assembly;

        // Assert
        assembly.GetName().Name.ShouldBe(expectedName);
    }

    [Fact]
    public void Analyze_InvalidAlias_ReportsInvalidAliasDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFContentType("invalid_alias", "Product")]
            public partial class Product;
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.InvalidAliasId);
    }

    [Fact]
    public void Analyze_ValidAliasCharacters_DoesNotReportInvalidAliasDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFContentType("product-page.é", "Product")]
            public partial class Product;
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.InvalidAliasId);
    }

    [Fact]
    public void Analyze_DuplicateDefinitionAliases_ReportsDuplicateAliasDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFContentType("product", "Product")]
            public partial class Product;
            [UCFDataType("product", "Product editor", "text")]
            public partial class ProductEditor;
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.DuplicateAliasId);
    }

    [Fact]
    public void Analyze_DuplicateDerivedDefinitionAliases_ReportsDuplicateAliasDiagnostic() {
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFContentType("Product Page")]
            public partial class Product;
            [UCFDataType("Product Page", "text")]
            public partial class ProductEditor;
            """;

        var diagnostics = Analyze(source);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.DuplicateAliasId);
    }

    [Fact]
    public void Analyze_DuplicateDerivedPropertyAliases_ReportsDuplicateAliasDiagnostic() {
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFContentType("Article")]
            public partial class Article {
                [UCFProperty("Product Page", "text")]
                public partial string First { get; }
                [UCFProperty("ProductPage", "text")]
                public partial string Second { get; }
            }
            """;

        var diagnostics = Analyze(source);

        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.DuplicateAliasId);
    }

    [Fact]
    public void Analyze_SharedDataTypeAcrossComposedTypes_DoesNotReportDuplicatePropertyAliasDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFContentType("base", "Base")]
            public partial class Base {
                [UCFProperty("title", "Title", "text")]
                public partial string Title { get; }
            }
            [UCFContentType("article", "Article")]
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
            diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.DuplicateAliasId);
    }

    [Fact]
    public void Analyze_SameCompositionReusedByMultipleContentTypes_DoesNotReportDuplicatePropertyAliasDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFContentType("seo", "SEO")]
            public partial class Seo {
                [UCFProperty("description", "Description", "text")]
                public partial string Description { get; }
            }
            [UCFContentType("article", "Article")]
            [UCFComposite(typeof(Seo))]
            public partial class Article;
            [UCFContentType("product", "Product")]
            [UCFComposite(typeof(Seo))]
            public partial class Product;
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldNotContain(
            diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.DuplicateAliasId);
    }

    [Fact]
    public void Analyze_PropertyAliasConflictingWithComposition_ReportsDuplicateAliasDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFContentType("seo", "SEO")]
            public partial class Seo {
                [UCFProperty("description", "Description", "text")]
                public partial string Description { get; }
            }
            [UCFContentType("article", "Article")]
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
            diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.DuplicateAliasId);
    }

    [Fact]
    public void Analyze_DuplicatePropertyAliasesWithinContentType_ReportsDuplicateAliasDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFContentType("article", "Article")]
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
            diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.DuplicateAliasId);
    }

    [Fact]
    public void Analyze_RepeatedGroupAlias_ReportsSharedLayoutDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFContentType("product", "Product")]
            public partial class Product {
                [UCFProperty("name", "Name", "text")]
                [UCFGroup("details", "Details")]
                public partial string Name { get; }
                [UCFProperty("sku", "SKU", "text")]
                [UCFGroup("details", "Details")]
                public partial string Sku { get; }
            }
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.DuplicateLayoutId);
    }

    [Fact]
    public void Analyze_RepeatedValidationConfiguration_ReportsSharedValidationDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFContentType("product", "Product")]
            public partial class Product {
                [UCFProperty("name", "Name", "text")]
                [UCFValidation("^[A-Z]+$", "Uppercase only")]
                public partial string Name { get; }
                [UCFProperty("sku", "SKU", "text")]
                [UCFValidation("^[A-Z]+$", "Uppercase only")]
                public partial string Sku { get; }
            }
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.DuplicateValidationId);
    }

    [Fact]
    public void Analyze_UnsupportedPropertyType_ReportsUnsupportedPropertyDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFContentType("product", "Product")]
            public partial class Product {
                [UCFProperty("value", "Value", "text")]
                public partial void Value { get; }
            }
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(
            diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.UnsupportedPropertyTypeId);
    }

    [Fact]
    public void Analyze_PropertyOutsideContentType_ReportsContainingTypeDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            public partial class Product {
                [UCFProperty("name", "Name", "text")]
                public partial string Name { get; }
            }
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(
            diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.PropertyOutsideContentTypeId);
    }

    [Fact]
    public void Analyze_CompositionTargetWithoutContentType_ReportsInvalidTargetDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFContentType("product", "Product")]
            [UCFComposite(typeof(NotAContentType))]
            public partial class Product;
            public partial class NotAContentType;
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(
            diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.InvalidCompositionTargetId);
    }

    [Fact]
    public void Analyze_SelfComposition_ReportsInvalidCompositionGraphDiagnostic() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFContentType("product", "Product")]
            [UCFComposite(typeof(Product))]
            public partial class Product;
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(
            diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.InvalidCompositionGraphId);
    }

    [Fact]
    public void Analyze_KnownEditorAndCompatiblePropertyType_DoesNotReportEditorDiagnostic() {
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDataType("textEditor", "Umbraco.TextBox")]
            public partial class TextEditor;
            [UCFContentType("article", "Article")]
            public partial class Article {
                [UCFProperty("title", "textEditor")]
                public partial string Title { get; }
            }
            """;

        var diagnostics = Analyze(source, _editorMetadata);

        diagnostics.ShouldNotContain(diagnostic =>
            diagnostic.Id == CodeFirstAttributeAnalyzer.UnknownEditorAliasId
            || diagnostic.Id == CodeFirstAttributeAnalyzer.IncompatibleEditorTypeId);
    }

    [Fact]
    public void Analyze_IncompatibleEditorPropertyType_ReportsEditorTypeDiagnostic() {
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDataType("textEditor", "Umbraco.TextBox")]
            public partial class TextEditor;
            [UCFContentType("article", "Article")]
            public partial class Article {
                [UCFProperty("title", "textEditor")]
                public partial int Title { get; }
            }
            """;

        var diagnostics = Analyze(source, _editorMetadata);

        diagnostics.ShouldContain(
            diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.IncompatibleEditorTypeId);
    }

    [Fact]
    public void Analyze_UnknownEditorAlias_ReportsUnknownEditorDiagnostic() {
        const string source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDataType("customEditor", "Umbraco.MissingEditor")]
            public partial class CustomEditor;
            """;

        var diagnostics = Analyze(source, _editorMetadata);

        diagnostics.ShouldContain(
            diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.UnknownEditorAliasId);
    }

    private static ImmutableArray<Diagnostic> Analyze(string source, string? metadata = null) {
        source = _attributeDefinitions + source;
        var compilation = CSharpCompilation.Create(
            "AnalyzerTests",
            [CSharpSyntaxTree.ParseText(source)],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ImmutableArray<DiagnosticAnalyzer> analyzers =
            [new CodeFirstAttributeAnalyzer()];
        var options = metadata is null
            ? new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty)
            : new AnalyzerOptions(
                [new InMemoryAdditionalText("umbraco-property-editors.json", metadata)]);
        return compilation
            .WithAnalyzers(analyzers, options)
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();
    }

    private const string _attributeDefinitions = """
        namespace Shorthand.UmbracoCodeFirst.Attributes;
        public sealed class UCFContentTypeAttribute : global::System.Attribute {
            public UCFContentTypeAttribute(string name) { }
            public UCFContentTypeAttribute(string alias, string name) { }
        }
        public sealed class UCFDataTypeAttribute : global::System.Attribute {
            public UCFDataTypeAttribute(string name, string editorAlias) { }
            public UCFDataTypeAttribute(string alias, string name, string editorAlias) { }
        }
        public sealed class UCFPropertyAttribute : global::System.Attribute {
            public UCFPropertyAttribute(string name, string dataTypeAlias) { }
            public UCFPropertyAttribute(string alias, string name, string dataTypeAlias) { }
        }
        [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
        public sealed class UCFCompositeAttribute(global::System.Type contentType) : global::System.Attribute;
        public class UCFGroupAttribute : global::System.Attribute {
            public UCFGroupAttribute(string name) { }
            public UCFGroupAttribute(string alias, string name) { }
        }
        public sealed class UCFTabAttribute : global::System.Attribute {
            public UCFTabAttribute(string name) { }
            public UCFTabAttribute(string alias, string name) { }
        }
        public class UCFValidationAttribute(string expression, string? message = null) : global::System.Attribute;
        """;

    private const string _editorMetadata = """
        {
          "editors": [
            {
              "id": "textBox",
              "name": "Textbox",
              "description": "A single-line text editor.",
              "alias": "Umbraco.TextBox",
              "uiAlias": "Umb.PropertyEditorUi.TextBox",
              "returnType": "string",
              "documentationUrl": "https://docs.umbraco.com/example"
            }
          ]
        }
        """;

    private sealed class InMemoryAdditionalText(string path, string content) : AdditionalText {
        public override string Path { get; } = path;

        public override SourceText? GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(content);
    }
}
