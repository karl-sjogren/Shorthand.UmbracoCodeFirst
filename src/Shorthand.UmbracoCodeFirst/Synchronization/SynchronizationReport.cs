using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Synchronization;

public enum SynchronizationStatus {
    Created,
    Updated,
    Unchanged,
    Skipped,
    Failed,
}

public sealed record SynchronizationItem(
    CodeFirstDefinitionKind Kind,
    string Alias,
    string Name,
    SynchronizationStatus Status,
    string? Message = null);

public sealed record SynchronizationReport(
    DateTimeOffset CompletedAt,
    IReadOnlyList<SynchronizationItem> Items) {
    public int CreatedCount => Items.Count(item => item.Status == SynchronizationStatus.Created);

    public int UpdatedCount => Items.Count(item => item.Status == SynchronizationStatus.Updated);

    public int UnchangedCount => Items.Count(item => item.Status == SynchronizationStatus.Unchanged);

    public int FailedCount => Items.Count(item => item.Status == SynchronizationStatus.Failed);
}

public interface ISynchronizationReportStore {
    SynchronizationReport Current { get; }

    void Set(SynchronizationReport report);
}

public sealed class SynchronizationReportStore : ISynchronizationReportStore {
    private SynchronizationReport _current = new(DateTimeOffset.MinValue, []);

    public SynchronizationReport Current => Volatile.Read(ref _current);

    public void Set(SynchronizationReport report) => Volatile.Write(ref _current, report);
}
