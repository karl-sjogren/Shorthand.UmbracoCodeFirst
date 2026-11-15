using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

internal static class MediaPickerCompatibility {
    public const string EditorAlias = "Umbraco.MediaPicker3";

    public static bool IsBuiltIn(string? key) =>
        Guid.TryParse(key, out var guid)
        && (guid == new Guid("4309A3EA-0D78-4329-A06C-C80B036AF19A")
            || guid == new Guid("1B661F40-2242-4B44-B9CB-3990EE2B13C0")
            || guid == new Guid("AD9F0CF2-BDA2-45D5-9EA1-A63CFC873FD3")
            || guid == new Guid("0E63D883-B62B-4799-88C3-157F82E83ECC"));

    public static bool IsCompatible(ITypeSymbol type, bool? multiple) =>
        (multiple != true && IsMedia(type))
        || (multiple != false && IsMediaEnumerable(type));

    private static bool IsMedia(ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).TrimEnd('?') is
            "global::Umbraco.Cms.Core.Models.MediaWithCrops"
            or "global::Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent";

    private static bool IsMediaEnumerable(ITypeSymbol type) {
        if(type is IArrayTypeSymbol array) {
            return IsMedia(array.ElementType);
        }

        if(type is not INamedTypeSymbol named) {
            return false;
        }

        return IsEnumerable(named) || named.AllInterfaces.Any(IsEnumerable);
    }

    private static bool IsEnumerable(INamedTypeSymbol type) =>
        type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
        && IsMedia(type.TypeArguments[0]);

    public static bool? GetMultiplicity(AttributeData attribute) {
        var configuration = attribute.NamedArguments
            .FirstOrDefault(static argument => argument.Key == "ConfigurationType").Value.Value as INamedTypeSymbol
            ?? (attribute.ConstructorArguments.Length >= 3
                ? attribute.ConstructorArguments[2].Value as INamedTypeSymbol
                : null);
        var json = attribute.NamedArguments.FirstOrDefault(static argument => argument.Key == "ConfigurationJson").Value.Value as string;
        if(configuration is null && json is not null) {
            try {
                using var document = JsonDocument.Parse(json);
                if(document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("multiple", out var value)) {
                    return value.ValueKind switch {
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => null,
                    };
                }
            } catch(JsonException) {
                return null;
            }
        }

        if(configuration?.BaseType?.ToDisplayString()
                != "Shorthand.UmbracoCodeFirst.PropertyEditors.UmbracoEditors.MediaPicker.Configuration"
            || configuration.GetMembers("Multiple").Length != 0) {
            return null;
        }

        bool? result = null;
        foreach(var reference in configuration.DeclaringSyntaxReferences) {
            if(reference.GetSyntax() is not TypeDeclarationSyntax declaration) {
                continue;
            }

            foreach(var constructor in declaration.Members.OfType<ConstructorDeclarationSyntax>()) {
                if(constructor.ParameterList.Parameters.Count != 0
                    || constructor.Initializer is not null
                    || constructor.ExpressionBody is not null
                    || constructor.Body is null) {
                    return null;
                }

                foreach(var statement in constructor.Body.Statements) {
                    if(statement is not ExpressionStatementSyntax {
                        Expression: AssignmentExpressionSyntax assignment
                    } || !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                        || assignment.Left is not IdentifierNameSyntax name) {
                        return null;
                    }

                    if(name.Identifier.ValueText == "Multiple") {
                        if(result is not null || assignment.Right is not LiteralExpressionSyntax literal
                            || literal.Token.Value is not bool value) {
                            return null;
                        }

                        result = value;
                    } else {
                        return null;
                    }
                }
            }
        }

        return result;
    }
}
