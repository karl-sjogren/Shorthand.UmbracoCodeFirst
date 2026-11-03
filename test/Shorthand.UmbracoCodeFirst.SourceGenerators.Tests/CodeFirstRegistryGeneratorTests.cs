using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Shorthand.UmbracoCodeFirst.SourceGenerators.Tests;

public class CodeFirstRegistryGeneratorTests {
    [Fact]
    public void GenerateRegistry_ContentTypeComposition_EmitsReferencedAlias() {
        // Arrange
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFContentTypeAttribute : global::System.Attribute
                {
                    public UCFContentTypeAttribute(string name) { }
                    public UCFContentTypeAttribute(string alias, string name) { }
                }

                [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
                public sealed class UCFCompositeAttribute(global::System.Type contentType)
                    : global::System.Attribute { }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFContentType("Base")]
                public partial class Base;

                [Shorthand.UmbracoCodeFirst.Attributes.UCFContentType("Article")]
                [Shorthand.UmbracoCodeFirst.Attributes.UCFComposite(typeof(Base))]
                public partial class Article;
            }
            """;

        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var result = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single()
            ;
        string generatedSource = result
            .GeneratedSources
            .Single(source => source.HintName == "CodeFirstRegistry.g.cs")
            .SourceText
            .ToString();

        // Assert
        generatedSource.ShouldContain("new global::System.String[] { \"base\" }");
    }

    [Fact]
    public void GenerateRegistry_AttributedPartialClasses_EmitsExpectedDefinitionsAndModels() {
        // Arrange
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFContentTypeAttribute : global::System.Attribute
                {
                    public UCFContentTypeAttribute(string name) { }
                    public UCFContentTypeAttribute(string alias, string name) { }
                }

                public sealed class UCFPropertyAttribute : global::System.Attribute
                {
                    public UCFPropertyAttribute(string name, string dataTypeAlias) { }
                    public UCFPropertyAttribute(string alias, string name, string dataTypeAlias) { }
                }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFContentType("Article")]
                public partial class Article
                {
                    [Shorthand.UmbracoCodeFirst.Attributes.UCFProperty("Title", "textstring")]
                    public partial string Title { get; }
                }

                [Shorthand.UmbracoCodeFirst.Attributes.UCFContentType("not-a-model", "Not a model")]
                public class NotAModel
                {
                }
            }
            """;

        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var generatorResult = result.GetRunResult().Results
            .Single()
            ;
        string generatedSource = generatorResult.GeneratedSources
            .Single(source => source.HintName == "CodeFirstRegistry.g.cs")
            .SourceText.ToString();
        string modelSource = generatorResult.GeneratedSources
            .Single(source => source.HintName.Contains("Example_Article", StringComparison.Ordinal))
            .SourceText.ToString();

