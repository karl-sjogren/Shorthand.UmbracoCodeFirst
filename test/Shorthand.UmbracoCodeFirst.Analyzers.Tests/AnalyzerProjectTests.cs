using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

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
            [UCFDataType("product", "Product editor")]
            public partial class ProductEditor;
            """;

        // Act
        var diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.DuplicateAliasId);
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

    private static ImmutableArray<Diagnostic> Analyze(string source) {
        source = _attributeDefinitions + source;
        CSharpCompilation compilation = CSharpCompilation.Create(
            "AnalyzerTests",
            [CSharpSyntaxTree.ParseText(source)],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ImmutableArray<DiagnosticAnalyzer> analyzers =
            [new CodeFirstAttributeAnalyzer()];
        return compilation
            .WithAnalyzers(analyzers)
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();
    }

    private const string _attributeDefinitions = """
        namespace Shorthand.UmbracoCodeFirst.Attributes;
        public sealed class UCFContentTypeAttribute(string alias, string name) : global::System.Attribute;
        public sealed class UCFDataTypeAttribute(string alias, string name) : global::System.Attribute;
        public sealed class UCFPropertyAttribute(string alias, string name, string dataTypeAlias) : global::System.Attribute;
        [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
        public sealed class UCFCompositeAttribute(global::System.Type contentType) : global::System.Attribute;
        public class UCFGroupAttribute(string alias, string name) : global::System.Attribute;
        public sealed class UCFTabAttribute(string alias, string name) : global::System.Attribute;
        public class UCFValidationAttribute(string expression, string? message = null) : global::System.Attribute;
        """;
}
