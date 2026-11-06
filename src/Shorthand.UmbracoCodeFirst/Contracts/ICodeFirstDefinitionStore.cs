using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;

namespace Shorthand.UmbracoCodeFirst.Contracts;

public interface ICodeFirstDefinitionStore {
    Task<SynchronizationStatus> ApplyAsync(CodeFirstDefinition definition, CancellationToken cancellationToken);
}
