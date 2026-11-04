using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public abstract class AnalyzerTestBase {
    protected const string AttributeDefinitions = """
        namespace Shorthand.UmbracoCodeFirst.Attributes
        {
            public sealed class UCFDocumentTypeAttribute : global::System.Attribute {
                public UCFDocumentTypeAttribute(string name) { }
                public UCFDocumentTypeAttribute(string alias, string name) { }
            }
            public sealed class UCFElementTypeAttribute : global::System.Attribute {
                public UCFElementTypeAttribute(string name) { }
                public UCFElementTypeAttribute(string alias, string name) { }
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
            public class UCFValidationAttribute(string expression, string message = null!) : global::System.Attribute;
        }
        """;

    protected const string EditorMetadata = """
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
        new IncompatibleCompositionTypeAnalyzer(),
    ];

    protected static string Source(string source) {
        return Normalize(source) + AttributeDefinitions;
    }

    protected static string SourceWithAppended(string source, string appended) {
        return Source(source) + Normalize(appended);
    }

    private static string Normalize(string source) {
        var lines = source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var indentation = lines
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Select(static line => line.Length - line.TrimStart().Length)
            .DefaultIfEmpty(0)
            .Min();

        return string.Join(
            '\n',
            lines.Select(line => line.Length >= indentation ? line[indentation..] : string.Empty));
    }
}
