namespace Shorthand.UmbracoCodeFirst.Backoffice.Cleanup;

public sealed record CodeFirstCleanupFailure(
    CodeFirstCleanupItem Item,
    string Message);
