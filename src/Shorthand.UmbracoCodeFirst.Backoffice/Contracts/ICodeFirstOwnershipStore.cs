using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Contracts;

public interface ICodeFirstOwnershipStore {
    CodeFirstDocumentTypeOwnership? Get(string alias, Guid documentTypeKey);

    void Set(string alias, Guid documentTypeKey, IEnumerable<string> compositionAliases);
}
