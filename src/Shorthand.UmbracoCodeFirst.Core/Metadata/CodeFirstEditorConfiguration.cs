using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shorthand.UmbracoCodeFirst.Metadata;

/// <summary>
/// Describes normalized configuration for a property editor without referencing Umbraco runtime
/// configuration types.
/// </summary>
public sealed record CodeFirstEditorConfiguration(
    string EditorId,
    string Json,
    int SchemaVersion = 1) {
    public static CodeFirstEditorConfiguration FromJson(
        string editorId,
        string json,
        int schemaVersion = 1) {
        ArgumentException.ThrowIfNullOrWhiteSpace(editorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        if(schemaVersion < 1) {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }

        using var document = JsonDocument.Parse(json);
        if(document.RootElement.ValueKind != JsonValueKind.Object) {
            throw new ArgumentException(
                "Editor configuration JSON must contain a JSON object.",
                nameof(json));
        }

        return new CodeFirstEditorConfiguration(
            editorId,
            Canonicalize(document.RootElement),
            schemaVersion);
    }

    public static CodeFirstEditorConfiguration FromType(
        Type configurationType,
        string editorId,
        int schemaVersion = 1) {
        ArgumentNullException.ThrowIfNull(configurationType);
        ArgumentException.ThrowIfNullOrWhiteSpace(editorId);

        if(!typeof(CodeFirstEditorConfigurationDefinition).IsAssignableFrom(configurationType)
            || configurationType.IsAbstract
            || configurationType.GetConstructor(Type.EmptyTypes) is null) {
            throw new ArgumentException(
                $"Configuration type '{configurationType.FullName}' must be a concrete type derived from {nameof(CodeFirstEditorConfigurationDefinition)} with a public parameterless constructor.",
                nameof(configurationType));
        }

        var instance = Activator.CreateInstance(configurationType)
            ?? throw new InvalidOperationException(
                $"Could not create editor configuration '{configurationType.FullName}'.");
        var definition = (CodeFirstEditorConfigurationDefinition)instance;
        var json = JsonSerializer.Serialize(
            definition,
            configurationType,
            new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            });
        return FromJson(
            definition.EditorId ?? editorId,
            json,
            definition.SchemaVersion > 0 ? definition.SchemaVersion : schemaVersion);
    }

    private static string Canonicalize(JsonElement element) {
        using var stream = new MemoryStream();
        using(var writer = new Utf8JsonWriter(stream)) {
            WriteCanonical(writer, element);
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element) {
        switch(element.ValueKind) {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach(var property in element.EnumerateObject().OrderBy(
                    property => property.Name,
                    StringComparer.Ordinal)) {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(writer, property.Value);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach(var item in element.EnumerateArray()) {
                    WriteCanonical(writer, item);
                }

                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}
