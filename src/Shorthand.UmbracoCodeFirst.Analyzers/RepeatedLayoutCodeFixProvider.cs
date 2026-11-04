using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RepeatedLayoutCodeFixProvider)), Shared]
public sealed class RepeatedLayoutCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [CodeFirstDiagnosticIds.DuplicateLayout];

    public override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        var diagnostic = context.Diagnostics.FirstOrDefault();
        if(diagnostic is null) {
            return;
        }

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);
        var attribute = root?.FindNode(diagnostic.Location.SourceSpan)
            .FirstAncestorOrSelf<AttributeSyntax>();
        if(attribute is null) {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
            .ConfigureAwait(false);
        if(semanticModel is null || !TryGetLayoutInfo(semanticModel, attribute, out var layout)) {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                $"Create shared {layout.Kind} attribute",
                cancellationToken => ApplyFixAsync(context.Document, layout, cancellationToken),
                nameof(RepeatedLayoutCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> ApplyFixAsync(
        Document document,
        LayoutInfo layout,
        CancellationToken cancellationToken) {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if(root is null) {
            return document;
        }

        var namespaceDeclaration = layout.ContainingType
            .Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault();
        var classDeclaration = SyntaxFactory.ParseMemberDeclaration(
            layout.GeneratedSource.Replace("\r\n", "\n"));
        if(classDeclaration is null) {
            return document;
        }

        var namespaceAnnotation = new SyntaxAnnotation();
        var attributeAnnotation = new SyntaxAnnotation();
        var annotatedRoot = root.ReplaceNodes(
            layout.MatchingAttributes,
            (node, _) => node.WithAdditionalAnnotations(attributeAnnotation));
        if(namespaceDeclaration is null) {
            annotatedRoot = annotatedRoot.WithAdditionalAnnotations(namespaceAnnotation);
        } else {
            var currentNamespace = annotatedRoot.FindNode(namespaceDeclaration.Span)
                .FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();
            if(currentNamespace is null) {
                return document;
            }

            annotatedRoot = annotatedRoot.ReplaceNode(
                currentNamespace,
                currentNamespace.WithAdditionalAnnotations(namespaceAnnotation));
        }

        var annotatedNamespace = annotatedRoot.GetAnnotatedNodes(namespaceAnnotation).FirstOrDefault();
        if(annotatedNamespace is BaseNamespaceDeclarationSyntax namespaceNode) {
            annotatedRoot = annotatedRoot.ReplaceNode(
                namespaceNode,
                namespaceNode.AddMembers(classDeclaration));
        } else if(annotatedNamespace is CompilationUnitSyntax compilationUnit) {
            annotatedRoot = compilationUnit.AddMembers(classDeclaration);
        } else {
            return document;
        }

        if(annotatedRoot is CompilationUnitSyntax compilationUnitWithUsings
            && !compilationUnitWithUsings.Usings.Any(usingDirective =>
                usingDirective.Name?.ToString() == "Shorthand.UmbracoCodeFirst.Attributes")) {
            annotatedRoot = compilationUnitWithUsings.AddUsings(
                SyntaxFactory.UsingDirective(
                    SyntaxFactory.ParseName("Shorthand.UmbracoCodeFirst.Attributes")));
        }

        var attributesToReplace = annotatedRoot
            .GetAnnotatedNodes(attributeAnnotation)
            .OfType<AttributeSyntax>()
            .ToArray();
        annotatedRoot = annotatedRoot.ReplaceNodes(
            attributesToReplace,
            (attribute, _) => SyntaxFactory.Attribute(SyntaxFactory.ParseName(layout.GeneratedName))
                .WithTriviaFrom(attribute));

        return document.WithSyntaxRoot(annotatedRoot);
    }

    private static bool TryGetLayoutInfo(
        SemanticModel semanticModel,
        AttributeSyntax attribute,
        out LayoutInfo layout) {
        layout = null!;
        var attributeType = semanticModel.GetTypeInfo(attribute).Type as INamedTypeSymbol;
        if(attributeType is null) {
            return false;
        }

        var attributeName = attributeType.Name;
        if(attributeName is not ("UCFGroupAttribute" or "UCFTabAttribute")
            || attribute.ArgumentList is null
            || attribute.ArgumentList.Arguments.Any(argument =>
                argument.NameEquals is not null || argument.NameColon is not null)) {
            return false;
        }

        var arguments = attribute.ArgumentList.Arguments;
        var stringArguments = arguments
            .Where(argument => semanticModel.GetConstantValue(argument.Expression).Value is string)
            .ToArray();
        if(stringArguments.Length == 0) {
            return false;
        }

        var nameArgument = stringArguments[stringArguments.Length - 1];
        var name = (string)semanticModel.GetConstantValue(nameArgument.Expression).Value!;
        var kind = attributeName == "UCFGroupAttribute" ? "group" : "tab";
        var generatedName = ToPascalIdentifier(name)
            + (kind == "group" ? "Group" : "Tab")
            + "Attribute";
        if(generatedName == "Attribute") {
            return false;
        }

        var containingType = attribute.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if(containingType is null) {
            return false;
        }

        var matchingAttributes = containingType
            .DescendantNodes()
            .OfType<AttributeSyntax>()
            .Where(candidate => IsMatchingLayout(semanticModel, candidate, attributeType, arguments))
            .ToImmutableArray();
        if(matchingAttributes.Length < 2) {
            return false;
        }

        var baseName = kind == "group" ? "UCFGroupAttribute" : "UCFTabAttribute";
        var constructorArguments = string.Join(
            ", ",
            arguments.Select(argument => argument.Expression.ToFullString().Trim()));
        var source = $$"""
            [global::System.AttributeUsage(global::System.AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
            public sealed class {{generatedName}} : {{baseName}} {
                public {{generatedName}}() : base({{constructorArguments}}) {
                }
            }
            """;

        layout = new LayoutInfo(
            kind,
            generatedName,
            source,
            containingType,
            matchingAttributes);
        return true;
    }

    private static bool IsMatchingLayout(
        SemanticModel semanticModel,
        AttributeSyntax candidate,
        INamedTypeSymbol expectedType,
        SeparatedSyntaxList<AttributeArgumentSyntax> expectedArguments) {
        if(semanticModel.GetTypeInfo(candidate).Type is not INamedTypeSymbol candidateType
            || !SymbolEqualityComparer.Default.Equals(candidateType, expectedType)
            || candidate.ArgumentList is null
            || candidate.ArgumentList.Arguments.Count != expectedArguments.Count) {
            return false;
        }

        for(var index = 0; index < expectedArguments.Count; index++) {
            var expected = semanticModel.GetConstantValue(expectedArguments[index].Expression);
            var actual = semanticModel.GetConstantValue(candidate.ArgumentList.Arguments[index].Expression);
            if(expected.HasValue && actual.HasValue) {
                if(!Equals(expected.Value, actual.Value)) {
                    return false;
                }
            } else if(!string.Equals(
                expectedArguments[index].Expression.ToString(),
                candidate.ArgumentList.Arguments[index].Expression.ToString(),
                StringComparison.Ordinal)) {
                return false;
            }
        }

        return true;
    }

    private static string ToPascalIdentifier(string value) {
        var builder = new System.Text.StringBuilder();
        var capitalize = true;
        foreach(var character in value) {
            if(!char.IsLetterOrDigit(character)) {
                capitalize = true;
                continue;
            }

            if(capitalize) {
                builder.Append(char.ToUpperInvariant(character));
                capitalize = false;
            } else {
                builder.Append(character);
            }
        }

        return builder.Length == 0 ? "Layout" : builder.ToString();
    }

    private sealed class LayoutInfo(
        string kind,
        string generatedName,
        string generatedSource,
        TypeDeclarationSyntax containingType,
        ImmutableArray<AttributeSyntax> matchingAttributes) {
        public string Kind { get; } = kind;
        public string GeneratedName { get; } = generatedName;
        public string GeneratedSource { get; } = generatedSource;
        public TypeDeclarationSyntax ContainingType { get; } = containingType;
        public ImmutableArray<AttributeSyntax> MatchingAttributes { get; } = matchingAttributes;
    }
}
