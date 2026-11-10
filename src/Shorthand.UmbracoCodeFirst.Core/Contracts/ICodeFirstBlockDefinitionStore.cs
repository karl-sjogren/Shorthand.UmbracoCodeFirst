using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;

namespace Shorthand.UmbracoCodeFirst.Contracts;

public interface ICodeFirstBlockDefinitionStore {
    Task<SynchronizationStatus> PrepareBlockDataTypeAsync(
        CodeFirstDefinition definition,
        CancellationToken cancellationToken);
}
