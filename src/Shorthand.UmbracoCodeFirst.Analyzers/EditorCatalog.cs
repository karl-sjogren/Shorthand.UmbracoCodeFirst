using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

internal sealed class EditorCatalog {
    private readonly ImmutableDictionary<string, EditorMetadata> _editors;

    private EditorCatalog(ImmutableDictionary<string, EditorMetadata> editors) {
        _editors = editors;
    }

    public bool IsEmpty => _editors.IsEmpty;

    public bool TryGet(string alias, out EditorMetadata metadata) =>
        _editors.TryGetValue(alias, out metadata!);

    public static EditorCatalog Load(ImmutableArray<AdditionalText> additionalFiles) {
        var metadataFile = additionalFiles.FirstOrDefault(static file =>
            file.Path.EndsWith("umbraco-property-editors.json", StringComparison.OrdinalIgnoreCase));
        if(metadataFile is null) {
            return Empty();
        }

        var content = metadataFile.GetText()?.ToString() ?? string.Empty;
        if(content.Length == 0) {
            return Empty();
        }

        try {
            using var document = JsonDocument.Parse(content);
            if(!document.RootElement.TryGetProperty("editors", out var editors)
                || editors.ValueKind != JsonValueKind.Array) {
                return Empty();
            }

            var builder = ImmutableDictionary.CreateBuilder<string, EditorMetadata>(
                StringComparer.Ordinal);
            foreach(var editor in editors.EnumerateArray()) {
                var alias = GetJsonString(editor, "alias");
                var returnType = GetJsonString(editor, "returnType");
                if(!string.IsNullOrWhiteSpace(alias) && !string.IsNullOrWhiteSpace(returnType)) {
                    builder[alias!] = new EditorMetadata(alias!, returnType!);
                }
            }

            return new EditorCatalog(builder.ToImmutable());
        } catch(JsonException) {
            return Empty();
        }
    }

    private static EditorCatalog Empty() =>
        new(ImmutableDictionary<string, EditorMetadata>.Empty);

    private static string? GetJsonString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}

internal sealed class EditorMetadata(string alias, string returnType) {
    public string Alias { get; } = alias;
    public string ReturnType { get; } = returnType;
}
