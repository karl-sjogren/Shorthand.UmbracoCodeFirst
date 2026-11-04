using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace Shorthand.UmbracoCodeFirst.PropertyEditors.SourceGenerators;

[Generator]
public sealed class UmbracoEditorGenerator : IIncrementalGenerator {
    private const string _fileName = "umbraco-property-editors.json";

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var metadata = context.AdditionalTextsProvider
            .Where(static text => text.Path.EndsWith(_fileName, StringComparison.OrdinalIgnoreCase))
            .Select(static (text, cancellationToken) => Parse(text, cancellationToken));

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(metadata.Collect()),
            static (productionContext, input) => {
                if(input.Left.GetTypeByMetadataName(
                    "Shorthand.UmbracoCodeFirst.PropertyEditors.UmbracoEditors") is not null) {
                    return;
                }

                var result = input.Right.FirstOrDefault();
                if(result is not null) {
                    Emit(productionContext, result);
                }
            });
    }

    private static MetadataResult Parse(AdditionalText text, CancellationToken cancellationToken) {
        var content = text.GetText(cancellationToken)?.ToString() ?? string.Empty;
        if(content.Length == 0) {
            return MetadataResult.Failure("The editor metadata file is empty.");
        }

        try {
            using var document = JsonDocument.Parse(content);
            if(!document.RootElement.TryGetProperty("editors", out var editors)
                || editors.ValueKind != JsonValueKind.Array) {
                return MetadataResult.Failure("The editor metadata file must contain an 'editors' array.");
            }

            var entries = new List<EditorMetadata>();
            foreach(var editor in editors.EnumerateArray()) {
                var id = GetString(editor, "id");
                var name = GetString(editor, "name");
                var description = GetString(editor, "description");
                var alias = GetString(editor, "alias");
                var uiAlias = GetString(editor, "uiAlias");
                var returnType = GetString(editor, "returnType");
                var documentationUrl = GetString(editor, "documentationUrl");
                if(new[] { id, name, description, alias, uiAlias, returnType, documentationUrl }
                    .Any(static value => string.IsNullOrWhiteSpace(value))) {
                    return MetadataResult.Failure("Every editor metadata entry must define all required fields.");
                }

                if(!editor.TryGetProperty("configuration", out var configuration)
                    || configuration.ValueKind != JsonValueKind.Object
                    || !configuration.TryGetProperty("properties", out var properties)
                    || properties.ValueKind != JsonValueKind.Array) {
                    return MetadataResult.Failure(
                        $"Editor metadata entry '{id}' must define configuration.properties, even when it is empty.");
                }

                var configurationProperties = ParseConfigurationProperties(editor, id!);
                entries.Add(new EditorMetadata(
                    id!,
                    name!,
                    description!,
                    alias!,
                    uiAlias!,
                    returnType!,
                    documentationUrl!,
                    configurationProperties));
            }

            var duplicate = entries
                .GroupBy(static entry => entry.Id, StringComparer.Ordinal)
                .FirstOrDefault(static group => group.Count() > 1);
            if(duplicate is not null) {
                return MetadataResult.Failure($"Editor metadata ID '{duplicate.Key}' is duplicated.");
            }

            return MetadataResult.Success(entries
                .OrderBy(static entry => entry.Id, StringComparer.Ordinal)
                .ToArray());
        } catch(JsonException exception) {
            return MetadataResult.Failure($"The editor metadata file is invalid JSON: {exception.Message}");
        }
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static void Emit(SourceProductionContext context, MetadataResult result) {
        if(!result.IsValid) {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "UCF200",
                    "Invalid Umbraco editor metadata",
                    "{0}",
                    "CodeFirst",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                Location.None,
                result.Error));
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("namespace Shorthand.UmbracoCodeFirst.PropertyEditors;");
        builder.AppendLine();
        builder.AppendLine("public sealed record UmbracoEditor(");
        builder.AppendLine("    string Alias,");
        builder.AppendLine("    string? UiAlias = null,");
        builder.AppendLine("    string? Name = null,");
        builder.AppendLine("    string? Description = null,");
        builder.AppendLine("    string? ReturnType = null,");
        builder.AppendLine("    string? DocumentationUrl = null);");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("/// Constants for the built-in Umbraco property editors.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public static class UmbracoEditors {");

        foreach(var editor in result.Entries!) {
            var className = GetClassName(editor.Id);
            builder.AppendLine($"    /// <summary>{EscapeXml(editor.Description)}</summary>");
            builder.AppendLine($"    /// <remarks><see href=\"{editor.DocumentationUrl}\">Documentation</see></remarks>");
            builder.AppendLine($"    public static class {className} {{");
            builder.AppendLine($"        public const string Alias = \"{EscapeString(editor.Alias)}\";");
            builder.AppendLine($"        public const string UiAlias = \"{EscapeString(editor.UiAlias)}\";");
            builder.AppendLine();
            AppendConfiguration(builder, editor.ConfigurationProperties);
            builder.AppendLine(
                $"        public static UmbracoEditor Definition {{ get; }} = new(Alias, UiAlias, \"{EscapeString(editor.Name)}\", \"{EscapeString(editor.Description)}\", \"{EscapeString(editor.ReturnType)}\", \"{EscapeString(editor.DocumentationUrl)}\");");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        builder.AppendLine("    public static class Numeric {");
        builder.AppendLine("        public const string Alias = Integer.Alias;");
        builder.AppendLine("        public const string UiAlias = Integer.UiAlias;");
        builder.AppendLine("        public static UmbracoEditor Definition { get; } = Integer.Definition;");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        context.AddSource("UmbracoEditors.g.cs", builder.ToString());
    }

    private static void AppendConfiguration(
        StringBuilder builder,
        IReadOnlyList<ConfigurationPropertyMetadata> properties) {
        builder.AppendLine();
        builder.AppendLine(
            "        public class Configuration : global::Shorthand.UmbracoCodeFirst.Metadata.CodeFirstEditorConfigurationDefinition {");
        foreach(var property in properties) {
            builder.AppendLine($"            public {property.CSharpType}? {property.Name} {{ get; set; }}");
        }

        builder.AppendLine("        }");
    }

    private static string GetClassName(string id) =>
        id switch {
            "checkBoxList" => "CheckBoxList",
            "dateTimeWithTimeZone" => "DateTimeWithTimeZone",
            "dateTimeUnspecified" => "DateTimeUnspecified",
            "eyeDropperColorPicker" => "EyeDropperColorPicker",
            "fileUpload" => "FileUpload",
            "multiUrlPicker" => "MultiUrlPicker",
            "radioButtonList" => "RadioButtonList",
            "repeatableTextstrings" => "RepeatableTextstrings",
            "richTextEditor" => "RichTextEditor",
            _ => char.ToUpperInvariant(id[0]) + id.Substring(1),
        };

    private static IReadOnlyList<ConfigurationPropertyMetadata> ParseConfigurationProperties(
        JsonElement editor,
        string editorId) {
        if(!editor.TryGetProperty("configuration", out var configuration)
            || configuration.ValueKind != JsonValueKind.Object
            || !configuration.TryGetProperty("properties", out var properties)
            || properties.ValueKind != JsonValueKind.Array) {
            return [];
        }

        var result = new List<ConfigurationPropertyMetadata>();
        foreach(var property in properties.EnumerateArray()) {
            var name = GetString(property, "name");
            var type = GetString(property, "type");
            if(string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type)) {
                throw new JsonException(
                    $"Configuration properties for editor '{editorId}' require 'name' and 'type'.");
            }

            var csharpType = type switch {
                "string" => "string",
                "bool" => "bool",
                "int" => "int",
                "decimal" => "decimal",
                "string[]" => "string[]",
                "int[]" => "int[]",
                "decimal[]" => "decimal[]",
                "json" => "global::System.Text.Json.JsonElement",
                _ => throw new JsonException(
                    $"Configuration property '{name}' on editor '{editorId}' uses unsupported type '{type}'."),
            };
            result.Add(new ConfigurationPropertyMetadata(name!, csharpType));
        }

        var duplicate = result
            .GroupBy(static property => property.Name, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if(duplicate is not null) {
            throw new JsonException(
                $"Configuration property '{duplicate.Key}' on editor '{editorId}' is duplicated.");
        }

        return result;
    }

    private static string EscapeString(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string EscapeXml(string value) =>
        value.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

    private sealed class EditorMetadata {
        public EditorMetadata(
            string id,
            string name,
            string description,
            string alias,
            string uiAlias,
            string returnType,
            string documentationUrl,
            IReadOnlyList<ConfigurationPropertyMetadata> configurationProperties) {
            Id = id;
            Name = name;
            Description = description;
            Alias = alias;
            UiAlias = uiAlias;
            ReturnType = returnType;
            DocumentationUrl = documentationUrl;
            ConfigurationProperties = configurationProperties;
        }

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string Alias { get; }
        public string UiAlias { get; }
        public string ReturnType { get; }
        public string DocumentationUrl { get; }
        public IReadOnlyList<ConfigurationPropertyMetadata> ConfigurationProperties { get; }
    }

    private sealed class ConfigurationPropertyMetadata {
        public ConfigurationPropertyMetadata(string name, string cSharpType) {
            Name = name;
            CSharpType = cSharpType;
        }

        public string Name { get; }
        public string CSharpType { get; }
    }

    private sealed class MetadataResult {
        private MetadataResult(bool isValid, IReadOnlyList<EditorMetadata>? entries, string? error) {
            IsValid = isValid;
            Entries = entries;
            Error = error;
        }

        public bool IsValid { get; }
        public IReadOnlyList<EditorMetadata>? Entries { get; }
        public string? Error { get; }

        public static MetadataResult Success(IReadOnlyList<EditorMetadata> entries) =>
            new(true, entries, null);

        public static MetadataResult Failure(string error) =>
            new(false, null, error);
    }
}
