using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PropertyOutsideDocumentTypeAnalyzer : DiagnosticAnalyzer {
    public override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [CodeFirstDiagnosticDescriptors.PropertyOutsideDocumentType];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context) {
        var property = (IPropertySymbol)context.Symbol;
        if(!property.GetAttributes().Any(static attribute =>
            attribute.AttributeClass?.Name == "UCFPropertyAttribute")
            || property.ContainingType is not null
                && CodeFirstAnalyzerHelpers.HasDocumentTypeAttribute(property.ContainingType)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            CodeFirstDiagnosticDescriptors.PropertyOutsideDocumentType,
            property.Locations.FirstOrDefault(),
            property.Name));
    }
}
