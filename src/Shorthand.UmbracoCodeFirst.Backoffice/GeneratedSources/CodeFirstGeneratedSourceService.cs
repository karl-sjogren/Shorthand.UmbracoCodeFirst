using Microsoft.Extensions.Hosting;
using Shorthand.UmbracoCodeFirst.Backoffice.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Backoffice.GeneratedSources;

internal sealed class CodeFirstGeneratedSourceService : ICodeFirstGeneratedSourceService {
    private readonly ICodeFirstGeneratedSourceCatalog? _catalog;
    private readonly Dictionary<int, string> _ids = [];
    private readonly Dictionary<string, int> _indices = new(StringComparer.Ordinal);

    public CodeFirstGeneratedSourceService(ICodeFirstDefinitionRegistry registry, IHostEnvironment environment)
        : this(registry, environment, DiscoverCatalog(registry, environment)) { }

    internal CodeFirstGeneratedSourceService(
        ICodeFirstDefinitionRegistry registry,
        IHostEnvironment environment,
        ICodeFirstGeneratedSourceCatalog? catalog) {
        _catalog = environment.IsDevelopment() ? catalog : null;
        if(_catalog is null) {
            return;
        }

        for(var index = 0; index < registry.Definitions.Count; index++) {
            if(_catalog.GetSources(index).Count == 0) {
                continue;
            }

            var id = Guid.NewGuid().ToString("N");
            _ids.Add(index, id);
            _indices.Add(id, index);
        }
    }

    public string? GetId(int definitionIndex) => _ids.GetValueOrDefault(definitionIndex);

    public IReadOnlyList<CodeFirstGeneratedSource>? GetSources(string id) =>
        _indices.TryGetValue(id, out var index) ? _catalog?.GetSources(index) : null;

    private static ICodeFirstGeneratedSourceCatalog? DiscoverCatalog(
        ICodeFirstDefinitionRegistry registry,
        IHostEnvironment environment) {
        if(!environment.IsDevelopment()
            || registry.GetType().FullName != "Shorthand.UmbracoCodeFirst.Generated.GeneratedCodeFirstDefinitionRegistry") {
            return null;
        }

        var type = registry.GetType().Assembly.GetType("Shorthand.UmbracoCodeFirst.Generated.GeneratedCodeFirstSourceCatalog");
        return type is not null && typeof(ICodeFirstGeneratedSourceCatalog).IsAssignableFrom(type)
            ? (ICodeFirstGeneratedSourceCatalog?)Activator.CreateInstance(type)
            : null;
    }
}
