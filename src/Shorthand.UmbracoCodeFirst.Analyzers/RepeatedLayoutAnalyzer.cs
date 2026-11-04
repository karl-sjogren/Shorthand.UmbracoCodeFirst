using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RepeatedLayoutAnalyzer : DiagnosticAnalyzer {
    public override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [CodeFirstDiagnosticDescriptors.DuplicateLayout];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilationContext => {
            var layouts = new ConcurrentBag<AliasOccurrence>();
            compilationContext.RegisterSymbolAction(
                symbolContext => Collect(symbolContext.Symbol, layouts),
                SymbolKind.NamedType,
                SymbolKind.Property);
            compilationContext.RegisterCompilationEndAction(endContext => {
                foreach(var group in layouts
                            .GroupBy(static layout =>
                                $"{CodeFirstAnalyzerHelpers.GetScopeName(layout)}:{layout.LayoutKind}:{layout.Alias}")) {
                    var entries = group.ToArray();
                    if(entries.Length > 1 && entries[0].LayoutKind is not null) {
                        endContext.ReportDiagnostic(Diagnostic.Create(
                            CodeFirstDiagnosticDescriptors.DuplicateLayout,
                            entries[1].Location,
                            entries[0].LayoutKind,
                            entries[0].Alias));
                    }
                }
            });
        });
    }

    private static void Collect(ISymbol symbol, ConcurrentBag<AliasOccurrence> layouts) {
        foreach(var attribute in symbol.GetAttributes()) {
            if(CodeFirstAttributeParser.TryGetLayout(attribute, symbol, out var layout)) {
                layouts.Add(layout);
            }
        }
    }
}
