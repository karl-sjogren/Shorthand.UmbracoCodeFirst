using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public abstract class AnalyzerTestBase {
    protected static ImmutableArray<Diagnostic> Analyze(string source, string? metadata = null) {
        source = _attributeDefinitions + source;
        var compilation = CSharpCompilation.Create(
            "AnalyzerTests",
            [CSharpSyntaxTree.ParseText(source)],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzers = AllAnalyzers;
        var options = metadata is null
            ? new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty)
            : new AnalyzerOptions(
                [new InMemoryAdditionalText("umbraco-property-editors.json", metadata)]);
        return compilation
            .WithAnalyzers(analyzers, options)
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();
    }

    protected static async Task<string> ApplyCodeFixAsync(string source) {
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject(
            ProjectInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Create(),
                "AnalyzerTests",
                "AnalyzerTests",
                LanguageNames.CSharp,
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                metadataReferences:
                [
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                ]));
        var document = project.AddDocument(
            "AnalyzerTests.cs",
            SourceText.From(_attributeDefinitions + source));
        var compilation = await document.Project.GetCompilationAsync();
        compilation.ShouldNotBeNull();
        var diagnostics = await compilation!.WithAnalyzers(
                AllAnalyzers)
            .GetAnalyzerDiagnosticsAsync();
        var diagnostic = diagnostics.Single(item =>
            item.Id == CodeFirstDiagnosticIds.DuplicateLayout);
        CodeAction? action = null;
        var context = new CodeFixContext(
            document,
            diagnostic,
            (registeredAction, _) => action = registeredAction,
            CancellationToken.None);

        await new RepeatedLayoutCodeFixProvider().RegisterCodeFixesAsync(context);
        action.ShouldNotBeNull();
        var operations = await action!.GetOperationsAsync(CancellationToken.None);
        var applyChanges = operations.OfType<ApplyChangesOperation>().Single();
        var changedDocument = applyChanges.ChangedSolution.GetDocument(document.Id);
        changedDocument.ShouldNotBeNull();
        return (await changedDocument!.GetTextAsync()).ToString();
    }

    protected static ImmutableArray<DiagnosticAnalyzer> AllAnalyzers =>
    [
        new InvalidAliasAnalyzer(),
        new DuplicateAliasAnalyzer(),
        new RepeatedLayoutAnalyzer(),
        new RepeatedValidationAnalyzer(),
        new UnsupportedPropertyTypeAnalyzer(),
        new PropertyOutsideDocumentTypeAnalyzer(),
        new InvalidCompositionTargetAnalyzer(),
        new InvalidCompositionGraphAnalyzer(),
        new EditorAliasAnalyzer(),
        new IncompatibleEditorTypeAnalyzer(),
    ];

    protected const string _attributeDefinitions = """
        namespace Shorthand.UmbracoCodeFirst.Attributes;
        public sealed class UCFDocumentTypeAttribute : global::System.Attribute {
            public UCFDocumentTypeAttribute(string name) { }
            public UCFDocumentTypeAttribute(string alias, string name) { }
        }
        public sealed class UCFDataTypeAttribute : global::System.Attribute {
            public UCFDataTypeAttribute(string name, string editorAlias) { }
            public UCFDataTypeAttribute(string alias, string name, string editorAlias) { }
        }
        public sealed class UCFPropertyAttribute : global::System.Attribute {
            public UCFPropertyAttribute(string name, string dataTypeAlias) { }
            public UCFPropertyAttribute(string alias, string name, string dataTypeAlias) { }
        }
        [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
        public sealed class UCFCompositeAttribute(global::System.Type contentType) : global::System.Attribute;
        public class UCFGroupAttribute : global::System.Attribute {
            public UCFGroupAttribute(string name) { }
            public UCFGroupAttribute(string alias, string name) { }
        }
        public sealed class UCFTabAttribute : global::System.Attribute {
            public UCFTabAttribute(string name) { }
            public UCFTabAttribute(string name, int sortOrder) { }
            public UCFTabAttribute(string alias, string name) { }
        }
        public class UCFValidationAttribute(string expression, string? message = null) : global::System.Attribute;
        """;

    protected const string _editorMetadata = """
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

    protected sealed class InMemoryAdditionalText(string path, string content) : AdditionalText {
        public override string Path { get; } = path;

        public override SourceText? GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(content);
    }
}
