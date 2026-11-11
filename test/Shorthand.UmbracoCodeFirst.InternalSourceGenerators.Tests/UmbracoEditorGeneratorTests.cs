using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shorthand.UmbracoCodeFirst.InternalSourceGenerators.Tests;

public class UmbracoEditorGeneratorTests {
    [Fact]
    public void GenerateEditorCatalog_EmitsPublicEditorDefinitionsFromMetadata() {
        // Arrange
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
                      { "name": "MaxChars", "type": "int", "comment": "Limit <characters> & values.\nSecond line." }
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

        // Act
        var generatedSource = driver
            .RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult()
            .Results
            .Single(result => result.GeneratedSources.Any(source => source.HintName == "UmbracoEditors.g.cs"))
            .GeneratedSources
            .Single(source => source.HintName == "UmbracoEditors.g.cs")
            .SourceText
            .ToString();

        // Assert
        generatedSource.ShouldContain("public static class TextBox");
        generatedSource.ShouldContain(
            "public class Configuration : global::Shorthand.UmbracoCodeFirst.Metadata.CodeFirstEditorConfigurationDefinition");
        generatedSource.ShouldContain("int? MaxChars");
        generatedSource.ShouldContain(
            "/// Limit &lt;characters&gt; &amp; values." + Environment.NewLine
            + "            /// Second line." + Environment.NewLine
            + "            /// </summary>" + Environment.NewLine
            + "            public int? MaxChars");
        var syntaxTree = CSharpSyntaxTree.ParseText(
            generatedSource,
            new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose),
            cancellationToken: TestContext.Current.CancellationToken);
        syntaxTree.GetDiagnostics(TestContext.Current.CancellationToken).ShouldBeEmpty();
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
        // Arrange
        var metadata = ReadCatalog();
        using var document = JsonDocument.Parse(metadata);
        // Act
        var editors = document.RootElement.GetProperty("editors");

        // Assert
        editors.GetArrayLength().ShouldBe(38);
        editors.EnumerateArray().ShouldAllBe(editor =>
            editor.GetProperty("configuration").GetProperty("properties").ValueKind
                == JsonValueKind.Array);
        foreach(var editor in editors.EnumerateArray()) {
            foreach(var property in editor.GetProperty("configuration").GetProperty("properties").EnumerateArray()) {
                string.IsNullOrWhiteSpace(property.GetProperty("comment").GetString()).ShouldBeFalse();
            }
        }

        var generatedSource = GenerateSource(metadata);

        foreach(var editor in editors.EnumerateArray()) {
            var url = editor.GetProperty("documentationUrl").GetString().ShouldNotBeNull();
            Uri.TryCreate(url, UriKind.Absolute, out var uri).ShouldBeTrue();
            uri!.Host.ShouldBe("docs.umbraco.com");
            url.ShouldNotBe("https://docs.umbraco.com/umbraco-cms/model-your-content/property-editors/built-in-umbraco-property-editors");
            generatedSource.ShouldContain($"<see href=\"{url}\">Documentation</see>");
        }

        CountOccurrences(generatedSource, "public class Configuration :")
            .ShouldBe(editors.GetArrayLength());
        generatedSource.ShouldContain(
            "global::System.Text.Json.JsonElement? Blocks");
        generatedSource.ShouldContain(
            "public class Configuration : global::Shorthand.UmbracoCodeFirst.Metadata.CodeFirstBlockEditorConfigurationDefinition");
        generatedSource.ShouldContain("public static class SingleBlock");
        generatedSource.ShouldContain("bool? UseLiveEditing");
        generatedSource.ShouldContain("string? CreateModalSize");
        generatedSource.ShouldContain("public class Configuration :");

        var generatedTypes = CSharpSyntaxTree.ParseText(generatedSource, cancellationToken: TestContext.Current.CancellationToken)
            .GetRoot(TestContext.Current.CancellationToken)
            .DescendantNodes().OfType<ClassDeclarationSyntax>();
        var properties = generatedTypes
            .Where(type => type.Identifier.ValueText == "Configuration")
            .SelectMany(type => type.Members.OfType<PropertyDeclarationSyntax>())
            .ToArray();
        properties.Length.ShouldBe(editors.EnumerateArray().Sum(editor =>
            editor.GetProperty("configuration").GetProperty("properties").GetArrayLength()));
        properties.ShouldAllBe(property => property.GetLeadingTrivia().ToFullString().Contains("/// <summary>", StringComparison.Ordinal));
        foreach(var editorName in new[] { "BlockList", "BlockGrid", "SingleBlock", "RichTextEditor" }) {
            var editor = generatedTypes.Single(type => type.Identifier.ValueText == editorName);
            var configuration = editor.Members.OfType<ClassDeclarationSyntax>().Single(type => type.Identifier.ValueText == "Configuration");
            configuration.BaseList.ShouldNotBeNull().Types.Single().ToString()
                .ShouldBe("global::Shorthand.UmbracoCodeFirst.Metadata.CodeFirstBlockEditorConfigurationDefinition");
        }
    }

    [Fact]
    public void GenerateEditorCatalog_MissingConfigurationMetadataReportsDiagnostic() {
        // Arrange
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

        // Act
        var diagnostics = GenerateDiagnostics(metadata);

        // Assert
        diagnostics.ShouldContain(diagnostic =>
            diagnostic.Id == "UCF200"
            && diagnostic.GetMessage().Contains("configuration.properties", StringComparison.Ordinal));
    }

    [Fact]
    public void GenerateEditorCatalog_DuplicateConfigurationPropertyReportsDiagnostic() {
        // Arrange
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
                      { "name": "MaxChars", "type": "int", "comment": "Maximum length." },
                      { "name": "MaxChars", "type": "int", "comment": "Maximum length." }
                    ]
                  }
                }
              ]
            }
            """;

        // Act
        var diagnostics = GenerateDiagnostics(metadata);

        // Assert
        diagnostics.ShouldContain(diagnostic =>
            diagnostic.Id == "UCF200"
            && diagnostic.GetMessage().Contains("duplicated", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData(", \"comment\": \"  \"")]
    [InlineData(", \"comment\": 1")]
    public void GenerateEditorCatalog_MissingOrInvalidPropertyCommentReportsDiagnostic(string comment) {
        // Arrange
        var metadata = $$"""
            {
              "editors": [{
                "id": "textBox",
                "name": "Textbox",
                "description": "A single-line text editor.",
                "alias": "Umbraco.TextBox",
                "uiAlias": "Umb.PropertyEditorUi.TextBox",
                "returnType": "string",
                "documentationUrl": "https://docs.umbraco.com/example",
                "configuration": { "properties": [{ "name": "MaxChars", "type": "int"{{comment}} }] }
              }]
            }
            """;

        // Act
        var diagnostics = GenerateDiagnostics(metadata);

        // Assert
        diagnostics.ShouldContain(diagnostic =>
            diagnostic.Id == "UCF200"
            && diagnostic.GetMessage().Contains("'comment'", StringComparison.Ordinal));
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
