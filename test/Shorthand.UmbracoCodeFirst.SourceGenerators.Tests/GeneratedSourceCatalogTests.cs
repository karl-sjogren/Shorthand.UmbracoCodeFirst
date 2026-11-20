using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.SourceGenerators.Tests;

public class GeneratedSourceCatalogTests {
    private const string _source = """
        using Shorthand.UmbracoCodeFirst.Attributes;
        namespace Example;
        [UCFDataType("Shared", "editor", "ui", Alias = "same", ConfigurationJson = "{\"value\":\"quoted\\nvalue\"}")]
        public partial class First;
        [UCFDataType("Shared", "editor", "ui", Alias = "same")]
        public partial class Second;
        [UCFMedia("Image")]
        public partial class Image {
            [UCFProperty("Caption", "same")]
            public string Caption { get; set; }
        }
        [UCFMember("Profile")]
        public partial class Profile {
            [UCFProperty("Bio", "same")]
            public string Bio { get; set; }
        }
        """;

    [Fact]
    public void DebugCatalog_ContainsExactGeneratedFilesForEachDefinition() {
        // Arrange
        var (driver, compilation) = CreateDriver("Debug");

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var output, out var diagnostics, TestContext.Current.CancellationToken);
        using var stream = new MemoryStream();
        var emitted = output.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        diagnostics.ShouldBeEmpty();
        emitted.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        emitted.Success.ShouldBeTrue();
        var catalogType = Assembly.Load(stream.ToArray())
            .GetType("Shorthand.UmbracoCodeFirst.Generated.GeneratedCodeFirstSourceCatalog").ShouldNotBeNull();
        var catalog = ((ICodeFirstGeneratedSourceCatalog?)Activator.CreateInstance(catalogType)).ShouldNotBeNull();
        var generated = driver.GetRunResult().Results.Single().GeneratedSources
            .ToDictionary(item => item.HintName, item => item.SourceText.ToString());
        var first = catalog.GetSources(0);
        var second = catalog.GetSources(3);
        var media = catalog.GetSources(1);
        var member = catalog.GetSources(2);
        var template = catalog.GetSources(4);
        first.Select(item => item.FileName).ShouldContain(name => name.Contains("First.DataType", StringComparison.Ordinal));
        second.Select(item => item.FileName).ShouldContain(name => name.Contains("Second.DataType", StringComparison.Ordinal));
        media.Select(item => item.FileName).ShouldContain(name => name.Contains("Image.PropertyAliases", StringComparison.Ordinal));
        member.Select(item => item.FileName).ShouldContain(name => name.Contains("Profile.PropertyAliases", StringComparison.Ordinal));
        template.Select(item => item.FileName).ShouldContain("UmbracoTemplates.g.cs");
        media[0].FileName.ShouldContain("Image.PropertyAliases");
        template[0].FileName.ShouldBe("UmbracoTemplates.g.cs");
        foreach(var file in first.Concat(second).Concat(media).Concat(member).Concat(template).Where(file => !file.IsOriginal)) {
            file.Content.ShouldBe(generated[file.FileName]);
        }

