using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EditorAliasAnalyzer : DiagnosticAnalyzer {
    public override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [CodeFirstDiagnosticDescriptors.UnknownEditorAlias];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilationContext => {
            var catalog = EditorCatalog.Load(compilationContext.Options.AdditionalFiles);
            var dataTypes = new ConcurrentBag<DataTypeOccurrence>();
            compilationContext.RegisterSymbolAction(
                symbolContext => Collect(symbolContext.Symbol, dataTypes),
                SymbolKind.NamedType);
            compilationContext.RegisterCompilationEndAction(endContext => {
                if(catalog.IsEmpty) {
                    return;
                }

                foreach(var dataType in dataTypes
                            .GroupBy(static dataType => dataType.Alias, StringComparer.Ordinal)
                            .Select(static group => group.First())) {
                    if(!catalog.TryGet(dataType.EditorAlias, out _)) {
                        endContext.ReportDiagnostic(Diagnostic.Create(
                            CodeFirstDiagnosticDescriptors.UnknownEditorAlias,
                            dataType.Symbol.Locations.FirstOrDefault(),
                            dataType.Alias,
                            dataType.EditorAlias));
                    }
                }
            });
        });
    }

    private static void Collect(
        ISymbol symbol,
        ConcurrentBag<DataTypeOccurrence> dataTypes) {
        foreach(var attribute in symbol.GetAttributes()) {
            if(CodeFirstAttributeParser.TryGetDataType(attribute, symbol, out var dataType)) {
                dataTypes.Add(dataType);
            }
        }
    }
}
