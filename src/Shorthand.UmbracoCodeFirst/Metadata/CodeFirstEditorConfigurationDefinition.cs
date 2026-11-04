using System.Text.Json.Serialization;

namespace Shorthand.UmbracoCodeFirst.Metadata;

public abstract class CodeFirstEditorConfigurationDefinition {
    [JsonIgnore]
    public virtual string? EditorId => null;

    [JsonIgnore]
    public virtual int SchemaVersion => 1;
}
