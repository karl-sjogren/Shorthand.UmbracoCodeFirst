using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Shorthand.UmbracoCodeFirst.SourceGenerators.Tests;

public class PropertyAliasGeneratorTests {
    private const string _attributes = """
        namespace Shorthand.UmbracoCodeFirst.Attributes {
            public class UCFDocumentAttribute(string name) : global::System.Attribute;
            public class UCFElementTypeAttribute(string name) : global::System.Attribute;
            public class UCFMediaAttribute(string name) : global::System.Attribute;
            public class UCFMemberAttribute(string name) : global::System.Attribute;
            public class UCFPropertyAttribute : global::System.Attribute {
                public UCFPropertyAttribute(string name, string dataType) { }
                public UCFPropertyAttribute(string alias, string name, string dataType) { }
            }
        }
        """;

    [Fact]
    public void Generator_EmitsPropertyAliasesForAllContentKinds() {
        // Arrange
        var source = _attributes + """
            namespace Example {
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocument("Article")]
                public partial class Article {
                    [UCFProperty("SEO URL", "text")] public partial string Address { get; }
                    [UCFProperty("customAlias", "Display name", "text")] public partial string @class { get; }
                }
                [UCFElementType("Block")]
                public partial class Block {
                    [UCFProperty("Block title", "text")] public partial string Title { get; }
                }
                [UCFMedia("Image")]
                public partial class Image {
                    [UCFProperty("Image caption", "text")] public string Caption { get; set; }
                }
                [UCFMember("Profile")]
                public partial class Profile {
                    [UCFProperty("userBio", "Biography", "text")] public string Bio { get; set; }
                }
            }
            """;

        // Act
        var result = Generate(source);
        var article = SourceFor(result, "Example_Article", "PublishedModel.g.cs");
        var block = SourceFor(result, "Example_Block", "PublishedModel.g.cs");
        var image = SourceFor(result, "Example_Image", "PropertyAliases.g.cs");
        var profile = SourceFor(result, "Example_Profile", "PropertyAliases.g.cs");
        var registry = result.GeneratedSources.Single(item => item.HintName == "CodeFirstRegistry.g.cs").SourceText.ToString();

        // Assert
        article.ShouldContain("const string Address = \"seoUrl\";");
        article.ShouldContain("const string @class = \"customAlias\";");
        block.ShouldContain("const string Title = \"blockTitle\";");
        image.ShouldContain("const string Caption = \"imageCaption\";");
        profile.ShouldContain("const string Bio = \"userBio\";");
        registry.ShouldContain("\"seoUrl\"");
        registry.ShouldContain("\"customAlias\"");
        Generate(source).GeneratedSources.Select(item => item.SourceText.ToString())
            .ShouldBe(result.GeneratedSources.Select(item => item.SourceText.ToString()));
    }

    [Fact]
    public void GeneratedConstants_CompileInAttributeAndAnotherAssembly() {
        // Arrange
        var source = "using Shorthand.UmbracoCodeFirst.Attributes;\n" + _attributes + """
            [UCFMedia("Image")]
            public partial class Image {
                [UCFProperty("captionAlias", "Caption", "text")]
                public string Caption { get; set; }
            }
            [UCFMedia("Internal image")]
            internal partial class InternalImage {
                [UCFProperty("Alt text", "text")]
                public string AltText { get; set; }
            }
            public partial class Container<T> {
                [UCFMedia("Nested image")]
                public partial class Nested<U> {
                    [UCFProperty("Nested caption", "text")]
                    public string Caption { get; set; }
                }
            }
            public class CaptureAttribute(string value) : global::System.Attribute;
            [Capture(Image.PropertyAliases.Caption)]
            public class Consumer;
            [Capture(Container<int>.Nested<string>.PropertyAliases.Caption)]
            public class NestedConsumer;
            """;
        var compilation = Compilation(source);
        var result = CSharpGeneratorDriver.Create(new CodeFirstRegistryGenerator().AsSourceGenerator())
            .RunGenerators(compilation, TestContext.Current.CancellationToken).GetRunResult().Results.Single();
        var aliasSources = result.GeneratedSources.Where(item =>
            item.HintName.EndsWith(".PropertyAliases.g.cs", StringComparison.Ordinal)).ToArray();

        // Act
        var generatedCompilation = compilation.AddSyntaxTrees(aliasSources.Select(item => item.SyntaxTree));
        using var assembly = new MemoryStream();
        var emitted = generatedCompilation.Emit(assembly, cancellationToken: TestContext.Current.CancellationToken);
        emitted.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        emitted.Success.ShouldBeTrue();
        var consumer = Compilation("""
            [Capture(Image.PropertyAliases.Caption)]
            public class OtherConsumer;
            """).WithAssemblyName("OtherAssembly").AddReferences(MetadataReference.CreateFromImage(assembly.ToArray()));
        var codeFirstConsumer = Compilation("""
            [Shorthand.UmbracoCodeFirst.Attributes.UCFMedia(Image.PropertyAliases.Caption)]
            public partial class OtherMedia;
            """).WithAssemblyName("CodeFirstConsumer").AddReferences(MetadataReference.CreateFromImage(assembly.ToArray()));
        var consumerResult = CSharpGeneratorDriver.Create(new CodeFirstRegistryGenerator().AsSourceGenerator())
            .RunGenerators(codeFirstConsumer, TestContext.Current.CancellationToken).GetRunResult().Results.Single();

        // Assert
        consumer.GetDiagnostics(TestContext.Current.CancellationToken).Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        consumerResult.Diagnostics.ShouldNotContain(item => item.Id == "UCF117");
        consumerResult.GeneratedSources.Single(item => item.HintName == "CodeFirstRegistry.g.cs").SourceText.ToString()
            .ShouldContain("\"captionAlias\"");
    }

