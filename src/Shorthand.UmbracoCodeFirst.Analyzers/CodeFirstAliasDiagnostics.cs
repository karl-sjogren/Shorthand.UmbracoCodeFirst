using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

internal static class CodeFirstAliasDiagnostics {
    public static void ReportDuplicateAliases(
        CompilationAnalysisContext context,
        IEnumerable<AliasOccurrence> occurrences,
        IEnumerable<CompositionOccurrence> compositions) {
        var allOccurrences = occurrences.ToArray();
        foreach(var group in allOccurrences
                    .Where(static occurrence =>
                        occurrence.Symbol is not IPropertySymbol || occurrence.LayoutKind is not null)
                    .GroupBy(static occurrence =>
                        $"{occurrence.Scope}:{CodeFirstAnalyzerHelpers.GetScopeName(occurrence)}:{occurrence.Alias}")) {
            var entries = group
                .OrderBy(static occurrence => occurrence.Location?.SourceSpan.Start ?? int.MaxValue)
                .ToArray();
            if(entries.Length > 1) {
                var first = entries[0];
                context.ReportDiagnostic(Diagnostic.Create(
                    CodeFirstDiagnosticDescriptors.DuplicateAlias,
                    entries[1].Location,
                    first.Alias));
            }
        }

        var propertyOccurrences = allOccurrences
            .Where(static occurrence => occurrence.Symbol is IPropertySymbol)
            .ToArray();
        foreach(var group in propertyOccurrences
                    .GroupBy(static occurrence =>
                        $"{CodeFirstAnalyzerHelpers.GetScopeName(occurrence)}:{occurrence.Alias}")) {
            ReportIfDuplicate(context, group);
        }

        var graph = BuildCompositionGraph(compositions);
        var documentTypes = propertyOccurrences
            .Select(static occurrence => occurrence.Symbol.ContainingType)
            .OfType<INamedTypeSymbol>()
            .Where(static type => CodeFirstAnalyzerHelpers.HasDocumentTypeAttribute(type))
            .Concat(graph.Keys)
            .Concat(graph.Values.SelectMany(static targets => targets))
            .Distinct(SymbolEqualityComparer.Default)
            .OfType<INamedTypeSymbol>();

        foreach(var documentType in documentTypes) {
            var effectiveTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            CollectEffectiveCompositionTypes(documentType, graph, effectiveTypes);
            foreach(var group in propertyOccurrences
                        .Where(occurrence => occurrence.Symbol.ContainingType is INamedTypeSymbol containingType
                                             && effectiveTypes.Contains(containingType))
                        .GroupBy(static occurrence => occurrence.Alias)) {
                if(group.Select(static entry => entry.Symbol.ContainingType)
                    .Distinct(SymbolEqualityComparer.Default)
                    .Count() > 1) {
                    ReportIfDuplicate(context, group);
                }
            }
        }
    }

    private static void ReportIfDuplicate(
        CompilationAnalysisContext context,
        IEnumerable<AliasOccurrence> group) {
        var entries = group
            .OrderBy(static occurrence => occurrence.Location?.SourceSpan.Start ?? int.MaxValue)
            .ToArray();
        if(entries.Length > 1) {
            context.ReportDiagnostic(Diagnostic.Create(
                CodeFirstDiagnosticDescriptors.DuplicateAlias,
                entries[1].Location ?? entries[1].Symbol.Locations.FirstOrDefault(),
                entries[1].Alias));
        }
    }

    private static Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>> BuildCompositionGraph(
        IEnumerable<CompositionOccurrence> compositions) {
        var graph = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>(
            SymbolEqualityComparer.Default);
        foreach(var composition in compositions.Where(static composition => composition.TargetHasDocumentType)) {
            if(!graph.TryGetValue(composition.Source, out var targets)) {
                targets = [];
                graph.Add(composition.Source, targets);
            }

            targets.Add(composition.Target);
        }

        return graph;
    }

    private static void CollectEffectiveCompositionTypes(
        INamedTypeSymbol source,
        IReadOnlyDictionary<INamedTypeSymbol, List<INamedTypeSymbol>> graph,
        HashSet<INamedTypeSymbol> effectiveTypes) {
        if(!effectiveTypes.Add(source)
            || !graph.TryGetValue(source, out var targets)) {
            return;
        }

        foreach(var target in targets) {
            CollectEffectiveCompositionTypes(target, graph, effectiveTypes);
        }
    }
}
