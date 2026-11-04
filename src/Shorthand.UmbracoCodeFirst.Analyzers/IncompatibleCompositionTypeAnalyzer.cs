using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IncompatibleCompositionTypeAnalyzer : DiagnosticAnalyzer {
    public override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [CodeFirstDiagnosticDescriptors.IncompatibleCompositionType];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilationContext => {
            var compositions = new ConcurrentBag<CompositionOccurrence>();
            compilationContext.RegisterSymbolAction(
                symbolContext => Collect(symbolContext.Symbol, compositions),
                SymbolKind.NamedType);
            compilationContext.RegisterCompilationEndAction(endContext => {
                foreach(var occurrence in compositions.Where(static item =>
                    item.TargetHasDocumentType && item.SourceIsElement != item.TargetIsElement)) {
                    endContext.ReportDiagnostic(Diagnostic.Create(
                        CodeFirstDiagnosticDescriptors.IncompatibleCompositionType,
                        occurrence.Location ?? occurrence.Source.Locations.FirstOrDefault(),
                        occurrence.Source.Name,
                        occurrence.Target.Name));
                }
            });
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