        // Assert
        generatedSource.ShouldContain("CodeFirstDefinitionKind.DocumentType");
        generatedSource.ShouldContain("\"article\"");
        generatedSource.ShouldContain("\"title\"");
        generatedSource.ShouldNotContain("NotAModel");
        modelSource.ShouldContain("[PublishedModel(\"article\")]");
        modelSource.ShouldContain("public partial class Article : PublishedContentModel");
        modelSource.ShouldContain("public new const string ModelTypeAlias = \"article\";");
        modelSource.ShouldContain("public new static IPublishedContentType GetModelContentType");
        modelSource.ShouldContain("public static IPublishedPropertyType GetModelPropertyType<TValue>");
        modelSource.ShouldContain("public partial global::System.String Title => this.Value<global::System.String>");
        modelSource.ShouldContain("[ImplementPropertyType(\"title\")]");
    }

    [Fact]
    public void GeneratePublishedModels_UnsupportedPropertyType_ReportsDiagnostic() {
        // Arrange
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFContentTypeAttribute : global::System.Attribute
                {
                    public UCFContentTypeAttribute(string name) { }
                }

                public sealed class UCFPropertyAttribute : global::System.Attribute
                {
                    public UCFPropertyAttribute(string name, string dataTypeAlias) { }
                }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFContentType("Article")]
                public unsafe partial class Article
                {
                    [Shorthand.UmbracoCodeFirst.Attributes.UCFProperty("Pointer", "textstring")]
                    public partial int* Pointer { get; }
                }
            }
            """;

        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true));

        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        // Assert
        result.GetRunResult().Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "UCF001");
    }

    [Fact]
    public void GenerateRegistry_DerivedGroupAndTabAttributes_EmitsLayoutMetadata() {
        // Arrange
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public class UCFGroupAttribute : global::System.Attribute
                {
                    public UCFGroupAttribute(string alias, string name, int sortOrder) { }
                }

                public sealed class MarketingGroupAttribute
                    : UCFGroupAttribute
                {
                    public MarketingGroupAttribute()
                        : base("marketing", "Marketing", 3) { }
                }

                public class UCFTabAttribute : global::System.Attribute
                {
                    public UCFTabAttribute(string alias, string name, int sortOrder) { }
                }

                public sealed class MarketingTabAttribute
                    : UCFTabAttribute
                {
                    public MarketingTabAttribute()
                        : base("marketingTab", "Marketing tab", 4) { }
                }

                public sealed class UCFContentTypeAttribute : global::System.Attribute
                {
                    public UCFContentTypeAttribute(string name) { }
                }

                public sealed class UCFPropertyAttribute : global::System.Attribute
                {
                    public UCFPropertyAttribute(string name, string dataTypeAlias) { }
                }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFContentType("Article")]
                public partial class Article
                {
                    [Shorthand.UmbracoCodeFirst.Attributes.UCFProperty("Title", "textstring")]
                    [Shorthand.UmbracoCodeFirst.Attributes.MarketingGroup]
                    [Shorthand.UmbracoCodeFirst.Attributes.MarketingTab]
                    public partial string Title { get; }
                }
            }
            """;

        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generatorResult = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single();
        string generatedSource = generatorResult.GeneratedSources
            .Single(source => source.HintName == "CodeFirstRegistry.g.cs")
            .SourceText.ToString();

        // Assert
        generatedSource.ShouldContain("\"marketing\"");
        generatedSource.ShouldContain("\"Marketing\"");
        generatedSource.ShouldContain("3, \"marketingTab\"");
        generatedSource.ShouldContain("\"Marketing tab\", 4");
    }

    [Fact]
    public void GenerateRegistry_PrimaryConstructorDerivedTabAttribute_PreservesExplicitAlias() {
        // Arrange
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public class UCFTabAttribute : global::System.Attribute
                {
                    public UCFTabAttribute(string alias, string name, int sortOrder) { }
                }

                public sealed class SeoTabAttribute()
                    : UCFTabAttribute("seo", "SEO", 4);

                public sealed class UCFContentTypeAttribute : global::System.Attribute
                {
                    public UCFContentTypeAttribute(string name) { }
                }

                public sealed class UCFPropertyAttribute : global::System.Attribute
                {
                    public UCFPropertyAttribute(string name, string dataTypeAlias) { }
                }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFContentType("Article")]
                public partial class Article
                {
                    [Shorthand.UmbracoCodeFirst.Attributes.UCFProperty("Title", "textstring")]
                    [Shorthand.UmbracoCodeFirst.Attributes.SeoTab]
                    public partial string Title { get; }
                }
            }
            """;

        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        string generatedSource = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single()
            .GeneratedSources
            .Single(generated => generated.HintName == "CodeFirstRegistry.g.cs")
            .SourceText
            .ToString();

        // Assert
        generatedSource.ShouldContain("\"seo\", \"SEO\", 4");
    }

    [Fact]
    public void GenerateRegistry_DataTypeEditorUiAlias_EmitsSeparateAliases() {
        // Arrange
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFDataTypeAttribute : global::System.Attribute
                {
                    public UCFDataTypeAttribute(string name, string editorAlias) { }
                    public string? EditorUiAlias { get; set; }
                }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFDataType(
                    "Text",
                    "Umbraco.TextBox",
                    EditorUiAlias = "Umb.PropertyEditorUi.TextBox")]
                public partial class TextDataType;
            }
            """;

        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        string generatedSource = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single()
            .GeneratedSources
            .Single(source => source.HintName == "CodeFirstRegistry.g.cs")
            .SourceText
            .ToString();

        // Assert
        generatedSource.ShouldContain("\"Umbraco.TextBox\"");
        generatedSource.ShouldContain("\"Umb.PropertyEditorUi.TextBox\"");
    }

    [Fact]
    public void GenerateDataTypeAlias_DataTypeDeclaration_EmitsCompileTimeConstant() {
        // Arrange
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFDataTypeAttribute : global::System.Attribute
                {
                    public UCFDataTypeAttribute(string alias, string name, string editorAlias) { }
                }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFDataType(
                    "codeFirstBoolean",
                    "Code First Boolean",
                    "Umbraco.TrueFalse")]
                public partial class CodeFirstBooleanDataType;
            }
            """;

        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        string generatedSource = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single()
            .GeneratedSources
            .Single(source => source.HintName.Contains("CodeFirstBooleanDataType", StringComparison.Ordinal))
            .SourceText
            .ToString();

        // Assert
        generatedSource.ShouldContain("public const string Alias = \"codeFirstBoolean\";");
    }

    [Fact]
    public void GenerateRegistry_GeneratedDataTypeAliasReference_ResolvesReferencedAlias() {
        // Arrange
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFDataTypeAttribute : global::System.Attribute
                {
                    public UCFDataTypeAttribute(string alias, string name, string editorAlias) { }
                }

                public sealed class UCFContentTypeAttribute : global::System.Attribute
                {
                    public UCFContentTypeAttribute(string name) { }
                }

                public sealed class UCFPropertyAttribute : global::System.Attribute
                {
                    public UCFPropertyAttribute(string alias, string name, string dataTypeAlias) { }
                }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFDataType(
                    "codeFirstLongText",
                    "Code First Long Text",
                    "Umbraco.TextArea")]
                public partial class CodeFirstLongTextDataType;

                [Shorthand.UmbracoCodeFirst.Attributes.UCFContentType("Article")]
                public partial class Article
                {
                    [Shorthand.UmbracoCodeFirst.Attributes.UCFProperty(
                        "description",
                        "Description",
                        CodeFirstLongTextDataType.Alias)]
                    public partial string Description { get; }
                }
            }
            """;

        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generatorResult = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single();
        // Assert
        generatorResult.Diagnostics.ShouldBeEmpty();
        string generatedSource = generatorResult.GeneratedSources
            .Single(source => source.HintName == "CodeFirstRegistry.g.cs")
            .SourceText
            .ToString();

        generatedSource.ShouldContain("\"description\", \"Description\", \"codeFirstLongText\"");
    }

    [Fact]
    public void GenerateRegistry_DerivedValidationAttribute_EmitsValidationMetadata() {
        // Arrange
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public class UCFValidationAttribute : global::System.Attribute
                {
                    public UCFValidationAttribute(string expression, string message) { }
                }

                public sealed class ProductCodeValidationAttribute
                    : UCFValidationAttribute
                {
                    public ProductCodeValidationAttribute()
                        : base("^[A-Z]{3}-[0-9]{4}$", "Use the ABC-1234 format.") { }
                }

                public sealed class UCFContentTypeAttribute : global::System.Attribute
                {
                    public UCFContentTypeAttribute(string name) { }
                }

                public sealed class UCFPropertyAttribute : global::System.Attribute
                {
                    public UCFPropertyAttribute(string name, string dataTypeAlias) { }
                }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFContentType("Product")]
                public partial class Product
                {
                    [Shorthand.UmbracoCodeFirst.Attributes.UCFProperty("Code", "textstring")]
                    [Shorthand.UmbracoCodeFirst.Attributes.ProductCodeValidation]
                    public partial string Code { get; }
                }
            }
            """;

        // Act
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generatorResult = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single();
        string generatedSource = generatorResult.GeneratedSources
            .Single(source => source.HintName == "CodeFirstRegistry.g.cs")
            .SourceText.ToString();

        // Assert
        generatedSource.ShouldContain("\"^[A-Z]{3}-[0-9]{4}$\"");
        generatedSource.ShouldContain("\"Use the ABC-1234 format.\"");
    }
}
