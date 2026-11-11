using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;

internal static class DefinitionFingerprint {
    public static string Create(IReadOnlyList<CodeFirstDefinition> definitions) {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ElementReferenceConverter());
        var entries = definitions.Select(definition => JsonSerializer.Serialize(new {
            Definition = definition,
            BlockTypes = definition.EditorConfiguration?.BlockTypes,
            BlockGroups = definition.EditorConfiguration?.BlockGroups,
            TypedSettings = definition.EditorConfiguration?.TypedSettings,
        }, options)).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(entries)));
    }

    private sealed class ElementReferenceConverter : JsonConverter<CodeFirstElementReference> {
        public override CodeFirstElementReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, CodeFirstElementReference value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteString("alias", value.Alias);
            writer.WriteString("type", value.ModelType?.FullName);
            writer.WriteString("assembly", value.ModelType?.Assembly.GetName().Name);
            writer.WriteEndObject();
        }
    }
}
