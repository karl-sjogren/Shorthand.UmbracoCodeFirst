namespace Shorthand.UmbracoCodeFirst.Backoffice.Cleanup;

public sealed record CodeFirstCleanupItem(
    string Kind,
    string Alias,
    string Name,
    string? ParentAlias = null,
    string? ParentKind = null);
