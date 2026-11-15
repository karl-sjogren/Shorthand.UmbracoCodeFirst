using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Shorthand.UmbracoCodeFirst.SourceGenerators.Tests;

public class TemplateGeneratorTests {
    [Fact]
    public void GenerateRegistry_MarkedViewWithoutModels_EmitsTemplate() {
        // Arrange
        var view = new InMemoryAdditionalText(
            Path.Combine("site", "Views", "landingPage.cshtml"),
            """@attribute [Shorthand.UmbracoCodeFirst.Attributes.UCFTemplate("Landing \"Page\"")]""");

        // Act
        var result = RunGenerator(view);

        // Assert
        var registry = result.GeneratedSources.Single(source => source.HintName == "CodeFirstRegistry.g.cs").SourceText.ToString();
        registry.ShouldContain("CodeFirstDefinitionKind.Template, \"landingPage\", \"Landing \\\"Page\\\"\"");
        var aliases = result.GeneratedSources.Single(source => source.HintName == "UmbracoTemplates.g.cs").SourceText.ToString();
        aliases.ShouldContain("public static class UmbracoTemplates");
        aliases.ShouldContain("/// <summary>Template alias generated from <c>Views\\landingPage.cshtml</c>.</summary>");
        aliases.ShouldContain("public const string LandingPage = \"landingPage\";");
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateRegistry_UnmarkedNestedAndCommentedViews_AreIgnored() {
        // Arrange
        var files = new[] {
            new InMemoryAdditionalText(Path.Combine("site", "Views", "Plain.cshtml"), "<h1>Plain</h1>"),
            new InMemoryAdditionalText(Path.Combine("site", "Views", "Nested", "Page.cshtml"), """@attribute [UCFTemplate("Page")]"""),
            new InMemoryAdditionalText(Path.Combine("other", "Views", "External.cshtml"), """@attribute [UCFTemplate("External")]"""),
            new InMemoryAdditionalText(Path.Combine("site", "Views", "Comment.cshtml"), """
                @*
                @attribute [UCFTemplate("Comment")]
                *@
                <!--
                @attribute [UCFTemplate("HTML")]
                -->
                """),
        };

        // Act
        var result = RunGenerator(files);

        // Assert
        result.GeneratedSources.ShouldBeEmpty();
        result.Diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("@attribute [UCFTemplate(Names.Page)]", "non-empty string literal")]
    [InlineData("@attribute [UCFTemplate(\"\")]", "non-empty string literal")]
    [InlineData("@attribute [UCFTemplate(\"Page\")", "non-empty string literal")]
    [InlineData("@attribute [UCFTemplate(\"Page\"), UCFTemplate(\"Other\")]", "non-empty string literal")]
    public void GenerateRegistry_InvalidDeclaration_ReportsDiagnostic(string directive, string message) {
        // Arrange
        var view = new InMemoryAdditionalText(Path.Combine("site", "Views", "Page.cshtml"), directive);

        // Act
        var result = RunGenerator(view);

        // Assert
        result.Diagnostics.Single().Id.ShouldBe("UCF120");
        result.Diagnostics.Single().GetMessage().ShouldContain(message);
        result.GeneratedSources.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateRegistry_DuplicateAliases_ReportsBoth() {
        // Arrange
        var files = new[] {
            new InMemoryAdditionalText(Path.Combine("site", "Views", "Page.cshtml"), """@attribute [UCFTemplate("Page")]"""),
            new InMemoryAdditionalText(Path.Combine("site", "Views", "page.cshtml"), """@attribute [UCFTemplate("Page")]"""),
        };

        // Act
        var result = RunGenerator(files);

        // Assert
        result.Diagnostics.Count(diagnostic => diagnostic.Id == "UCF121").ShouldBe(2);
        result.GeneratedSources.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateRegistry_UnsafeFilename_ReportsDiagnostic() {
        // Arrange
        var view = new InMemoryAdditionalText(Path.Combine("site", "Views", "Bad-name.cshtml"), """@attribute [UCFTemplate("Page")]""");

        // Act
        var result = RunGenerator(view);

        // Assert
        result.Diagnostics.Single().Id.ShouldBe("UCF120");
        result.GeneratedSources.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateRegistry_ChangedAndRemovedView_UpdatesOutput() {
        // Arrange
        var oldView = new InMemoryAdditionalText(Path.Combine("site", "Views", "Page.cshtml"), """@attribute [UCFTemplate("Old")]""");
        var newView = new InMemoryAdditionalText(oldView.Path, """@attribute [UCFTemplate("New")]""");
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new CodeFirstRegistryGenerator().AsSourceGenerator()], additionalTexts: [oldView],
            optionsProvider: new TestOptionsProvider());
        var compilation = CSharpCompilation.Create("Templates",
            [CSharpSyntaxTree.ParseText("public class Example { }", cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        // Act
        driver = driver.ReplaceAdditionalText(oldView, newView).RunGenerators(compilation, TestContext.Current.CancellationToken);
        var updated = driver.GetRunResult().Results.Single().GeneratedSources.Single(source => source.HintName == "CodeFirstRegistry.g.cs").SourceText.ToString();
        var contentOnly = new InMemoryAdditionalText(oldView.Path, """@attribute [UCFTemplate("New")]""" + "\n<p>Changed</p>");
        driver = driver.ReplaceAdditionalText(newView, contentOnly).RunGenerators(compilation, TestContext.Current.CancellationToken);
        var contentUpdated = driver.GetRunResult().Results.Single().GeneratedSources.Single(source => source.HintName == "CodeFirstRegistry.g.cs").SourceText.ToString();
        driver = driver.RemoveAdditionalTexts([contentOnly]).RunGenerators(compilation, TestContext.Current.CancellationToken);

        // Assert
        updated.ShouldContain("\"Page\", \"New\"");
        updated.ShouldNotContain("\"Old\"");
        contentUpdated.ShouldNotBe(updated);
        driver.GetRunResult().Results.Single().GeneratedSources.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateRegistry_TemplateConstantsInSameCompilation_ResolveAttributeSettings() {
        // Arrange
        const string source = """
            using Shorthand.UmbracoCodeFirst;
            using Shorthand.UmbracoCodeFirst.Attributes;
            namespace Shorthand.UmbracoCodeFirst.Attributes {
                public sealed class UCFDocumentAttribute(string name) : global::System.Attribute {
                    public string DefaultTemplateAlias { get; set; }
                    public string[] AllowedTemplateAliases { get; set; }
                }
            }
            [UCFDocument("Page", DefaultTemplateAlias = (UmbracoTemplates.Page),
                AllowedTemplateAliases = new[] { "other", global::Shorthand.UmbracoCodeFirst.UmbracoTemplates.Page })]
            public partial class Page;
            """;
        var view = new InMemoryAdditionalText(Path.Combine("site", "Views", "page.cshtml"), """@attribute [UCFTemplate("Page")]""");
        var compilation = CSharpCompilation.Create("Templates",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new CodeFirstRegistryGenerator().AsSourceGenerator()], additionalTexts: [view],
            optionsProvider: new TestOptionsProvider());

        // Act
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken).GetRunResult().Results.Single();
        var registry = result.GeneratedSources.Single(item => item.HintName == "CodeFirstRegistry.g.cs").SourceText.ToString();

        // Assert
        result.Diagnostics.ShouldBeEmpty();
        System.Text.RegularExpressions.Regex.IsMatch(registry, "\"page\",\\s*new global::System.String\\[\\]").ShouldBeTrue();
        registry.ShouldContain("""new global::System.String[] { "other", "page" }""");
    }

    [Fact]
    public void GenerateRegistry_TemplateConstantNameCollision_ReportsDiagnostic() {
        // Arrange
        var files = new[] {
            new InMemoryAdditionalText(Path.Combine("site", "Views", "umbracoTemplates.cshtml"), """@attribute [UCFTemplate("One")]"""),
            new InMemoryAdditionalText(Path.Combine("site", "Views", "templateUmbracoTemplates.cshtml"), """@attribute [UCFTemplate("Two")]"""),
        };

        // Act
        var result = RunGenerator(files);

        // Assert
        result.Diagnostics.Count(diagnostic => diagnostic.Id == "UCF122").ShouldBe(2);
        result.GeneratedSources.ShouldBeEmpty();
    }

    private static GeneratorRunResult RunGenerator(params AdditionalText[] files) {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new CodeFirstRegistryGenerator().AsSourceGenerator()], additionalTexts: files,
            optionsProvider: new TestOptionsProvider());
        var compilation = CSharpCompilation.Create("Templates",
            [CSharpSyntaxTree.ParseText("public class Example { }")],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return driver.RunGenerators(compilation, TestContext.Current.CancellationToken).GetRunResult().Results.Single();
    }

    private sealed class InMemoryAdditionalText(string path, string content) : AdditionalText {
        public override string Path { get; } = System.IO.Path.GetFullPath(path);

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content);
    }

    private sealed class TestOptionsProvider : AnalyzerConfigOptionsProvider {
        private readonly AnalyzerConfigOptions _options = new TestOptions();

        public override AnalyzerConfigOptions GlobalOptions => _options;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;
    }

    private sealed class TestOptions : AnalyzerConfigOptions {
        public override bool TryGetValue(string key, out string value) {
            value = key == "build_property.MSBuildProjectDirectory" ? Path.GetFullPath("site") : null!;
            return value is not null;
        }
    }
}
