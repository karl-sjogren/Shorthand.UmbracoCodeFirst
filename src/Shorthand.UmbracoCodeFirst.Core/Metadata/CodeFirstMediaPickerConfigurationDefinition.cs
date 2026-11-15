using System.Text.Json.Serialization;

namespace Shorthand.UmbracoCodeFirst.Metadata;

public abstract class CodeFirstMediaPickerConfigurationDefinition : CodeFirstEditorConfigurationDefinition {
    /// <summary>Keys of accepted media types.</summary>
    [JsonIgnore]
    public List<Guid>? AcceptedMediaTypeKeys { get; set; }

    [JsonPropertyName("filter")]
    public string? Filter => AcceptedMediaTypeKeys is null ? null : string.Join(",", AcceptedMediaTypeKeys);

    /// <summary>Whether multiple media items can be selected.</summary>
    public bool? Multiple { get; set; }

    /// <summary>The minimum and maximum number of selected items.</summary>
    public CodeFirstMediaPickerValidationLimit? ValidationLimit { get; set; }

    /// <summary>The key of the media start node.</summary>
    public Guid? StartNodeId { get; set; }

    /// <summary>Whether local focal point editing is enabled.</summary>
    public bool? EnableLocalFocalPoint { get; set; }

    /// <summary>Local image crops.</summary>
    public List<CodeFirstMediaPickerCrop>? Crops { get; set; }

    /// <summary>Whether the picker overrides user media start-node restrictions.</summary>
    public bool? IgnoreUserStartNodes { get; set; }
}

public sealed record CodeFirstMediaPickerValidationLimit(int? Min = null, int? Max = null);

public sealed record CodeFirstMediaPickerCrop(string Alias, int Width, int Height);
