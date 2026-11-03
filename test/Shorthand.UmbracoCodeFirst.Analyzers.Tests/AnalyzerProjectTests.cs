using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class AnalyzerProjectTests {
    [Fact]
    public void AnalyzerAssembly_LoadProjectAssembly_LoadsSuccessfully() {
        // Arrange
        const string expectedName = "Shorthand.UmbracoCodeFirst.Analyzers.Tests";

        // Act
        Assembly assembly = typeof(AnalyzerProjectTests).Assembly;

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
        ImmutableArray<Diagnostic> diagnostics = Analyze(source);

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
        ImmutableArray<Diagnostic> diagnostics = Analyze(source);

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
        ImmutableArray<Diagnostic> diagnostics = Analyze(source);

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
        ImmutableArray<Diagnostic> diagnostics = Analyze(source);

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
        ImmutableArray<Diagnostic> diagnostics = Analyze(source);

        // Assert
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == CodeFirstAttributeAnalyzer.DuplicateValidationId);
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
        public class UCFGroupAttribute(string alias, string name) : global::System.Attribute;
        public sealed class UCFTabAttribute(string alias, string name) : global::System.Attribute;
        public class UCFValidationAttribute(string expression, string? message = null) : global::System.Attribute;
        """;
}