    [Fact]
    public void Generator_ReportsCollisionsAndUnresolvedSameCompilationReferences() {
        // Arrange
        var source = _attributes + """
            namespace Example {
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFMedia("Image")]
                public partial class Image {
                    public static class PropertyAliases { }
                    [UCFProperty("Caption", "text")] public string Caption { get; set; }
                }
                [UCFMedia(Other.PropertyAliases.Title)]
                public partial class Consumer;
                [UCFMedia("Other")]
                public partial class Other {
                    [UCFProperty("Title", "text")] public string Title { get; set; }
                }
                public class NonPartial {
                    [UCFMedia("Nested")]
                    public partial class Nested {
                        [UCFProperty("Caption", "text")] public string Caption { get; set; }
                    }
                }
            }
            """;

        // Act
        var result = Generate(source);

        // Assert
        result.Diagnostics.Count(item => item.Id == "UCF116").ShouldBe(1);
        result.Diagnostics.Count(item => item.Id == "UCF117").ShouldBe(1);
        result.Diagnostics.Count(item => item.Id == "UCF118").ShouldBe(1);
        SourceFor(result, "Example_Image", "PropertyAliases.g.cs").ShouldNotContain("static class PropertyAliases");
    }

    [Fact]
    public void Generator_AllowsExistingConstantsAndHidesInheritedAliasClass() {
        // Arrange
        var source = _attributes + """
            namespace Example {
                using Shorthand.UmbracoCodeFirst.Attributes;
                public static class External {
                    public static class PropertyAliases { public const string Title = "externalTitle"; }
                }
                [UCFMedia(External.PropertyAliases.Title)]
                public partial class Base {
                    [UCFProperty("Title", "text")] public string Title { get; set; }
                }
                [UCFMedia("Derived")]
                public partial class Derived : Base {
                    [UCFProperty("Caption", "text")] public string Caption { get; set; }
                }
            }
            """;

        // Act
        var result = Generate(source);

        // Assert
        result.Diagnostics.ShouldNotContain(item => item.Id == "UCF117");
        SourceFor(result, "Example_Derived", "PropertyAliases.g.cs").ShouldContain("public new static class PropertyAliases");
        result.GeneratedSources.Single(item => item.HintName == "CodeFirstRegistry.g.cs").SourceText.ToString()
            .ShouldContain("\"externalTitle\"");
    }

    private static GeneratorRunResult Generate(string source) =>
        CSharpGeneratorDriver.Create(new CodeFirstRegistryGenerator().AsSourceGenerator())
            .RunGenerators(Compilation(source)).GetRunResult().Results.Single();

    private static CSharpCompilation Compilation(string source) =>
        CSharpCompilation.Create(
            "AliasTests",
            [CSharpSyntaxTree.ParseText(source)],
            new[] { typeof(object).Assembly.Location, typeof(GeneratedCodeAttribute).Assembly.Location }
                .Distinct(StringComparer.Ordinal)
                .Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static string SourceFor(GeneratorRunResult result, string name, string suffix) =>
        result.GeneratedSources.Single(item => item.HintName.Contains(name, StringComparison.Ordinal)
            && item.HintName.EndsWith(suffix, StringComparison.Ordinal)).SourceText.ToString();
}
