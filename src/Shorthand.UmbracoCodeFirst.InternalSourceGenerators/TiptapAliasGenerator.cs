using System.Text;
using System.Text.Json;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Shorthand.UmbracoCodeFirst.InternalSourceGenerators;

[Generator]
public sealed class TiptapAliasGenerator : IIncrementalGenerator {
    private const string _fileName = "umbraco-tiptap-aliases.json";

    private static readonly DiagnosticDescriptor _invalidCatalog = new(
        "UCF202",
        "Invalid Umbraco Tiptap alias catalog",
        "{0}",
        "CodeFirst",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly string _generatedCodeAttribute =
        $"[global::System.CodeDom.Compiler.GeneratedCode(\"Shorthand.UmbracoCodeFirst.InternalSourceGenerators\", \"{typeof(TiptapAliasGenerator).Assembly.GetName().Version}\")]";

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var metadata = context.AdditionalTextsProvider
            .Where(static text => text.Path.EndsWith(_fileName, StringComparison.OrdinalIgnoreCase))
            .Select(static (text, cancellationToken) => Parse(text, cancellationToken));

        context.RegisterSourceOutput(metadata, static (productionContext, result) => Emit(productionContext, result));
    }

    private static CatalogResult Parse(AdditionalText text, CancellationToken cancellationToken) {
        var content = text.GetText(cancellationToken)?.ToString() ?? string.Empty;
        if(content.Length == 0) {
            return CatalogResult.Failure("The Tiptap alias catalog file is empty.");
        }

        try {
            using var document = JsonDocument.Parse(content);
            if(document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("groups", out var groups)
                || groups.ValueKind != JsonValueKind.Array) {
                return CatalogResult.Failure("The Tiptap alias catalog must contain a 'groups' array.");
            }

            var groupEntries = new List<GroupMetadata>();
            foreach(var group in groups.EnumerateArray()) {
                if(group.ValueKind != JsonValueKind.Object
                    || GetString(group, "name") is not { } groupName
                    || !IsValidIdentifier(groupName)
                    || !group.TryGetProperty("aliases", out var aliases)
                    || aliases.ValueKind != JsonValueKind.Array) {
                    return CatalogResult.Failure("Every group must have a valid C# name and an 'aliases' array.");
                }

                var aliasEntries = new List<AliasMetadata>();
                foreach(var alias in aliases.EnumerateArray()) {
                    if(alias.ValueKind != JsonValueKind.Object
                        || GetString(alias, "name") is not { } name
                        || !IsValidIdentifier(name)
                        || name == groupName
                        || GetString(alias, "alias") is not { } value
                        || string.IsNullOrWhiteSpace(value)) {
                        return CatalogResult.Failure(
                            $"Every alias in '{groupName}' must have a valid C# name and a non-empty alias.");
                    }

                    string? description = null;
                    if(alias.TryGetProperty("description", out var descriptionElement)) {
                        if(descriptionElement.ValueKind != JsonValueKind.String
                            || descriptionElement.GetString() is not { } descriptionText
                            || string.IsNullOrWhiteSpace(descriptionText)) {
                            return CatalogResult.Failure(
                                $"The description for '{groupName}.{name}' must be a non-empty string.");
                        }

                        description = descriptionText;
                        try {
                            XmlConvert.VerifyXmlChars(descriptionText);
                        } catch(XmlException) {
                            return CatalogResult.Failure(
                                $"The description for '{groupName}.{name}' contains invalid XML characters.");
                        }
                    }

                    if(aliasEntries.Any(entry => entry.Name == name || entry.Alias == value)) {
                        return CatalogResult.Failure($"The name or alias for '{groupName}.{name}' is duplicated.");
                    }

                    aliasEntries.Add(new AliasMetadata(name, value, description));
                }

                if(groupEntries.Any(entry => entry.Name == groupName)) {
                    return CatalogResult.Failure($"The group '{groupName}' is duplicated.");
                }

                groupEntries.Add(new GroupMetadata(groupName, aliasEntries));
            }

            return CatalogResult.Success(groupEntries);
        } catch(JsonException exception) {
            return CatalogResult.Failure($"The Tiptap alias catalog is invalid JSON: {exception.Message}");
        }
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static bool IsValidIdentifier(string name) =>
        !name.StartsWith("@", StringComparison.Ordinal)
        && SyntaxFacts.IsValidIdentifier(name)
        && SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None;

    private static void Emit(SourceProductionContext context, CatalogResult result) {
        if(result.Error is not null) {
            context.ReportDiagnostic(Diagnostic.Create(_invalidCatalog, Location.None, result.Error));
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("namespace Shorthand.UmbracoCodeFirst.PropertyEditors;");
        builder.AppendLine();

        foreach(var group in result.Groups!) {
            builder.AppendLine("/// <summary>Built-in Umbraco Tiptap aliases.</summary>");
            builder.AppendLine(_generatedCodeAttribute);
            builder.AppendLine($"public static class {group.Name} {{");
            foreach(var alias in group.Aliases) {
                if(alias.Description is not null) {
                    builder.AppendLine("    /// <summary>");
                    foreach(var line in alias.Description.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) {
                        builder.AppendLine($"    /// {EscapeXml(line)}");
                    }

                    builder.AppendLine("    /// </summary>");
                }

                builder.AppendLine($"    public const string {alias.Name} = {SyntaxFactory.Literal(alias.Alias)};");
            }

            builder.AppendLine("}");
            builder.AppendLine();
        }

        context.AddSource("TiptapAliases.g.cs", builder.ToString());
    }

    private static string EscapeXml(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private sealed class AliasMetadata(string name, string alias, string? description) {
        public string Name { get; } = name;
        public string Alias { get; } = alias;
        public string? Description { get; } = description;
    }

    private sealed class GroupMetadata(string name, IReadOnlyList<AliasMetadata> aliases) {
        public string Name { get; } = name;
        public IReadOnlyList<AliasMetadata> Aliases { get; } = aliases;
    }

    private sealed class CatalogResult(IReadOnlyList<GroupMetadata>? groups, string? error) {
        public IReadOnlyList<GroupMetadata>? Groups { get; } = groups;
        public string? Error { get; } = error;

        public static CatalogResult Success(IReadOnlyList<GroupMetadata> groups) => new(groups, null);
        public static CatalogResult Failure(string error) => new(null, error);
    }
}
