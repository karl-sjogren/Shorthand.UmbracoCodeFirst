using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IncompatibleEditorTypeAnalyzer : DiagnosticAnalyzer {
    public override System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [CodeFirstDiagnosticDescriptors.IncompatibleEditorType];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilationContext => {
            var dataTypes = new ConcurrentBag<DataTypeOccurrence>();
            var properties = new ConcurrentBag<EditorPropertyOccurrence>();
            var aliasTargets = new ConcurrentBag<(IPropertySymbol Property, ISymbol Target)>();
            var catalog = EditorCatalog.Load(compilationContext.Options.AdditionalFiles);
            compilationContext.RegisterSyntaxNodeAction(syntaxContext => {
                var syntax = (AttributeSyntax)syntaxContext.Node;
                if(syntax.ArgumentList?.Arguments.LastOrDefault(static argument => argument.NameEquals is null)?.Expression
                        is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Alias" } member
                    || syntax.FirstAncestorOrSelf<PropertyDeclarationSyntax>() is not { } declaration
                    || syntaxContext.SemanticModel.GetDeclaredSymbol(declaration) is not IPropertySymbol property
                    || syntaxContext.SemanticModel.GetSymbolInfo(member.Expression).Symbol is not { } target) {
                    return;
                }

                aliasTargets.Add((property, target));
            }, SyntaxKind.Attribute);
            compilationContext.RegisterSyntaxNodeAction(syntaxContext => {
                var declaration = (PropertyDeclarationSyntax)syntaxContext.Node;
                if(declaration.AttributeLists.SelectMany(static list => list.Attributes).Any(attribute =>
                        (syntaxContext.SemanticModel.GetSymbolInfo(attribute).Symbol as IMethodSymbol)?
                            .ContainingType.Name == "UCFPropertyAttribute")
                    && syntaxContext.SemanticModel.GetDeclaredSymbol(declaration) is IPropertySymbol property) {
                    Collect(property, dataTypes, properties);
                }
            }, SyntaxKind.PropertyDeclaration);
            compilationContext.RegisterSymbolAction(
                symbolContext => Collect(symbolContext.Symbol, dataTypes, properties),
                SymbolKind.NamedType);
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
                    editorByDataType.TryGetValue(property.DataTypeAlias, out var dataType);
                    if(dataType is null) {
                        var target = aliasTargets.FirstOrDefault(candidate =>
                            SymbolEqualityComparer.Default.Equals(candidate.Property, property.Property)).Target;
                        dataType = dataTypes.FirstOrDefault(candidate =>
                            SymbolEqualityComparer.Default.Equals(candidate.Symbol, target));
                    }

                    var editorAlias = dataType?.EditorAlias
                        ?? (MediaPickerCompatibility.IsBuiltIn(property.DataTypeKey) ? MediaPickerCompatibility.EditorAlias : null);
                    if(editorAlias is null || !catalog.TryGet(editorAlias, out var editor)) {
                        continue;
                    }

                    var compatible = editorAlias == MediaPickerCompatibility.EditorAlias
                        ? MediaPickerCompatibility.IsCompatible(
                            property.Property.Type,
                            dataType is null ? null : MediaPickerCompatibility.GetMultiplicity(dataType.Attribute))
                        : EditorCompatibilityHelpers.IsCompatible(property.Property.Type, editor.ReturnType);
                    if(compatible) {
                        continue;
                    }

                    endContext.ReportDiagnostic(Diagnostic.Create(
                        CodeFirstDiagnosticDescriptors.IncompatibleEditorType,
                        property.Property.Locations.FirstOrDefault(),
                        property.Property.Name,
                        property.Property.Type.ToDisplayString(),
                        editorAlias,
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
