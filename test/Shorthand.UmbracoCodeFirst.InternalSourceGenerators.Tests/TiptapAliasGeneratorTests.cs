using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shorthand.UmbracoCodeFirst.InternalSourceGenerators.Tests;

public class TiptapAliasGeneratorTests {
    [Theory]
    [InlineData("{}", "groups")]
    [InlineData("[]", "groups")]
    [InlineData("{", "invalid JSON")]
    [InlineData("""{"groups":[{"name":"bad-name","aliases":[]}]}""", "valid C# name")]
    [InlineData("""{"groups":[{"name":"class","aliases":[]}]}""", "valid C# name")]
    [InlineData("""{"groups":[{"name":"Example","aliases":[{"name":"bad-name","alias":"a"}]}]}""", "valid C# name")]
    [InlineData("""{"groups":[{"name":"Example","aliases":[{"name":"Example","alias":"a"}]}]}""", "valid C# name")]
    [InlineData("""{"groups":[{"name":"Example","aliases":[{"name":"One","alias":""}]}]}""", "non-empty alias")]
    [InlineData("""{"groups":[{"name":"Example","aliases":[{"name":"One","alias":"a","description":42}]}]}""", "description")]
    [InlineData("""{"groups":[{"name":"Example","aliases":[{"name":"One","alias":"a","description":"Bad \u0001 text"}]}]}""", "invalid XML")]
    [InlineData("""{"groups":[{"name":"Example","aliases":[{"name":"One","alias":"a"},{"name":"One","alias":"b"}]}]}""", "duplicated")]
    [InlineData("""{"groups":[{"name":"Example","aliases":[{"name":"One","alias":"a"},{"name":"Two","alias":"a"}]}]}""", "duplicated")]
    [InlineData("""{"groups":[{"name":"Example","aliases":[]},{"name":"Example","aliases":[]}]}""", "duplicated")]
    public void GenerateTiptapAliases_InvalidCatalogReportsDiagnostic(string metadata, string message) {
        // Arrange

        // Act
        var result = RunGenerator(metadata);

        // Assert
        var diagnostic = result.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe("UCF202");
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
        diagnostic.GetMessage().ShouldContain(message);
        result.GeneratedSources.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateTiptapAliases_EmitsConstantsAndEscapedMultilineDocumentation() {
        // Arrange
        const string metadata = """
            {
              "groups": [{
                "name": "TiptapToolbar",
                "aliases": [
                  { "name": "Bold", "alias": "Example.\"Bold\"", "description": "Use <bold> & more.\nSecond line." },
                  { "name": "Custom", "alias": "Example.Custom" }
                ]
              }]
            }
            """;

        // Act
        var source = RunGenerator(metadata).GeneratedSources.Single().SourceText.ToString();

        // Assert
        source.ShouldContain("public static class TiptapToolbar");
        source.ShouldContain(
            "/// Use &lt;bold&gt; &amp; more." + Environment.NewLine
            + "    /// Second line." + Environment.NewLine
            + "    /// </summary>" + Environment.NewLine
            + "    public const string Bold = \"Example.\\\"Bold\\\"\";");
        source.ShouldContain("public const string Custom = \"Example.Custom\";");
        var tree = CSharpSyntaxTree.ParseText(source,
            new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose),
            cancellationToken: TestContext.Current.CancellationToken);
        tree.GetDiagnostics(TestContext.Current.CancellationToken).ShouldBeEmpty();
        var type = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes()
            .OfType<ClassDeclarationSyntax>().Single();
        type.AttributeLists.SelectMany(list => list.Attributes)
            .ShouldContain(attribute => attribute.Name.ToString() == "global::System.CodeDom.Compiler.GeneratedCode");
    }

    [Fact]
    public void GenerateTiptapAliases_RealCatalogEmitsAllDocumentedAliases() {
        // Arrange
        var resource = typeof(TiptapAliasGeneratorTests).Assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("umbraco-tiptap-aliases.json", StringComparison.Ordinal));
        using var stream = typeof(TiptapAliasGeneratorTests).Assembly.GetManifestResourceStream(resource);
        using var reader = new StreamReader(stream!);
        var metadata = reader.ReadToEnd();
        using var document = JsonDocument.Parse(metadata);

