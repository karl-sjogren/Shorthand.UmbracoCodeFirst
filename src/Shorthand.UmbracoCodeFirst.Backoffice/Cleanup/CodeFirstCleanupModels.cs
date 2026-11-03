namespace Shorthand.UmbracoCodeFirst.Backoffice.Cleanup;

public sealed record CodeFirstCleanupResponse(
    IReadOnlyList<CodeFirstCleanupItem> Items);

public sealed record CodeFirstCleanupItem(
    string Kind,
    string Alias,
    string Name,
    string? ParentAlias = null);

public sealed record CodeFirstCleanupResult(
    int RemovedCount,
    IReadOnlyList<CodeFirstCleanupItem> Removed,
    IReadOnlyList<CodeFirstCleanupFailure> Failed);

public sealed record CodeFirstCleanupFailure(
    CodeFirstCleanupItem Item,
    string Message);
