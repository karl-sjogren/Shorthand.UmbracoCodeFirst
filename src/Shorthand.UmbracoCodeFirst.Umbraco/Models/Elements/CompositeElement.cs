using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.Elements;

[UCFElementType(
    "CompositeElement",
    Icon = "icon-document",
    AllowInLibrary = false)]
[UCFDescription("A really cool element")]
[UCFFolderPath("Elements/Compositions/In/A/Deep/Folders")]
public partial class CompositeElement {
    [UCFProperty(
        "Copyright year",
        CodeFirstIntegerDataType.Alias,
        SortOrder = 1)]
    [UCFDescription("The copyright year.")]
    public partial int CopyrightYear { get; }
}
