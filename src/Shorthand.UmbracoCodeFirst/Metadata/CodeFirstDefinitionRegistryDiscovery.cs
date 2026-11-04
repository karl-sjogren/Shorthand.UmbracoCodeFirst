using System.Reflection;

namespace Shorthand.UmbracoCodeFirst.Metadata;

public static class CodeFirstDefinitionRegistryDiscovery {
    public static ICodeFirstDefinitionRegistry Discover() {
        var registryType = typeof(ICodeFirstDefinitionRegistry);
        var implementations = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .Where(type =>
                !type.IsAbstract &&
                !type.IsInterface &&
                registryType.IsAssignableFrom(type) &&
                type.GetConstructor(Type.EmptyTypes) is not null)
            .ToArray();
        var implementation = implementations
            .OrderByDescending(type => type.Name == "GeneratedCodeFirstDefinitionRegistry")
            .ThenBy(type => type.Assembly.GetName().Name, StringComparer.Ordinal)
            .ThenBy(type => type.FullName, StringComparer.Ordinal)
            .FirstOrDefault();

        return implementation is null
            ? new EmptyCodeFirstDefinitionRegistry()
            : (ICodeFirstDefinitionRegistry)Activator.CreateInstance(implementation)!;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly) {
        try {
            return assembly.GetTypes();
        } catch(ReflectionTypeLoadException exception) {
            return exception.Types.OfType<Type>();
        }
    }

    private sealed class EmptyCodeFirstDefinitionRegistry : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = [];
    }
}