        first.Single(file => file.IsOriginal).Content.ShouldBe(_source);
        media.Single(file => file.IsOriginal).Content.ShouldBe(_source);
        template.Single(file => file.IsOriginal).Content.ShouldBe("""@attribute [UCFTemplate("Page")]""");
        catalog.GetSources(-1).ShouldBeEmpty();
        catalog.GetSources(5).ShouldBeEmpty();
    }

    [Fact]
    public void IncrementalUpdate_RefreshesCapturedSourcesDeterministically() {
        // Arrange
        var (driver, compilation) = CreateDriver("Debug");
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var initial = driver.GetRunResult().Results.Single().GeneratedSources
            .Single(item => item.HintName == "CodeFirstSourceCatalog.g.cs").SourceText.ToString();
        var originalTree = compilation.SyntaxTrees.Single();
        var updatedTree = CSharpSyntaxTree.ParseText(
            _source.Replace("\"Caption\"", "\"New caption\"", StringComparison.Ordinal),
            cancellationToken: TestContext.Current.CancellationToken);
        var updatedCompilation = compilation.ReplaceSyntaxTree(originalTree, updatedTree);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(
            updatedCompilation, out var output, out _, TestContext.Current.CancellationToken);
        using var stream = new MemoryStream();
        var emitted = output.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        var result = driver.GetRunResult().Results.Single();
        var updated = result.GeneratedSources.Single(item => item.HintName == "CodeFirstSourceCatalog.g.cs").SourceText.ToString();
        var catalogType = Assembly.Load(stream.ToArray())
            .GetType("Shorthand.UmbracoCodeFirst.Generated.GeneratedCodeFirstSourceCatalog").ShouldNotBeNull();
        var catalog = ((ICodeFirstGeneratedSourceCatalog?)Activator.CreateInstance(catalogType)).ShouldNotBeNull();

        // Assert
        emitted.Success.ShouldBeTrue();
        updated.ShouldNotBe(initial);
        var media = catalog.GetSources(1);
        foreach(var file in media.Where(file => !file.IsOriginal)) {
            file.Content.ShouldBe(result.GeneratedSources.Single(item => item.HintName == file.FileName).SourceText.ToString());
        }

        media.Single(file => file.FileName.Contains("PropertyAliases", StringComparison.Ordinal))
            .Content.ShouldContain("newCaption");
        media.Single(file => file.IsOriginal).Content.ShouldContain("\"New caption\"");
        driver.RunGenerators(updatedCompilation, TestContext.Current.CancellationToken)
            .GetRunResult().Results.Single().GeneratedSources
            .Single(item => item.HintName == "CodeFirstSourceCatalog.g.cs").SourceText.ToString().ShouldBe(updated);
    }

    [Fact]
    public void DebugCatalog_IncludesAllPartialFilesWithoutAbsolutePaths() {
        // Arrange
        var (driver, compilation) = CreateDriver("Debug");
        var firstPath = Path.GetFullPath(Path.Combine("first", "Shared.cs"));
        var secondPath = Path.GetFullPath(Path.Combine("second", "Shared.cs"));
        var originalTree = CSharpSyntaxTree.ParseText(_source, path: firstPath, cancellationToken: TestContext.Current.CancellationToken);
        const string partial = "namespace Example; public partial class Image { public int Other => 42; }";
        var partialTree = CSharpSyntaxTree.ParseText(partial, path: secondPath, cancellationToken: TestContext.Current.CancellationToken);
        compilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(originalTree, partialTree);

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var output, out _, TestContext.Current.CancellationToken);
        using var stream = new MemoryStream();
        var emitted = output.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        emitted.Success.ShouldBeTrue();
        var catalogType = Assembly.Load(stream.ToArray())
            .GetType("Shorthand.UmbracoCodeFirst.Generated.GeneratedCodeFirstSourceCatalog").ShouldNotBeNull();
        var catalog = ((ICodeFirstGeneratedSourceCatalog?)Activator.CreateInstance(catalogType)).ShouldNotBeNull();
        var originals = catalog.GetSources(1).Where(file => file.IsOriginal).ToArray();
        originals.Select(file => file.FileName).ShouldBe(["Shared.cs", "Shared (2).cs"]);
        originals.Select(file => file.Content).ShouldBe([_source, partial]);
        originals.ShouldAllBe(file => !Path.IsPathRooted(file.FileName));
        catalog.GetSources(0).Count(file => file.IsOriginal).ShouldBe(1);
    }

    [Theory]
    [InlineData("Release")]
    [InlineData(null)]
    public void NonDebugBuild_DoesNotEmitSourceCatalog(string? configuration) {
        // Arrange
        var (driver, compilation) = CreateDriver(configuration);

        // Act
        var result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken).GetRunResult().Results.Single();

        // Assert
        result.GeneratedSources.ShouldNotContain(item => item.HintName == "CodeFirstSourceCatalog.g.cs");
    }

    private static (GeneratorDriver Driver, CSharpCompilation Compilation) CreateDriver(string? configuration) {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(UCFDataTypeAttribute).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            $"CatalogTests{Guid.NewGuid():N}",
            [CSharpSyntaxTree.ParseText(_source, cancellationToken: TestContext.Current.CancellationToken)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var view = new TestAdditionalText(Path.Combine("site", "Views", "page.cshtml"), """@attribute [UCFTemplate("Page")]""");
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new CodeFirstRegistryGenerator().AsSourceGenerator()],
            additionalTexts: [view],
            optionsProvider: new TestOptionsProvider(configuration));
        return (driver, compilation);
    }

    private sealed class TestAdditionalText(string path, string content) : AdditionalText {
        public override string Path { get; } = System.IO.Path.GetFullPath(path);
        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content);
    }

    private sealed class TestOptionsProvider(string? configuration) : AnalyzerConfigOptionsProvider {
        private readonly AnalyzerConfigOptions _options = new TestOptions(configuration);
        public override AnalyzerConfigOptions GlobalOptions => _options;
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;
    }

    private sealed class TestOptions(string? configuration) : AnalyzerConfigOptions {
        public override bool TryGetValue(string key, out string value) {
            value = key switch {
                "build_property.MSBuildProjectDirectory" => Path.GetFullPath("site"),
                "build_property.Configuration" => configuration!,
                _ => null!,
            };
            return value is not null;
        }
    }
}
