using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shorthand.UmbracoCodeFirst.SourceGenerators.Tests;

public class CodeFirstRegistryGeneratorTests {
    [Fact]
    public void GenerateClasses_MarksRegistryAndPartialMembersButNotPartialTypes() {
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFDocumentTypeAttribute(string name) : global::System.Attribute;
                public sealed class UCFDataTypeAttribute(string name, string editorAlias, string editorUiAlias)
                    : global::System.Attribute;
                public sealed class UCFPropertyAttribute(string name, string dataTypeAlias)
                    : global::System.Attribute;
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentType("Article")]
                public partial class Article
                {
                    [Shorthand.UmbracoCodeFirst.Attributes.UCFProperty("Title", "text")]
                    public partial string Title { get; }
                }

                [Shorthand.UmbracoCodeFirst.Attributes.UCFDataType("Text", "text", "text")]
                public partial class Text;

                [Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentType("Both")]
                [Shorthand.UmbracoCodeFirst.Attributes.UCFDataType("Both", "text", "text")]
                public partial class Both;

                [global::System.CodeDom.Compiler.GeneratedCode("Existing", "1")]
                [Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentType("Existing")]
                public partial class Existing;
            }
            """;

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var references = new[] {
            typeof(object).Assembly.Location,
            typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).Assembly.Location,
        }.Distinct(StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var classes = driver.RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult().Results.Single().GeneratedSources
            .SelectMany(generated => generated.SyntaxTree.GetRoot(TestContext.Current.CancellationToken)
                .DescendantNodes().OfType<ClassDeclarationSyntax>())
            .ToArray();

        var registry = classes.Single(declaration =>
            declaration.Identifier.ValueText == "GeneratedCodeFirstDefinitionRegistry");
        registry.AttributeLists.SelectMany(list => list.Attributes)
            .ShouldContain(attribute => attribute.Name.ToString().EndsWith("GeneratedCode", StringComparison.Ordinal));

        var partialClasses = classes.Where(declaration => declaration.Modifiers.Any(SyntaxKind.PartialKeyword)).ToArray();
        partialClasses.Length.ShouldBe(5);
        foreach(var declaration in partialClasses) {
            declaration.AttributeLists.SelectMany(list => list.Attributes)
                .ShouldNotContain(attribute => attribute.Name.ToString().EndsWith("GeneratedCode", StringComparison.Ordinal));
            declaration.Members.ShouldAllBe(member => member.AttributeLists
                .SelectMany(list => list.Attributes)
                .Any(attribute => attribute.Name.ToString().EndsWith("GeneratedCode", StringComparison.Ordinal)));
        }
    }

    [Fact]
    public void GenerateRegistry_MediaAndMemberTypes_EmitsDistinctDefinitionKinds() {
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFMediaTypeAttribute(string name)
                    : global::System.Attribute;
                public sealed class UCFMemberTypeAttribute(string name)
                    : global::System.Attribute;
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFMediaType("Image Asset")]
                public partial class ImageAsset;

                [Shorthand.UmbracoCodeFirst.Attributes.UCFMemberType("Member Profile")]
                public partial class MemberProfile;
            }
            """;

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(
                source,
                cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generatedSource = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single()
            .GeneratedSources
            .Single(generated => generated.HintName == "CodeFirstRegistry.g.cs")
            .SourceText
            .ToString();

        generatedSource.ShouldContain("CodeFirstDefinitionKind.MediaType");
        generatedSource.ShouldContain("\"imageAsset\"");
        generatedSource.ShouldContain("CodeFirstDefinitionKind.MemberType");
        generatedSource.ShouldContain("\"memberProfile\"");
    }

    [Fact]
    public void GenerateRegistry_DataTypeConfigurationType_EmitsConstantJson() {
        const string source = """"
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFDataTypeAttribute : global::System.Attribute
                {
                    public UCFDataTypeAttribute(
                        string name,
                        string editorAlias,
                        string editorUiAlias) { }
                    public string? Alias { get; set; }
                    public string? ConfigurationJson { get; set; }
                    public global::System.Type? ConfigurationType { get; set; }
                }
            }

            namespace Example
            {
                public static class TextBoxConfiguration
                {
                    public const string Json = """{"maxChars":512}""";
                }

                [Shorthand.UmbracoCodeFirst.Attributes.UCFDataType(
                    "Configured Text",
                    "Umbraco.TextBox",
                    "Umb.PropertyEditorUi.TextBox",
                    ConfigurationType = typeof(TextBoxConfiguration))]
                public partial class ConfiguredText;
            }
            """";

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(
                source,
                cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generatedSource = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single()
            .GeneratedSources
            .Single(generated => generated.HintName == "CodeFirstRegistry.g.cs")
            .SourceText
            .ToString();

        generatedSource.ShouldContain("\\\"maxChars\\\":512");
        generatedSource.ShouldContain(
            "new global::Shorthand.UmbracoCodeFirst.Metadata.CodeFirstEditorConfiguration");
    }

    [Fact]
    public void GenerateRegistry_DataTypeConfigurationTypeWithoutJson_ReportsDiagnostic() {
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFDataTypeAttribute(string name, string editorAlias, string editorUiAlias)
                    : global::System.Attribute
                {
                    public global::System.Type? ConfigurationType { get; set; }
                }
            }

            namespace Example
            {
                public static class InvalidConfiguration
                {
                    public const int SchemaVersion = 1;
                }

                [Shorthand.UmbracoCodeFirst.Attributes.UCFDataType(
                    "Invalid",
                    "Umbraco.TextBox",
                    "Umb.PropertyEditorUi.TextBox",
                    ConfigurationType = typeof(InvalidConfiguration))]
                public partial class InvalidDataType;
            }
            """;

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(
                source,
                cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        result.GetRunResult().Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "UCF111");
    }

    [Fact]
    public void GenerateRegistry_TypedConfigurationConstants_EmitsJsonObject() {
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFDataTypeAttribute(string name, string editorAlias, string editorUiAlias)
                    : global::System.Attribute
                {
                    public global::System.Type? ConfigurationType { get; set; }
                }
            }

            namespace Example
            {
                public static class TextBoxConfiguration
                {
                    public const int MaxChars = 512;
                    public const bool Trim = true;
                }

                [Shorthand.UmbracoCodeFirst.Attributes.UCFDataType(
                    "Configured Text",
                    "Umbraco.TextBox",
                    "Umb.PropertyEditorUi.TextBox",
                    ConfigurationType = typeof(TextBoxConfiguration))]
                public partial class ConfiguredText;
            }
            """;

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(
                source,
                cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generatedSource = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single()
            .GeneratedSources
            .Single(generated => generated.HintName == "CodeFirstRegistry.g.cs")
            .SourceText
            .ToString();

        generatedSource.ShouldContain("\\\"maxChars\\\":512");
        generatedSource.ShouldContain("\\\"trim\\\":true");
    }

    [Fact]
    public void GenerateRegistry_NoDefinitions_DoesNotEmitEmptyRegistry() {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(
                "public static class Empty { }",
                cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var result = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single();

        result.GeneratedSources.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateRegistry_ElementType_EmitsElementMetadataAndModel() {
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFElementTypeAttribute : global::System.Attribute
                {
                    public UCFElementTypeAttribute(string name) { }
                    public UCFElementTypeAttribute(string alias, string name) { }
                    public bool AllowInLibrary { get; set; }
                }

                public sealed class UCFFolderPathAttribute(string path)
                    : global::System.Attribute
                {
                    public string Path { get; } = path;
                }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFElementType("seoElement")]
                [Shorthand.UmbracoCodeFirst.Attributes.UCFFolderPath("Elements/SEO")]
                public partial class SeoElement;
            }
            """;

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(
                source,
                cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generatorResult = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single();
        var registrySource = generatorResult.GeneratedSources
            .Single(generatedSource => generatedSource.HintName == "CodeFirstRegistry.g.cs")
            .SourceText
            .ToString();
        var modelSource = generatorResult.GeneratedSources
            .Single(generatedSource => generatedSource.HintName.Contains("SeoElement", StringComparison.Ordinal))
            .SourceText
            .ToString();

        registrySource.ShouldContain("CodeFirstTypeSettings(");
        registrySource.ShouldContain("true,");
        registrySource.ShouldContain("\"Elements/SEO\"");
        modelSource.ShouldContain("ModelItemType = PublishedItemType.Element");
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("Body text", "bodyText")]
    [InlineData("Product Page", "productPage")]
    [InlineData("ProductPage", "productPage")]
    [InlineData("SEO", "seo")]
    [InlineData("SEO URL", "seoUrl")]
    [InlineData("XMLParser", "xmlParser")]
    [InlineData("version2Value", "version2Value")]
    [InlineData("foo_bar-baz", "fooBarBaz")]
    [InlineData("  Multiple   words  ", "multipleWords")]
    public void ToAlias_DerivesExpectedCamelCaseAlias(string value, string expected) {
        CodeFirstRegistryGenerator.ToAlias(value).ShouldBe(expected);
    }

    [Fact]
    public void GenerateRegistry_DocumentTypeComposition_EmitsReferencedAlias() {
        // Arrange
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFDocumentTypeAttribute : global::System.Attribute
                {
                    public UCFDocumentTypeAttribute(string name) { }
                    public UCFDocumentTypeAttribute(string alias, string name) { }
                }

                [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
                public sealed class UCFCompositeAttribute(global::System.Type contentType)
                    : global::System.Attribute { }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentType("Base")]
                public partial class Base;

                [Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentType("Article")]
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
        var generatedSource = result
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
                public sealed class UCFDocumentTypeAttribute : global::System.Attribute
                {
                    public UCFDocumentTypeAttribute(string name) { }
                    public UCFDocumentTypeAttribute(string alias, string name) { }
                }

                public sealed class UCFPropertyAttribute : global::System.Attribute
                {
                    public UCFPropertyAttribute(string name, string dataTypeAlias) { }
                    public UCFPropertyAttribute(string alias, string name, string dataTypeAlias) { }
                }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentType("Article")]
                public partial class Article
                {
                    [Shorthand.UmbracoCodeFirst.Attributes.UCFProperty("Title", "textstring")]
                    public partial string Title { get; }
                }

                [Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentType("not-a-model", "Not a model")]
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
        var generatedSource = generatorResult.GeneratedSources
            .Single(source => source.HintName == "CodeFirstRegistry.g.cs")
            .SourceText.ToString();
        var modelSource = generatorResult.GeneratedSources
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
        var generatorVersion = typeof(CodeFirstRegistryGenerator).Assembly.GetName().Version!.ToString();
        modelSource.ShouldContain(
            $$"""
                /// <summary>
                ///   Title
                /// </summary>
                [GeneratedCode("Shorthand.UmbracoCodeFirst.SourceGenerators", "{{generatorVersion}}")]
                [MaybeNull]
                [ImplementPropertyType("title")]
                public partial global::System.String Title => this.Value<global::System.String>(_publishedValueFallback, "title");
            """);
    }

    [Fact]
    public void GenerateRegistry_DerivesCamelCaseAliasesFromPascalCaseNames() {
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFDocumentTypeAttribute : global::System.Attribute
                {
                    public UCFDocumentTypeAttribute(string name) { }
                }

                public sealed class UCFPropertyAttribute : global::System.Attribute
                {
                    public UCFPropertyAttribute(string name, string dataTypeAlias) { }
                }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentType("ProductPage")]
                public partial class ProductPage
                {
                    [Shorthand.UmbracoCodeFirst.Attributes.UCFProperty("SEO URL", "text")]
                    public partial string SeoUrl { get; }
                }
            }
            """;

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var generatedSource = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single()
            .GeneratedSources
            .Single(source => source.HintName == "CodeFirstRegistry.g.cs")
            .SourceText
            .ToString();

        generatedSource.ShouldContain("\"productPage\"");
        generatedSource.ShouldContain("\"seoUrl\"");
    }

    [Fact]
    public void GeneratePublishedModels_UnsupportedPropertyType_ReportsDiagnostic() {
        // Arrange
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFDocumentTypeAttribute : global::System.Attribute
                {
                    public UCFDocumentTypeAttribute(string name) { }
                }

                public sealed class UCFPropertyAttribute : global::System.Attribute
                {
                    public UCFPropertyAttribute(string name, string dataTypeAlias) { }
                }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentType("Article")]
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

                public sealed class UCFDocumentTypeAttribute : global::System.Attribute
                {
                    public UCFDocumentTypeAttribute(string name) { }
                }

                public sealed class UCFPropertyAttribute : global::System.Attribute
                {
                    public UCFPropertyAttribute(string name, string dataTypeAlias) { }
                }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentType("Article")]
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
        var generatedSource = generatorResult.GeneratedSources
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
                    public UCFTabAttribute(string name, int sortOrder) { }
                    public UCFTabAttribute(string alias, string name, int sortOrder) { }
                }

                public sealed class SeoTabAttribute()
                    : UCFTabAttribute("seo", "SEO", 4);

                public sealed class ImplicitSeoTabAttribute()
                    : UCFTabAttribute("SEO", 4);

                public sealed class UCFDocumentTypeAttribute : global::System.Attribute
                {
                    public UCFDocumentTypeAttribute(string name) { }
                }

                public sealed class UCFPropertyAttribute : global::System.Attribute
                {
                    public UCFPropertyAttribute(string name, string dataTypeAlias) { }
                }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentType("Article")]
                public partial class Article
                {
                    [Shorthand.UmbracoCodeFirst.Attributes.UCFProperty("Title", "textstring")]
                    [Shorthand.UmbracoCodeFirst.Attributes.SeoTab]
                    public partial string Title { get; }

                    [Shorthand.UmbracoCodeFirst.Attributes.UCFProperty("Description", "textstring")]
                    [Shorthand.UmbracoCodeFirst.Attributes.ImplicitSeoTab]
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
        var generatedSource = driver
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
        generatedSource.ShouldContain("\"description\", \"Description\"");
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

        var generatedSource = driver
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
    public void GenerateRegistry_DataTypeEditorType_EmitsUnquotedEditorAliases() {
        // Arrange
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFDataTypeAttribute : global::System.Attribute
                {
                    public UCFDataTypeAttribute(string name, global::System.Type editor) { }
                    public string? Alias { get; set; }
                }
            }

            namespace Example
            {
                public static class TextBox
                {
                    public const string Alias = "Umbraco.TextBox";
                    public const string UiAlias = "Umb.PropertyEditorUi.TextBox";
                }

                [Shorthand.UmbracoCodeFirst.Attributes.UCFDataType(
                    "Text",
                    typeof(TextBox))]
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
        var generatedSource = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single()
            .GeneratedSources
            .Single(generated => generated.HintName == "CodeFirstRegistry.g.cs")
            .SourceText
            .ToString();

        // Assert
        generatedSource.ShouldContain("\"Umbraco.TextBox\",");
        generatedSource.ShouldContain("\"Umb.PropertyEditorUi.TextBox\"");
        generatedSource.ShouldNotContain("\"\\\"Umbraco.TextBox\\\"\"");
        generatedSource.ShouldNotContain("\"\\\"Umb.PropertyEditorUi.TextBox\\\"\"");
    }

    [Fact]
    public void GenerateRegistry_EditorAndConfigurationTypes_EmitsAliasesAndConfiguration() {
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Metadata
            {
                public abstract class CodeFirstEditorConfigurationDefinition { }
            }

            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFDataTypeAttribute : global::System.Attribute
                {
                    public UCFDataTypeAttribute(string name, global::System.Type editor, global::System.Type configurationType) { }
                    public string? Alias { get; set; }
                    public global::System.Type? ConfigurationType { get; set; }
                }
            }

            namespace Example
            {
                public static class IntegerEditor
                {
                    public const string Alias = "Umbraco.Integer";
                    public const string UiAlias = "Umb.PropertyEditorUi.Integer";
                }

                public class IntegerConfiguration : Shorthand.UmbracoCodeFirst.Metadata.CodeFirstEditorConfigurationDefinition { }
                public static class ConstantConfiguration { public const int Min = 3; }
                public static class OverrideConfiguration { public const int Min = 7; }

                [Shorthand.UmbracoCodeFirst.Attributes.UCFDataType("Runtime", typeof(IntegerEditor), typeof(IntegerConfiguration))]
                public partial class RuntimeDataType;

                [Shorthand.UmbracoCodeFirst.Attributes.UCFDataType("Constant", typeof(IntegerEditor), typeof(ConstantConfiguration))]
                public partial class ConstantDataType;

                [Shorthand.UmbracoCodeFirst.Attributes.UCFDataType("Override", typeof(IntegerEditor), typeof(ConstantConfiguration),
                    ConfigurationType = typeof(OverrideConfiguration))]
                public partial class OverrideDataType;

                [Shorthand.UmbracoCodeFirst.Attributes.UCFDataType("No Configuration", typeof(IntegerEditor), typeof(ConstantConfiguration),
                    ConfigurationType = null)]
                public partial class NoConfigurationDataType;
            }
            """;

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken).GetRunResult();
        result.Diagnostics.ShouldBeEmpty();
        var generatedSource = result.Results.Single().GeneratedSources
            .Single(generated => generated.HintName == "CodeFirstRegistry.g.cs")
            .SourceText.ToString();

        var definitions = generatedSource.Split('\n')
            .Where(line => line.Contains("CodeFirstDefinitionKind.DataType", StringComparison.Ordinal))
            .ToArray();
        definitions.Length.ShouldBe(4);
        foreach(var definition in definitions) {
            definition.ShouldContain("\"Umbraco.Integer\"");
            definition.ShouldContain("\"Umb.PropertyEditorUi.Integer\"");
        }

        definitions.Single(line => line.Contains("\"Runtime\"", StringComparison.Ordinal))
            .ShouldContain("CodeFirstEditorConfiguration.FromType(typeof(global::Example.IntegerConfiguration), \"Umbraco.Integer\", 1)");
        definitions.Single(line => line.Contains("\"Constant\"", StringComparison.Ordinal))
            .ShouldContain("\\\"min\\\":3");
        definitions.Single(line => line.Contains("\"Override\"", StringComparison.Ordinal))
            .ShouldContain("\\\"min\\\":7");
        definitions.Single(line => line.Contains("\"No Configuration\"", StringComparison.Ordinal))
            .TrimEnd().ShouldEndWith(", null),");
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

        var generatedSource = driver
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

                public sealed class UCFDocumentTypeAttribute : global::System.Attribute
                {
                    public UCFDocumentTypeAttribute(string name) { }
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

                [Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentType("Article")]
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
        var generatedSource = generatorResult.GeneratedSources
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

                public sealed class UCFDocumentTypeAttribute : global::System.Attribute
                {
                    public UCFDocumentTypeAttribute(string name) { }
                }

                public sealed class UCFPropertyAttribute : global::System.Attribute
                {
                    public UCFPropertyAttribute(string name, string dataTypeAlias) { }
                }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentType("Product")]
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
        var generatedSource = generatorResult.GeneratedSources
            .Single(source => source.HintName == "CodeFirstRegistry.g.cs")
            .SourceText.ToString();

        // Assert
        generatedSource.ShouldContain("\"^[A-Z]{3}-[0-9]{4}$\"");
        generatedSource.ShouldContain("\"Use the ABC-1234 format.\"");
    }

    [Fact]
    public void GenerateRegistry_ExpandedTypeSettings_EmitsArraysAndConfiguration() {
        const string source = """
            namespace Shorthand.UmbracoCodeFirst.Attributes
            {
                public sealed class UCFDataTypeAttribute : global::System.Attribute
                {
                    public UCFDataTypeAttribute(string name, string editorAlias, string editorUiAlias) { }
                    public string? Alias { get; set; }
                    public string? ConfigurationJson { get; set; }
                }

                public sealed class UCFDocumentTypeAttribute : global::System.Attribute
                {
                    public UCFDocumentTypeAttribute(string name) { }
                    public bool AllowAsRoot { get; set; }
                    public string? DefaultTemplateAlias { get; set; }
                    public string[] AllowedTemplateAliases { get; set; } = [];
                    public string[] AllowedChildAliases { get; set; } = [];
                    public bool IsListView { get; set; }
                }

                public sealed class UCFDescriptionAttribute(string description)
                    : global::System.Attribute
                {
                    public string Description { get; } = description;
                }
            }

            namespace Example
            {
                [Shorthand.UmbracoCodeFirst.Attributes.UCFDataType(
                    "Text",
                    "Umbraco.TextBox",
                    "Umb.PropertyEditorUi.TextBox",
                    Alias = "integrationText",
                    ConfigurationJson = "{\"maxChars\":128}")]
                public partial class TextDataType;

                [Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentType(
                    "Landing",
                    AllowAsRoot = true,
                    DefaultTemplateAlias = "landing",
                    AllowedTemplateAliases = new[] { "landing", "campaign" },
                    AllowedChildAliases = new[] { "article", "folder" },
                    IsListView = true)]
                [Shorthand.UmbracoCodeFirst.Attributes.UCFDescription("Landing description")]
                public partial class Landing;
            }
            """;

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new CodeFirstRegistryGenerator().AsSourceGenerator());
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generatedSource = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single()
            .GeneratedSources
            .Single(source => source.HintName == "CodeFirstRegistry.g.cs")
            .SourceText
            .ToString();

        generatedSource.ShouldContain("\"integrationText\"");
        generatedSource.ShouldContain("\"{\\\"maxChars\\\":128}\"");
        generatedSource.ShouldContain("\"landing\"");
        generatedSource.ShouldContain("\"campaign\"");
        generatedSource.ShouldContain("\"article\"");
        generatedSource.ShouldContain("\"folder\"");
        generatedSource.ShouldContain("\"Landing description\"");
        generatedSource.ShouldContain("true");
    }
}
