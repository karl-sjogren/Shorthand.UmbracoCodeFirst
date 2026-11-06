namespace Shorthand.UmbracoCodeFirst.Backoffice.Cleanup;

public sealed record CodeFirstCleanupResponse(
    IReadOnlyList<CodeFirstCleanupItem> Items);
