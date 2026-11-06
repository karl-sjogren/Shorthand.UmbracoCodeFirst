using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;

[UCFDocumentType("integrationRichDocument", "Integration Rich Document", Icon = UmbracoIcons.Document)]
[UCFDescription("Initial rich document description")]
[UCFFolderPath("Integration/Rich")]
public partial class IntegrationRichDocument {
    [UCFProperty("headline", "Headline", IntegrationTextDataType.Alias, SortOrder = 0, Mandatory = true)]
    [UCFDescription("Initial headline description")]
    [UCFGroup("content", "Content", 0)]
    [UCFTab("details", "Details", 0)]
    [UCFValidation(@"^.{3,}$", "Headline must contain at least three characters.")]
    public string Headline { get; } = string.Empty;

    [UCFProperty("count", "Count", IntegrationIntegerDataType.Alias, SortOrder = 1, LabelOnTop = true)]
    [UCFGroup("content", "Content", 0)]
    public int Count { get; }
}
