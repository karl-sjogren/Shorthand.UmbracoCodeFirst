using Shorthand.UmbracoCodeFirst.Synchronization;

namespace Shorthand.UmbracoCodeFirst.Contracts;

public interface ISynchronizationReportStore {
    SynchronizationReport Current { get; }

    void Set(SynchronizationReport report);
}
