using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnsupportedPropertyTypeAnalyzer : DiagnosticAnalyzer {
    public override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [CodeFirstDiagnosticDescriptors.UnsupportedPropertyType];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context) {
        var property = (IPropertySymbol)context.Symbol;
        if(!property.GetAttributes().Any(static attribute =>
            attribute.AttributeClass?.Name == "UCFPropertyAttribute")
            || property.Type.TypeKind is not (TypeKind.Pointer or TypeKind.FunctionPointer)
                && property.Type.SpecialType != SpecialType.System_Void) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            CodeFirstDiagnosticDescriptors.UnsupportedPropertyType,
            property.Locations.FirstOrDefault(),
            property.Name,
            property.Type.ToDisplayString()));
    }
}
