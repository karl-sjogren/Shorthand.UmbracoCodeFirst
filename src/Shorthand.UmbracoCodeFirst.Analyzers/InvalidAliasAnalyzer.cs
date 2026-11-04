using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InvalidAliasAnalyzer : DiagnosticAnalyzer {
    public override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [CodeFirstDiagnosticDescriptors.InvalidAlias];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType, SymbolKind.Property);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context) {
        foreach(var attribute in context.Symbol.GetAttributes()) {
            if((CodeFirstAttributeParser.TryGetAlias(attribute, context.Symbol, out var alias)
                || CodeFirstAttributeParser.TryGetLayout(attribute, context.Symbol, out alias))
                && !alias.IsValid) {
                context.ReportDiagnostic(Diagnostic.Create(
                    CodeFirstDiagnosticDescriptors.InvalidAlias,
                    alias.Location ?? context.Symbol.Locations.FirstOrDefault(),
                    alias.Alias));
            }
        }
    }
}
