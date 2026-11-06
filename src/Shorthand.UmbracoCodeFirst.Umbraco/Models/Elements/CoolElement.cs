using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.Elements;

[UCFElementType(
    "CoolElement",
    Icon = UmbracoIcons.Document,
    AllowInLibrary = true)]
[UCFComposite(typeof(CompositeElement))]
[UCFDescription("A really cool element")]
[UCFFolderPath("Elements")]
public partial class CoolElement {
    [UCFProperty(
        "Name",
        CodeFirstTextDataType.Alias,
        SortOrder = 0,
        Mandatory = true,
        LabelOnTop = true)]
    [UCFDescription("The name.")]
    public partial string TheName { get; }
}
