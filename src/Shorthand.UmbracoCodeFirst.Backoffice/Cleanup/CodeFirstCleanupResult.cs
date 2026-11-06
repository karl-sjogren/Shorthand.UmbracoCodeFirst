namespace Shorthand.UmbracoCodeFirst.Backoffice.Cleanup;

public sealed record CodeFirstCleanupResult(
    int RemovedCount,
    IReadOnlyList<CodeFirstCleanupItem> Removed,
    IReadOnlyList<CodeFirstCleanupFailure> Failed);
