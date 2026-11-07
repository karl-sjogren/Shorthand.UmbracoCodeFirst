using Shorthand.UmbracoCodeFirst.Synchronization;

namespace Shorthand.UmbracoCodeFirst.Contracts;

public interface ICodeFirstSynchronizer {
    Task<SynchronizationReport> SynchronizeAsync(CancellationToken cancellationToken = default);
}
