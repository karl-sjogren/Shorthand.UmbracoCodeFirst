using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Shorthand.UmbracoCodeFirst.SourceGenerators;

public sealed partial class CodeFirstRegistryGenerator {
    private static readonly DiagnosticDescriptor _compositeMemberConflict = new(
        "UCF125",
        "Conflicting generated composite member",
        "Cannot generate composite model for '{0}': member '{1}' conflicts with {2}",
        "CodeFirst",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static Dictionary<INamedTypeSymbol, ImmutableArray<INamedTypeSymbol>> BuildCompositionGraph(
        ImmutableArray<INamedTypeSymbol> definitions) {
        var models = definitions.Where(static definition => FindDocumentTypeAttribute(definition) is not null).ToArray();
        var modelSet = new HashSet<INamedTypeSymbol>(models, SymbolEqualityComparer.Default);
        var graph = new Dictionary<INamedTypeSymbol, ImmutableArray<INamedTypeSymbol>>(SymbolEqualityComparer.Default);
        foreach(var model in models) {
            var isElement = IsAttribute(FindDocumentTypeAttribute(model)!, _elementTypeAttributeMetadataName);
            graph[model] = model.GetAttributes()
                .Where(static attribute => IsAttribute(attribute, _compositeAttributeMetadataName))
                .Select(static attribute => attribute.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol)
                .Where(target => target is not null && modelSet.Contains(target)
                    && IsAttribute(FindDocumentTypeAttribute(target)!, _elementTypeAttributeMetadataName) == isElement)
                .Cast<INamedTypeSymbol>()
                .Distinct(SymbolEqualityComparer.Default)
                .Cast<INamedTypeSymbol>()
                .OrderBy(static target => target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
                .ToImmutableArray();
        }

        // Cyclic edges are diagnosed by the analyzer and cannot form C# interface inheritance.
        var acyclicGraph = new Dictionary<INamedTypeSymbol, ImmutableArray<INamedTypeSymbol>>(SymbolEqualityComparer.Default);
        foreach(var model in models) {
            acyclicGraph[model] = graph[model].Where(target =>
                !Reaches(target, model, graph, new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default)))
                .ToImmutableArray();
        }

        return acyclicGraph;
    }

    private static bool Reaches(
        INamedTypeSymbol current,
        INamedTypeSymbol target,
        IReadOnlyDictionary<INamedTypeSymbol, ImmutableArray<INamedTypeSymbol>> graph,
        HashSet<INamedTypeSymbol> visited) {
        if(SymbolEqualityComparer.Default.Equals(current, target)) {
            return true;
        }

        return visited.Add(current) && graph.TryGetValue(current, out var next)
            && next.Any(item => Reaches(item, target, graph, visited));
    }

    private static ImmutableArray<INamedTypeSymbol> GetEffectiveCompositions(
        INamedTypeSymbol model,
        IReadOnlyDictionary<INamedTypeSymbol, ImmutableArray<INamedTypeSymbol>> graph) {
        var result = new List<INamedTypeSymbol>();
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default) { model };
        void Visit(INamedTypeSymbol current) {
            if(!graph.TryGetValue(current, out var targets)) {
                return;
            }

            foreach(var target in targets) {
                if(visited.Add(target)) {
                    result.Add(target);
                    Visit(target);
                }
            }
        }

        Visit(model);
        return result.ToImmutableArray();
    }

    private static IEnumerable<IPropertySymbol> GetDeclaredModelProperties(INamedTypeSymbol model) =>
        model.GetMembers().OfType<IPropertySymbol>()
            .Where(static property => FindAttribute(property, _propertyAttributeMetadataName) is not null)
            .OrderBy(static property => property.Name, StringComparer.Ordinal);

    private static string GetCompositeInterfaceName(INamedTypeSymbol model) =>
        (model.ContainingNamespace.IsGlobalNamespace
            ? "global::"
            : $"global::{model.ContainingNamespace.ToDisplayString()}.") + EscapeIdentifier("I" + model.Name);

    private static void RemoveConflictingCompositions(
        SourceProductionContext context,
        Dictionary<INamedTypeSymbol, ImmutableArray<INamedTypeSymbol>> graph,
        Compilation compilation) {
        var targets = new HashSet<INamedTypeSymbol>(graph.Values.SelectMany(static value => value), SymbolEqualityComparer.Default);
        var invalid = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach(var model in graph.Keys) {
            var inheritedProperties = GetEffectiveCompositions(model, graph)
                .SelectMany(GetDeclaredModelProperties).ToArray();
            var allProperties = GetDeclaredModelProperties(model).Concat(inheritedProperties).ToArray();
            foreach(var group in allProperties.GroupBy(static property => property.Name, StringComparer.Ordinal)) {
                if(group.Count() > 1 && group.Any(property =>
                    !SymbolEqualityComparer.Default.Equals(property.ContainingType, model))) {
                    Report(group.Key, "another composite property");
                }
            }

            foreach(var property in inheritedProperties) {
                var baseName = IsAttribute(FindDocumentTypeAttribute(model)!, _elementTypeAttributeMetadataName)
                    ? "Umbraco.Cms.Core.Models.PublishedContent.PublishedElementModel"
                    : "Umbraco.Cms.Core.Models.PublishedContent.PublishedContentModel";
                var baseType = compilation.GetTypeByMetadataName(baseName);
                if(model.GetMembers(property.Name).Any(member =>
                    member is not IPropertySymbol existing
                    || FindAttribute(existing, _propertyAttributeMetadataName) is null)
                    || HasAccessibleMember(baseType, property.Name)
                    || property.Name == model.Name
                    || property.Name is "PropertyAliases" or "ModelTypeAlias" or "ModelItemType"
                        or "GetModelContentType" or "GetModelPropertyType" or "_publishedValueFallback") {
                    Report(property.Name, "an existing member");
                }
            }

            if(targets.Contains(model)) {
                var interfaceName = "I" + model.Name;
                if(model.ContainingNamespace.GetTypeMembers(interfaceName).Any()) {
                    Report(interfaceName, "an existing type");
                }

                foreach(var property in GetDeclaredModelProperties(model)) {
                    var getter = "Get" + property.Name;
                    if(model.GetMembers(getter).Any()
                        || getter is "GetModelContentType" or "GetModelPropertyType"
                        || allProperties.Any(candidate => candidate.Name == getter)) {
                        Report(getter, "an existing or generated member");
                    }
                }
            }

            void Report(string member, string conflict) {
                context.ReportDiagnostic(Diagnostic.Create(
                    _compositeMemberConflict, model.Locations.FirstOrDefault(),
                    model.ToDisplayString(), member, conflict));
                invalid.Add(model);
            }
        }

        foreach(var model in graph.Keys.ToArray()) {
            graph[model] = invalid.Contains(model)
                ? ImmutableArray<INamedTypeSymbol>.Empty
                : graph[model].Where(target => !invalid.Contains(target)).ToImmutableArray();
        }
    }

    private static bool HasAccessibleMember(INamedTypeSymbol? type, string name) {
        for(var current = type; current is not null; current = current.BaseType) {
            if(current.GetMembers(name).Any(static member => member.DeclaredAccessibility is
                Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal)) {
                return true;
            }
        }

        return false;
    }
}
