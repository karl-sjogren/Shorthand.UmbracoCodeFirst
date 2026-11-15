using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;
using Umbraco.Cms.Core.Models;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.Elements;

[UCFElementType("Image block", AllowInLibrary = true, Icon = UmbracoIcons.DocumentImage)]
[UCFFolderPath("Elements")]
public partial class ImageBlock {
    [UCFProperty("Image", DataTypeKey = global::Umbraco.Cms.Core.Constants.DataTypes.Guids.MediaPicker3SingleImage)]
    public partial MediaWithCrops? Image { get; }

    [UCFProperty("Alt text",
        CodeFirstTextDataType.Alias)]
    public partial string AltText { get; }
}
