using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

internal static class CompositionAnalyzerHelpers {
    public static void ReportInvalidGraph(
        CompilationAnalysisContext context,
        IEnumerable<CompositionOccurrence> compositions) {
        var validCompositions = compositions
            .Where(static occurrence => occurrence.TargetHasDocumentType)
            .ToArray();

        foreach(var duplicate in validCompositions
                    .GroupBy(static occurrence =>
                        $"{occurrence.Source.ToDisplayString()}:{occurrence.Target.ToDisplayString()}")) {
            var entries = duplicate.ToArray();
            if(entries.Length > 1) {
                Report(context, entries[1]);
            }
        }

        foreach(var occurrence in validCompositions.Where(static occurrence =>
                    SymbolEqualityComparer.Default.Equals(occurrence.Source, occurrence.Target))) {
            Report(context, occurrence);
        }

        var graph = new Dictionary<INamedTypeSymbol, List<CompositionOccurrence>>(
            SymbolEqualityComparer.Default);
        foreach(var occurrence in validCompositions) {
            if(!graph.TryGetValue(occurrence.Source, out var edges)) {
                edges = [];
                graph.Add(occurrence.Source, edges);
            }

            edges.Add(occurrence);
        }

        var visiting = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach(var source in graph.Keys) {
            Visit(source, graph, visiting, visited, context);
        }
    }

    private static void Visit(
        INamedTypeSymbol source,
        IReadOnlyDictionary<INamedTypeSymbol, List<CompositionOccurrence>> graph,
        HashSet<INamedTypeSymbol> visiting,
        HashSet<INamedTypeSymbol> visited,
        CompilationAnalysisContext context) {
        if(visited.Contains(source) || !visiting.Add(source)) {
            return;
        }

        if(graph.TryGetValue(source, out var edges)) {
            foreach(var edge in edges) {
                if(visiting.Contains(edge.Target)) {
                    Report(context, edge);
                    continue;
                }

                Visit(edge.Target, graph, visiting, visited, context);
            }
        }

        visiting.Remove(source);
        visited.Add(source);
    }

    private static void Report(
        CompilationAnalysisContext context,
        CompositionOccurrence occurrence) {
        context.ReportDiagnostic(Diagnostic.Create(
            CodeFirstDiagnosticDescriptors.InvalidCompositionGraph,
            occurrence.Location ?? occurrence.Source.Locations.FirstOrDefault(),
            occurrence.Source.Name,
            occurrence.Target.Name));
    }
}
