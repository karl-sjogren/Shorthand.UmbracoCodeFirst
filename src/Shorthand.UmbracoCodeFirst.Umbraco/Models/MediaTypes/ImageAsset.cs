using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.MediaTypes;

[UCFMediaType("Image Asset", Icon = UmbracoIcons.Picture, AllowAsRoot = true)]
[UCFFolderPath("Assets")]
[UCFDescription("Images managed by the code-first Media Type.")]
public partial class ImageAsset {
    [UCFProperty("Caption", CodeFirstTextDataType.Alias)]
    [ImageAssetGroup]
    public string? Caption { get; set; }

    [UCFProperty("Alt Text", CodeFirstTextDataType.Alias)]
    [ImageAssetGroup]
    public string? AltText { get; set; }
}

internal sealed class ImageAssetGroupAttribute : UCFGroupAttribute {
    public ImageAssetGroupAttribute()
        : base("image", "Image") { }
}
