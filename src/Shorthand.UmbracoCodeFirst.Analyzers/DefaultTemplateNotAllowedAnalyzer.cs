using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DefaultTemplateNotAllowedAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [CodeFirstDiagnosticDescriptors.DefaultTemplateNotAllowed];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilationContext => {
            var attributeType = compilationContext.Compilation.GetTypeByMetadataName(
                "Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentAttribute");
            if(attributeType is null) {
                return;
            }

            compilationContext.RegisterSymbolAction(symbolContext => {
                foreach(var attribute in symbolContext.Symbol.GetAttributes()) {
                    if(!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType)) {
                        continue;
                    }

                    var defaultArgument = attribute.NamedArguments
                        .FirstOrDefault(argument => argument.Key == "DefaultTemplateAlias").Value;
                    if(defaultArgument.Kind == TypedConstantKind.Error
                        || defaultArgument.Value is not string defaultAlias) {
                        continue;
                    }

                    var hasAllowedArgument = attribute.NamedArguments.Any(argument => argument.Key == "AllowedTemplateAliases");
                    var allowedArgument = attribute.NamedArguments
                        .FirstOrDefault(argument => argument.Key == "AllowedTemplateAliases").Value;
                    if(hasAllowedArgument && (allowedArgument.Kind == TypedConstantKind.Error
                        || (allowedArgument.Kind == TypedConstantKind.Array
                            && !allowedArgument.IsNull
                            && allowedArgument.Values.Any(value => value.Kind == TypedConstantKind.Error)))) {
                        continue;
                    }

                    if(allowedArgument.Kind == TypedConstantKind.Array
                        && !allowedArgument.IsNull
                        && allowedArgument.Values.Any(value =>
                            string.Equals(value.Value as string, defaultAlias, StringComparison.OrdinalIgnoreCase))) {
                        continue;
                    }

                    var syntax = attribute.ApplicationSyntaxReference?.GetSyntax(symbolContext.CancellationToken) as AttributeSyntax;
                    var location = syntax?.ArgumentList?.Arguments
                        .FirstOrDefault(argument => argument.NameEquals?.Name.Identifier.ValueText == "DefaultTemplateAlias")
                        ?.Expression.GetLocation()
                        ?? syntax?.GetLocation()
                        ?? symbolContext.Symbol.Locations.FirstOrDefault();
                    symbolContext.ReportDiagnostic(Diagnostic.Create(
                        CodeFirstDiagnosticDescriptors.DefaultTemplateNotAllowed,
                        location,
                        defaultAlias));
                }
            }, SymbolKind.NamedType);
        });
    }
}
