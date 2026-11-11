namespace Shorthand.UmbracoCodeFirst.Synchronization;

public sealed record SynchronizationResult(
    SynchronizationStatus Status,
    string? Message = null);
