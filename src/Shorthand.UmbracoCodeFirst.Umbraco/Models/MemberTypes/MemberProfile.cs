using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.DataTypes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Models.MemberTypes;

[UCFMemberType("Member Profile", Icon = "icon-user")]
[UCFFolderPath("Profiles")]
[UCFDescription("Profile fields managed by the code-first Member Type.")]
public partial class MemberProfile {
    [UCFProperty("Display Name", CodeFirstTextDataType.Alias, Mandatory = true)]
    [MemberProfileGroup]
    public string? DisplayName { get; set; }

    [UCFProperty("Biography", CodeFirstLongTextDataType.Alias)]
    [MemberProfileGroup]
    public string? Biography { get; set; }
}

internal sealed class MemberProfileGroupAttribute : UCFGroupAttribute {
    public MemberProfileGroupAttribute()
        : base("profile", "Profile") { }
}
