using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Shorthand.UmbracoCodeFirst.PropertyEditors.SourceGenerators.Tests;

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
                  "documentationUrl": "https://docs.umbraco.com/example"
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
        generatedSource.ShouldContain("public const string Alias = \"Umbraco.TextBox\"");
        generatedSource.ShouldContain("ReturnType");
    }

    private sealed class InMemoryAdditionalText(string path, string content) : AdditionalText {
        public override string Path { get; } = path;

        public override global::Microsoft.CodeAnalysis.Text.SourceText? GetText(
            CancellationToken cancellationToken = default) =>
            global::Microsoft.CodeAnalysis.Text.SourceText.From(content);
    }
}
