using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Shorthand.UmbracoCodeFirst.SourceGenerators.Tests;

public class CodeFirstRegistryGeneratorTests {
    [Fact]
    public void Generates_registry_for_attributed_partial_classes() {
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

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorRunResult generatorResult = result.GetRunResult().Results
            .Single()
            ;
        string generatedSource = generatorResult.GeneratedSources
            .Single(source => source.HintName == "CodeFirstRegistry.g.cs")
            .SourceText.ToString();
        string modelSource = generatorResult.GeneratedSources
            .Single(source => source.HintName.Contains("Example_Article", StringComparison.Ordinal))
            .SourceText.ToString();

        generatedSource.ShouldContain("CodeFirstDefinitionKind.DocumentType");
        generatedSource.ShouldContain("\"article\"");
        generatedSource.ShouldContain("\"title\"");
        generatedSource.ShouldNotContain("NotAModel");
        modelSource.ShouldContain("[PublishedModel(\"article\")]");
        modelSource.ShouldContain("public partial class Article : PublishedContentModel");
        modelSource.ShouldContain("public partial global::System.String Title => this.Value<global::System.String>");
        modelSource.ShouldContain("[ImplementPropertyType(\"title\")]");
    }

    [Fact]
    public void Reports_diagnostic_for_unsupported_published_property_types() {
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

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true));

        GeneratorDriver result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        result.GetRunResult().Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "UCF001");
    }

    [Fact]
    public void Reads_group_and_tab_metadata_from_derived_attributes() {
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

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorRunResult generatorResult = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single();
        string generatedSource = generatorResult.GeneratedSources
            .Single(source => source.HintName == "CodeFirstRegistry.g.cs")
            .SourceText.ToString();

        generatedSource.ShouldContain("\"marketing\"");
        generatedSource.ShouldContain("\"Marketing\"");
        generatedSource.ShouldContain("3, \"marketingTab\"");
        generatedSource.ShouldContain("\"Marketing tab\", 4");
    }

    [Fact]
    public void Reads_data_type_editor_ui_alias() {
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

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        CSharpCompilation compilation = CSharpCompilation.Create(
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

        generatedSource.ShouldContain("\"Umbraco.TextBox\"");
        generatedSource.ShouldContain("\"Umb.PropertyEditorUi.TextBox\"");
    }

    [Fact]
    public void Generates_static_alias_for_data_type_classes() {
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

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        CSharpCompilation compilation = CSharpCompilation.Create(
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

        generatedSource.ShouldContain("public const string Alias = \"codeFirstBoolean\";");
    }

    [Fact]
    public void Resolves_generated_data_type_aliases_in_property_attributes() {
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

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorRunResult generatorResult = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single();
        generatorResult.Diagnostics.ShouldBeEmpty();
        string generatedSource = generatorResult.GeneratedSources
            .Single(source => source.HintName == "CodeFirstRegistry.g.cs")
            .SourceText
            .ToString();

        generatedSource.ShouldContain("\"codeFirstLongText\"");
    }

    [Fact]
    public void Reads_validation_metadata_from_derived_attributes() {
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

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorRunResult generatorResult = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single();
        string generatedSource = generatorResult.GeneratedSources
            .Single(source => source.HintName == "CodeFirstRegistry.g.cs")
            .SourceText.ToString();

        generatedSource.ShouldContain("\"^[A-Z]{3}-[0-9]{4}$\"");
        generatedSource.ShouldContain("\"Use the ABC-1234 format.\"");
    }
}
