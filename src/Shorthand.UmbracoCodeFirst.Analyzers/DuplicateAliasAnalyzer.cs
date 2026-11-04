using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicateAliasAnalyzer : DiagnosticAnalyzer {
    public override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [CodeFirstDiagnosticDescriptors.DuplicateAlias];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilationContext => {
            var aliases = new ConcurrentBag<AliasOccurrence>();
            var compositions = new ConcurrentBag<CompositionOccurrence>();
            compilationContext.RegisterSymbolAction(
                symbolContext => Collect(symbolContext.Symbol, aliases, compositions),
                SymbolKind.NamedType,
                SymbolKind.Property);
            compilationContext.RegisterCompilationEndAction(endContext =>
                CodeFirstAliasDiagnostics.ReportDuplicateAliases(endContext, aliases, compositions));
        });
    }

    private static void Collect(
        ISymbol symbol,
        ConcurrentBag<AliasOccurrence> aliases,
        ConcurrentBag<CompositionOccurrence> compositions) {
        foreach(var attribute in symbol.GetAttributes()) {
            if(CodeFirstAttributeParser.TryGetAlias(attribute, symbol, out var alias)
                || CodeFirstAttributeParser.TryGetLayout(attribute, symbol, out alias)) {
                aliases.Add(alias);
            }

            if(symbol is INamedTypeSymbol type
                && CodeFirstAttributeParser.TryGetComposition(attribute, type, out var composition)) {
                compositions.Add(composition);
            }
        }
    }
}
