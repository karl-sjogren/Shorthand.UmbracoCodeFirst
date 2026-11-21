using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.Elements;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;

[UCFDataType(
    "Site Settings Picker",
    typeof(UmbracoEditors.ElementPicker),
    typeof(UmbracoBlogSiteSettingsPickerConfiguration))]
[UCFFolderPath("CodeFirst")]
public partial class UmbracoBlogSiteSettingsPickerDataType;

internal sealed class UmbracoBlogSiteSettingsPickerConfiguration : UmbracoEditors.ElementPicker.Configuration {
    public UmbracoBlogSiteSettingsPickerConfiguration() {
        MaxNumber = 1;
        Filter = [SiteSettingsElement.ModelTypeAlias];
    }
}
