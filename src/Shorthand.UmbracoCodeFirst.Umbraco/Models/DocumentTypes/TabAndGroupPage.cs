using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.DocumentTypes;

[UCFDocument(
    "Tab And Group Page",
    Icon = UmbracoIcons.Home,
    AllowAsRoot = true)]
[UCFDescription("A page used to test tabs and groups.")]
public partial class TabAndGroupPage {
    [UCFProperty(
        "Coolest",
        CodeFirstTextDataType.Alias,
        SortOrder = 0,
        Mandatory = true)]
    [CoolTab]
    [CoolGroup]
    public partial string Coolest { get; }

    [UCFProperty(
        "Kinda Cool",
        CodeFirstTextDataType.Alias,
        SortOrder = 0,
        Mandatory = true)]
    [CoolTab]
    [UCFGroup("KindaCoolGroup")]
    public partial string KindaCool { get; }

    [UCFProperty(
        "Somewhat Cool",
        CodeFirstTextDataType.Alias,
        SortOrder = 0,
        Mandatory = true)]
    [CoolTab]
    [UCFGroup("KindaCoolGroup")]
    public partial string SomewhatCool { get; }

    [UCFProperty(
        "Not Cool",
        CodeFirstTextDataType.Alias,
        SortOrder = 0,
        Mandatory = true)]
    [NotCoolTabAttribute]
    [UCFGroup("KindaCoolGroup")]
    public partial string NotCool { get; }
}

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
internal sealed class CoolTabAttribute()
    : UCFTabAttribute("CoolTab");

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
internal sealed class NotCoolTabAttribute()
    : UCFTabAttribute("NotCoolTab");

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
internal sealed class CoolGroupAttribute()
    : UCFGroupAttribute("CoolGroup");
