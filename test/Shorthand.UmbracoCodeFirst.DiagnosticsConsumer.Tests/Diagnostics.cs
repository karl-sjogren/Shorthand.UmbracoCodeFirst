using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.DiagnosticsConsumer.Tests;

#if BAD108
[UCFDataType("Picker", "Umbraco.UnknownPicker", "Umb.PropertyEditorUi.MediaPicker")]
#else
[UCFDataType("Picker", typeof(Shorthand.UmbracoCodeFirst.PropertyEditors.UmbracoEditors.MediaPicker), ConfigurationJson = "{\"multiple\":false}")]
#endif
public partial class Picker;

#if BAD119
[UCFDocument("Page", DefaultTemplateAlias = "page", AllowedTemplateAliases = new[] { "other" })]
#else
[UCFDocument("Page", DefaultTemplateAlias = "page", AllowedTemplateAliases = new[] { "page" })]
#endif
public partial class Page {
#if BAD123
    [UCFProperty("Image")]
#else
    [UCFProperty("Image", Picker.Alias)]
#endif
#if BAD109
    public partial string? Image { get; }
#else
    public partial Umbraco.Cms.Core.Models.MediaWithCrops? Image { get; }
#endif

    [UCFProperty("Built-in image", DataTypeKey = "ad9f0cf2-bda2-45d5-9ea1-a63cfc873fd3")]
    public partial Umbraco.Cms.Core.Models.MediaWithCrops? BuiltInImage { get; }
}
