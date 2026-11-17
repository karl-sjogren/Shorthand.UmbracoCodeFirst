using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;

[UCFDataType(
    "Media Picker",
    typeof(UmbracoEditors.MediaPicker),
    typeof(UmbracoBlogMediaPickerDataType))]
[UCFFolderPath("CodeFirst")]
public partial class UmbracoBlogMediaPickerDataType;

public sealed partial class UmbracoBlogMediaPickerDataType : UmbracoEditors.MediaPicker.Configuration {
    public UmbracoBlogMediaPickerDataType() {
        AcceptedMediaTypeKeys = [UmbracoConstants.MediaTypes.Guids.ImageGuid];
        Multiple = true;
        ValidationLimit = new CodeFirstMediaPickerValidationLimit(Max: 3);
        EnableLocalFocalPoint = true;
        Crops = [new CodeFirstMediaPickerCrop("thumbnail", 400, 300)];
    }
}
