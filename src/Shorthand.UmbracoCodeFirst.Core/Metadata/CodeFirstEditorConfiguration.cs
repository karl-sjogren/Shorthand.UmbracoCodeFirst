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
    [JsonIgnore]
    public IReadOnlyList<CodeFirstBlockType>? BlockTypes { get; init; }

    [JsonIgnore]
    public IReadOnlyList<CodeFirstBlockGroup>? BlockGroups { get; init; }

    [JsonIgnore]
    public IReadOnlyDictionary<string, JsonElement>? TypedSettings { get; init; }

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
        var options = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        var json = JsonSerializer.Serialize(definition, configurationType, options);
        var result = FromJson(
            definition.EditorId ?? editorId,
            json,
            definition.SchemaVersion > 0 ? definition.SchemaVersion : schemaVersion);
        if(definition is not CodeFirstBlockEditorConfigurationDefinition block) {
            return result;
        }

        Dictionary<string, JsonElement> typedSettings = [];
        AddSetting("validationLimit", block.ValidationRange);
        AddSetting("toolbar", block.ToolbarRows);
        AddSetting("statusbar", block.StatusbarGroups);
        AddSetting("dimensions", block.EditorDimensions);
        AddSetting("stylesheets", block.StylesheetPaths);
        return result with {
            BlockTypes = block.BlockTypes,
            BlockGroups = block.Groups,
            TypedSettings = typedSettings.Count > 0 ? typedSettings : null,
        };

        void AddSetting<T>(string name, T? value) where T : class {
            if(value is not null) {
                typedSettings.Add(name, JsonSerializer.SerializeToElement(value, options));
            }
        }
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
