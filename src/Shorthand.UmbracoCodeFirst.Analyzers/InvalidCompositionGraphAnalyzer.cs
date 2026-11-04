using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InvalidCompositionGraphAnalyzer : DiagnosticAnalyzer {
    public override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [CodeFirstDiagnosticDescriptors.InvalidCompositionGraph];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilationContext => {
            var compositions = new ConcurrentBag<CompositionOccurrence>();
            compilationContext.RegisterSymbolAction(
                symbolContext => Collect(symbolContext.Symbol, compositions),
                SymbolKind.NamedType);
            compilationContext.RegisterCompilationEndAction(endContext =>
                CompositionAnalyzerHelpers.ReportInvalidGraph(endContext, compositions));
        });
    }

    private static void Collect(
        ISymbol symbol,
        ConcurrentBag<CompositionOccurrence> compositions) {
        if(symbol is not INamedTypeSymbol source) {
            return;
        }

        foreach(var attribute in source.GetAttributes()) {
            if(CodeFirstAttributeParser.TryGetComposition(attribute, source, out var composition)) {
                compositions.Add(composition);
            }
        }
    }
}
