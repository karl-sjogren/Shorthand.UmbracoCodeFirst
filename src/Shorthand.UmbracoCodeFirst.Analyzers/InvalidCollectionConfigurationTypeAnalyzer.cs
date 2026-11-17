using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InvalidCollectionConfigurationTypeAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [CodeFirstDiagnosticDescriptors.InvalidCollectionConfigurationType];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilationContext => {
            var attributeType = compilationContext.Compilation.GetTypeByMetadataName(
                "Shorthand.UmbracoCodeFirst.Attributes.UCFCollectionAttribute");
            var configurationBase = compilationContext.Compilation.GetTypeByMetadataName(
                "Shorthand.UmbracoCodeFirst.Metadata.CodeFirstCollectionConfiguration");
            if(attributeType is null || configurationBase is null) {
                return;
            }

            compilationContext.RegisterSymbolAction(symbolContext => {
                foreach(var attribute in symbolContext.Symbol.GetAttributes()) {
                    if(!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType)
                        || attribute.ConstructorArguments.Length == 0
                        || attribute.ConstructorArguments[0].Kind != TypedConstantKind.Type
                        || attribute.ConstructorArguments[0].Value is not ITypeSymbol type
                        || type.TypeKind == TypeKind.Error) {
                        continue;
                    }

                    var current = type.BaseType;
                    while(current is not null && !SymbolEqualityComparer.Default.Equals(current, configurationBase)) {
                        current = current.BaseType;
                    }

                    if(current is null) {
                        var syntax = attribute.ApplicationSyntaxReference?.GetSyntax(symbolContext.CancellationToken) as AttributeSyntax;
                        var location = syntax?.ArgumentList?.Arguments.FirstOrDefault()?.Expression.GetLocation()
                            ?? syntax?.GetLocation()
                            ?? symbolContext.Symbol.Locations.FirstOrDefault();
                        symbolContext.ReportDiagnostic(Diagnostic.Create(
                            CodeFirstDiagnosticDescriptors.InvalidCollectionConfigurationType,
                            location,
                            type.ToDisplayString()));
                    }
                }
            }, SymbolKind.NamedType);
        });
    }
}