        // Act
        var result = RunGenerator(metadata);
        var source = result.GeneratedSources.Single().SourceText.ToString();
        var tree = CSharpSyntaxTree.ParseText(source,
            new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose),
            cancellationToken: TestContext.Current.CancellationToken);
        var classes = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes()
            .OfType<ClassDeclarationSyntax>().ToArray();

        // Assert
        result.Diagnostics.ShouldBeEmpty();
        tree.GetDiagnostics(TestContext.Current.CancellationToken).ShouldBeEmpty();
        var groups = document.RootElement.GetProperty("groups").EnumerateArray().ToArray();
        classes.Length.ShouldBe(groups.Length);
        foreach(var group in groups) {
            var type = classes.Single(item => item.Identifier.ValueText == group.GetProperty("name").GetString());
            var fields = type.Members.OfType<FieldDeclarationSyntax>().ToArray();
            var aliases = group.GetProperty("aliases").EnumerateArray().ToArray();
            fields.Length.ShouldBe(aliases.Length);
            foreach(var alias in aliases) {
                var name = alias.GetProperty("name").GetString();
                var field = fields.Single(item => item.Declaration.Variables.Single().Identifier.ValueText == name);
                field.Modifiers.Any(SyntaxKind.ConstKeyword).ShouldBeTrue();
                field.Declaration.Variables.Single().Initializer.ShouldNotBeNull().Value
                    .ShouldBeOfType<LiteralExpressionSyntax>().Token.ValueText.ShouldBe(alias.GetProperty("alias").GetString());
                field.GetLeadingTrivia().ToFullString().ShouldContain(alias.GetProperty("description").GetString()!);
            }
        }
    }

    [Fact]
    public void GenerateTiptapAliases_CatalogChangesUpdateGeneratedDocumentation() {
        // Arrange
        const string original = """{"groups":[{"name":"TiptapToolbar","aliases":[{"name":"Bold","alias":"a","description":"Before"}]}]}""";
        const string updated = """{"groups":[{"name":"TiptapToolbar","aliases":[{"name":"Bold","alias":"b","description":"After"}]}]}""";
        var oldText = new InMemoryAdditionalText("umbraco-tiptap-aliases.json", original);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TiptapAliasGenerator().AsSourceGenerator()], additionalTexts: [oldText]);
        var compilation = CSharpCompilation.Create("Example");
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        // Act
        driver = driver.ReplaceAdditionalText(oldText, new InMemoryAdditionalText(oldText.Path, updated))
            .RunGenerators(compilation, TestContext.Current.CancellationToken);
        var source = driver.GetRunResult().Results.Single().GeneratedSources.Single().SourceText.ToString();

        // Assert
        source.ShouldContain("/// After");
        source.ShouldContain("""public const string Bold = "b";""");
        source.ShouldNotContain("/// Before");
    }

    private static GeneratorRunResult RunGenerator(string metadata) {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new TiptapAliasGenerator().AsSourceGenerator()],
            additionalTexts: [new InMemoryAdditionalText("umbraco-tiptap-aliases.json", metadata)]);
        var compilation = CSharpCompilation.Create(
            "Shorthand.UmbracoCodeFirst",
            [CSharpSyntaxTree.ParseText(
                "public static class Empty { }",
                cancellationToken: TestContext.Current.CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return driver.RunGenerators(compilation, TestContext.Current.CancellationToken)
            .GetRunResult().Results.Single();
    }

    private sealed class InMemoryAdditionalText(string path, string content) : AdditionalText {
        public override string Path { get; } = path;

        public override global::Microsoft.CodeAnalysis.Text.SourceText? GetText(
            CancellationToken cancellationToken = default) =>
            global::Microsoft.CodeAnalysis.Text.SourceText.From(content);
    }
}
