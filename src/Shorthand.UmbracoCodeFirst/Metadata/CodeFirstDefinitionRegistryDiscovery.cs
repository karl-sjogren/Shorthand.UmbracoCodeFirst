using System.Reflection;

namespace Shorthand.UmbracoCodeFirst.Metadata;

public static class CodeFirstDefinitionRegistryDiscovery {
    public static ICodeFirstDefinitionRegistry Discover() {
        var registryType = typeof(ICodeFirstDefinitionRegistry);
        var implementation = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .FirstOrDefault(type =>
                !type.IsAbstract &&
                !type.IsInterface &&
                registryType.IsAssignableFrom(type) &&
                type.GetConstructor(Type.EmptyTypes) is not null);

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
