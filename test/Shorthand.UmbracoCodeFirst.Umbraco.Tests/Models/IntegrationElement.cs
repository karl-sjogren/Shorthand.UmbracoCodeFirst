using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;

[UCFElementType("integrationElement", "Integration Element", AllowInLibrary = true, Icon = "icon-element")]
[UCFDescription("An integration element.")]
[UCFFolderPath("Integration/Elements")]
public partial class IntegrationElement {
    [UCFProperty("elementText", "Element text", IntegrationTextDataType.Alias)]
    [UCFGroup("content", "Content", 0)]
    public string ElementText { get; } = string.Empty;
}
