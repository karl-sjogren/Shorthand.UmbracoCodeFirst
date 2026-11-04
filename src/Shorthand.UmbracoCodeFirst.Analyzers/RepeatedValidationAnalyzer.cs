using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RepeatedValidationAnalyzer : DiagnosticAnalyzer {
    public override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [CodeFirstDiagnosticDescriptors.DuplicateValidation];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilationContext => {
            var validations = new ConcurrentBag<ValidationOccurrence>();
            compilationContext.RegisterSymbolAction(
                symbolContext => Collect(symbolContext.Symbol, validations),
                SymbolKind.NamedType,
                SymbolKind.Property);
            compilationContext.RegisterCompilationEndAction(endContext => {
                foreach(var group in validations
                            .GroupBy(static validation =>
                                $"{validation.ContainingType.ToDisplayString()}:{validation.Expression}:{validation.Message}")) {
                    var entries = group.ToArray();
                    if(entries.Length > 1) {
                        endContext.ReportDiagnostic(Diagnostic.Create(
                            CodeFirstDiagnosticDescriptors.DuplicateValidation,
                            entries[1].Location,
                            entries[0].Expression));
                    }
                }
            });
        });
    }

    private static void Collect(ISymbol symbol, ConcurrentBag<ValidationOccurrence> validations) {
        foreach(var attribute in symbol.GetAttributes()) {
            if(CodeFirstAttributeParser.TryGetValidation(attribute, symbol, out var validation)) {
                validations.Add(validation);
            }
        }
    }
}
