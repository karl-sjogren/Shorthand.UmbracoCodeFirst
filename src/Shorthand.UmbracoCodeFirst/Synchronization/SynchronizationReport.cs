namespace Shorthand.UmbracoCodeFirst.Synchronization;

public sealed record SynchronizationReport(
        DateTimeOffset CompletedAt,
        IReadOnlyList<SynchronizationItem> Items) {
    public int CreatedCount => Items.Count(item => item.Status == SynchronizationStatus.Created);

    public int UpdatedCount => Items.Count(item => item.Status == SynchronizationStatus.Updated);

    public int UnchangedCount => Items.Count(item => item.Status == SynchronizationStatus.Unchanged);

    public int FailedCount => Items.Count(item => item.Status == SynchronizationStatus.Failed);

    public int SkippedCount => Items.Count(item => item.Status == SynchronizationStatus.Skipped);
}
