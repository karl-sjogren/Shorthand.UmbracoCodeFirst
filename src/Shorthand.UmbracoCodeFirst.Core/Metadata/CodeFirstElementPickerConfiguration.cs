using System.Text.Json;

namespace Shorthand.UmbracoCodeFirst.Metadata;

public static class CodeFirstElementPickerConfiguration {
    public static bool HasSettings(CodeFirstEditorConfiguration? configuration) {
        if(configuration?.EditorId != "Umbraco.ElementPicker") {
            return false;
        }

        using var document = JsonDocument.Parse(configuration.Json);
        var root = document.RootElement;
        return root.TryGetProperty("filter", out _)
            || root.TryGetProperty("minNumber", out _)
            || root.TryGetProperty("maxNumber", out _);
    }

    public static IReadOnlyList<string> FilterValues(CodeFirstEditorConfiguration configuration) {
        using var document = JsonDocument.Parse(configuration.Json);
        if(!document.RootElement.TryGetProperty("filter", out var filter) || filter.ValueKind == JsonValueKind.Null) {
            return [];
        }

        if(filter.ValueKind == JsonValueKind.String) {
            return filter.GetString()!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        if(filter.ValueKind != JsonValueKind.Array) {
            throw new InvalidOperationException("The element picker filter must be a collection of element aliases or keys.");
        }

        var values = new List<string>();
        foreach(var value in filter.EnumerateArray()) {
            if(value.ValueKind != JsonValueKind.String) {
                throw new InvalidOperationException("The element picker filter must contain only element aliases or keys as strings.");
            }

            var entry = value.GetString()!.Trim();
            if(entry.Length > 0) {
                values.Add(entry);
            }
        }

        return values;
    }

    public static IEnumerable<CodeFirstElementReference> GetReferences(CodeFirstEditorConfiguration configuration) =>
        HasSettings(configuration)
            ? FilterValues(configuration).Where(value => !Guid.TryParse(value, out _)).Select(value => new CodeFirstElementReference(value))
            : [];
}
