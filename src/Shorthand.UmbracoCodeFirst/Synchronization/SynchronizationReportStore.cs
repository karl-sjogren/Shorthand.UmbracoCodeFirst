using Shorthand.UmbracoCodeFirst.Contracts;

namespace Shorthand.UmbracoCodeFirst.Synchronization;

public sealed class SynchronizationReportStore : ISynchronizationReportStore {
    private SynchronizationReport _current = new(DateTimeOffset.MinValue, []);

    public SynchronizationReport Current => Volatile.Read(ref _current);

    public void Set(SynchronizationReport report) => Volatile.Write(ref _current, report);
}
