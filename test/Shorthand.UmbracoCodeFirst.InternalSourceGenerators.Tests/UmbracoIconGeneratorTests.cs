using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shorthand.UmbracoCodeFirst.InternalSourceGenerators.Tests;

public class UmbracoIconGeneratorTests {
    [Fact]
    public void GenerateIconCatalog_EmitsAttributeCompatibleConstantsFromMetadata() {
        // Arrange
        const string metadata = """
            {
              "icons": [
                "icon-document",
                "icon-application-window-alt"
              ]
            }
            """;

        // Act
        var generatedSource = GenerateSource(metadata);

        // Assert
        generatedSource.ShouldContain("public static class UmbracoIcons");
        generatedSource.ShouldContain("public const string Document = \"icon-document\";");
        generatedSource.ShouldContain(
            "public const string ApplicationWindowAlt = \"icon-application-window-alt\";");
        generatedSource.ShouldContain(
            $"public const string ApplicationWindowAlt = \"icon-application-window-alt\";{Environment.NewLine}"
            + $"{Environment.NewLine}    /// <summary>Umbraco icon 'icon-document'.</summary>");
        var iconClass = CSharpSyntaxTree.ParseText(generatedSource, cancellationToken: TestContext.Current.CancellationToken)
            .GetRoot(TestContext.Current.CancellationToken)
            .DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
        iconClass.AttributeLists.SelectMany(list => list.Attributes)
            .ShouldContain(attribute => attribute.Name.ToString() == "global::System.CodeDom.Compiler.GeneratedCode");
    }

    [Fact]
    public void GenerateIconCatalog_RealCatalogIncludesEveryV18Icon() {
        // Arrange
        var metadata = ReadCatalog();
        using var document = JsonDocument.Parse(metadata);
        // Act
        var icons = document.RootElement.GetProperty("icons");

        // Assert
        icons.GetArrayLength().ShouldBe(678);
        icons.EnumerateArray()
            .ShouldAllBe(icon => icon.GetString()!.StartsWith("icon-", StringComparison.Ordinal));

        var generatedSource = GenerateSource(metadata);

        CountOccurrences(generatedSource, "public const string ").ShouldBe(icons.GetArrayLength());
        generatedSource.ShouldContain("public const string Document = \"icon-document\";");
        generatedSource.ShouldContain("public const string User = \"icon-user\";");
    }

    [Fact]
    public void GenerateIconCatalog_DuplicateIconReportsDiagnostic() {
        // Arrange
        const string metadata = """
            {
              "icons": [
                "icon-document",
                "icon-document"
              ]
            }
            """;

        // Act
        var diagnostics = GenerateDiagnostics(metadata);

        // Assert
        diagnostics.ShouldContain(diagnostic =>
            diagnostic.Id == "UCF201"
            && diagnostic.GetMessage().Contains("duplicated", StringComparison.Ordinal));
    }

    [Fact]
    public void GenerateIconCatalog_IdentifierCollisionReportsDiagnostic() {
        // Arrange
        const string metadata = """
            {
              "icons": [
                "icon-a-b",
                "icon-a_b"
              ]
            }
            """;

        // Act
        var diagnostics = GenerateDiagnostics(metadata);

        // Assert
        diagnostics.ShouldContain(diagnostic =>
            diagnostic.Id == "UCF201"
            && diagnostic.GetMessage().Contains("all generate the identifier", StringComparison.Ordinal));
    }

    private static string ReadCatalog() {
        var resourceName = typeof(UmbracoIconGeneratorTests)
            .Assembly
            .GetManifestResourceNames()
            .Single(name => name.EndsWith("umbraco-icons.json", StringComparison.Ordinal));
        using var stream = typeof(UmbracoIconGeneratorTests)
            .Assembly
            .GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    private static string GenerateSource(string metadata) =>
        RunGenerator(metadata)
            .Results
            .Single(result => result.GeneratedSources.Any(source =>
                source.HintName == "UmbracoIcons.g.cs"))
            .GeneratedSources
            .Single(source => source.HintName == "UmbracoIcons.g.cs")
            .SourceText
            .ToString();

    private static IReadOnlyList<Diagnostic> GenerateDiagnostics(string metadata) =>
        RunGenerator(metadata)
            .Results
            .SelectMany(result => result.Diagnostics)
            .ToArray();

    private static int CountOccurrences(string text, string value) {
        var count = 0;
        var index = 0;
        while((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0) {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static GeneratorDriverRunResult RunGenerator(string metadata) {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new UmbracoIconGenerator().AsSourceGenerator()],
            additionalTexts: [new InMemoryAdditionalText("umbraco-icons.json", metadata)]);
        var compilation = CSharpCompilation.Create(
            "Shorthand.UmbracoCodeFirst",
            [CSharpSyntaxTree.ParseText(
                "public static class Empty { }",
                cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult();
    }

    private sealed class InMemoryAdditionalText(string path, string content) : AdditionalText {
        public override string Path { get; } = path;

        public override global::Microsoft.CodeAnalysis.Text.SourceText? GetText(
            CancellationToken cancellationToken = default) =>
            global::Microsoft.CodeAnalysis.Text.SourceText.From(content);
    }
}
