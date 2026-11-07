using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shorthand.UmbracoCodeFirst.InternalSourceGenerators.Tests;

public class UmbracoEditorGeneratorTests {
    [Fact]
    public void GenerateEditorCatalog_EmitsPublicEditorDefinitionsFromMetadata() {
        const string metadata = """
            {
              "editors": [
                {
                  "id": "textBox",
                  "name": "Textbox",
                  "description": "A single-line text editor.",
                  "alias": "Umbraco.TextBox",
                  "uiAlias": "Umb.PropertyEditorUi.TextBox",
                  "returnType": "string",
                  "documentationUrl": "https://docs.umbraco.com/example",
                  "configuration": {
                    "properties": [
                      { "name": "MaxChars", "type": "int" }
                    ]
                  }
                }
              ]
            }
            """;

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new UmbracoEditorGenerator().AsSourceGenerator()],
            additionalTexts: [new InMemoryAdditionalText(
                "umbraco-property-editors.json",
                metadata)]);
        var compilation = CSharpCompilation.Create(
            "Shorthand.UmbracoCodeFirst",
            [CSharpSyntaxTree.ParseText(
                "public static class Empty { }",
                cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generatedSource = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single(result => result.GeneratedSources.Any(source => source.HintName == "UmbracoEditors.g.cs"))
            .GeneratedSources
            .Single(source => source.HintName == "UmbracoEditors.g.cs")
            .SourceText
            .ToString();

        generatedSource.ShouldContain("public static class TextBox");
        generatedSource.ShouldContain(
            "public class Configuration : global::Shorthand.UmbracoCodeFirst.Metadata.CodeFirstEditorConfigurationDefinition");
        generatedSource.ShouldContain("int? MaxChars");
        generatedSource.ShouldContain("public const string Alias = \"Umbraco.TextBox\"");
        generatedSource.ShouldContain("ReturnType");
        var generatedTypes = CSharpSyntaxTree.ParseText(generatedSource, cancellationToken: TestContext.Current.CancellationToken)
            .GetRoot(TestContext.Current.CancellationToken)
            .DescendantNodes().OfType<TypeDeclarationSyntax>().ToArray();
        generatedTypes.Length.ShouldBe(5);
        generatedTypes.ShouldAllBe(type => type.AttributeLists
            .SelectMany(list => list.Attributes)
            .Any(attribute => attribute.Name.ToString() == "global::System.CodeDom.Compiler.GeneratedCode"));
    }

    [Fact]
    public void GenerateEditorCatalog_RealCatalogDefinesConfigurationForEveryEditor() {
        var metadata = ReadCatalog();
        using var document = JsonDocument.Parse(metadata);
        var editors = document.RootElement.GetProperty("editors");

        editors.GetArrayLength().ShouldBe(37);
        editors.EnumerateArray().ShouldAllBe(editor =>
            editor.GetProperty("configuration").GetProperty("properties").ValueKind
                == JsonValueKind.Array);

        var generatedSource = GenerateSource(metadata);

        CountOccurrences(generatedSource, "public class Configuration :")
            .ShouldBe(editors.GetArrayLength());
        generatedSource.ShouldContain(
            "global::System.Text.Json.JsonElement? Blocks");
        generatedSource.ShouldContain("public class Configuration :");
    }

    [Fact]
    public void GenerateEditorCatalog_MissingConfigurationMetadataReportsDiagnostic() {
        const string metadata = """
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

        var diagnostics = GenerateDiagnostics(metadata);

        diagnostics.ShouldContain(diagnostic =>
            diagnostic.Id == "UCF200"
            && diagnostic.GetMessage().Contains("configuration.properties", StringComparison.Ordinal));
    }

    [Fact]
    public void GenerateEditorCatalog_DuplicateConfigurationPropertyReportsDiagnostic() {
        const string metadata = """
            {
              "editors": [
                {
                  "id": "textBox",
                  "name": "Textbox",
                  "description": "A single-line text editor.",
                  "alias": "Umbraco.TextBox",
                  "uiAlias": "Umb.PropertyEditorUi.TextBox",
                  "returnType": "string",
                  "documentationUrl": "https://docs.umbraco.com/example",
                  "configuration": {
                    "properties": [
                      { "name": "MaxChars", "type": "int" },
                      { "name": "MaxChars", "type": "int" }
                    ]
                  }
                }
              ]
            }
            """;

        var diagnostics = GenerateDiagnostics(metadata);

        diagnostics.ShouldContain(diagnostic =>
            diagnostic.Id == "UCF200"
            && diagnostic.GetMessage().Contains("duplicated", StringComparison.Ordinal));
    }

    private static string ReadCatalog() {
        var resourceName = typeof(UmbracoEditorGeneratorTests)
            .Assembly
            .GetManifestResourceNames()
            .Single(name => name.EndsWith(
                "umbraco-property-editors.json",
                StringComparison.Ordinal));
        using var stream = typeof(UmbracoEditorGeneratorTests)
            .Assembly
            .GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    private static string GenerateSource(string metadata) =>
        RunGenerator(metadata)
            .Results
            .Single(result => result.GeneratedSources.Any(source =>
                source.HintName == "UmbracoEditors.g.cs"))
            .GeneratedSources
            .Single(source => source.HintName == "UmbracoEditors.g.cs")
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
            [new UmbracoEditorGenerator().AsSourceGenerator()],
            additionalTexts: [new InMemoryAdditionalText(
                "umbraco-property-editors.json",
                metadata)]);
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
