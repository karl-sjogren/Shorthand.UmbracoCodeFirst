using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Contracts;

public interface ICodeFirstGeneratedSourceService {
    string? GetId(int definitionIndex);

    IReadOnlyList<CodeFirstGeneratedSource>? GetSources(string id);
}
