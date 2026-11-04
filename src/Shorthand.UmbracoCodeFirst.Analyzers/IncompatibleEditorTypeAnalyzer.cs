using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IncompatibleEditorTypeAnalyzer : DiagnosticAnalyzer {
    public override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [CodeFirstDiagnosticDescriptors.IncompatibleEditorType];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilationContext => {
            var dataTypes = new ConcurrentBag<DataTypeOccurrence>();
            var properties = new ConcurrentBag<EditorPropertyOccurrence>();
            var catalog = EditorCatalog.Load(compilationContext.Options.AdditionalFiles);
            compilationContext.RegisterSymbolAction(
                symbolContext => Collect(symbolContext.Symbol, dataTypes, properties),
                SymbolKind.NamedType,
                SymbolKind.Property);
            compilationContext.RegisterCompilationEndAction(endContext => {
                if(catalog.IsEmpty) {
                    return;
                }

                var editorByDataType = dataTypes
                    .GroupBy(static dataType => dataType.Alias, StringComparer.Ordinal)
                    .ToDictionary(
                        static group => group.Key,
                        static group => group.First(),
                        StringComparer.Ordinal);
                foreach(var property in properties) {
                    if(!editorByDataType.TryGetValue(property.DataTypeAlias, out var dataType)
                        || !catalog.TryGet(dataType.EditorAlias, out var editor)
                        || EditorCompatibilityHelpers.IsCompatible(property.Property.Type, editor.ReturnType)) {
                        continue;
                    }

                    endContext.ReportDiagnostic(Diagnostic.Create(
                        CodeFirstDiagnosticDescriptors.IncompatibleEditorType,
                        property.Property.Locations.FirstOrDefault(),
                        property.Property.Name,
                        property.Property.Type.ToDisplayString(),
                        dataType.EditorAlias,
                        editor.ReturnType));
                }
            });
        });
    }

    private static void Collect(
        ISymbol symbol,
        ConcurrentBag<DataTypeOccurrence> dataTypes,
        ConcurrentBag<EditorPropertyOccurrence> properties) {
        foreach(var attribute in symbol.GetAttributes()) {
            if(CodeFirstAttributeParser.TryGetDataType(attribute, symbol, out var dataType)) {
                dataTypes.Add(dataType);
            }

            if(symbol is IPropertySymbol property
                && CodeFirstAttributeParser.TryGetEditorProperty(attribute, property, out var editorProperty)) {
                properties.Add(editorProperty);
            }
        }
    }
}
