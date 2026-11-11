using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;

namespace Shorthand.UmbracoCodeFirst.Contracts;

public interface ICodeFirstBlockDefinitionStore {
    Task<SynchronizationResult> PrepareBlockDataTypeAsync(
        CodeFirstDefinition definition,
        CancellationToken cancellationToken);
}
