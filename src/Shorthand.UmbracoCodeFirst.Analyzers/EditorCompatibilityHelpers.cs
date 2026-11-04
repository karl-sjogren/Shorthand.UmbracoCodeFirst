using Microsoft.CodeAnalysis;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

internal static class EditorCompatibilityHelpers {
    public static bool IsCompatible(ITypeSymbol type, string returnType) {
        var actual = type is INamedTypeSymbol { IsGenericType: true } named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                ? named.TypeArguments[0]
                : type;

        return returnType switch {
            "object" => true,
            "string" => actual.SpecialType == SpecialType.System_String,
            "bool" => actual.SpecialType == SpecialType.System_Boolean,
            "int" => actual.SpecialType == SpecialType.System_Int32,
            "decimal" => actual.SpecialType == SpecialType.System_Decimal,
            "System.DateTime" => actual.SpecialType == SpecialType.System_DateTime,
            "System.DateTimeOffset" => actual.ToDisplayString() == "System.DateTimeOffset",
            "System.DateOnly" => actual.ToDisplayString() == "System.DateOnly",
            "System.TimeOnly" => actual.ToDisplayString() == "System.TimeOnly",
            _ when returnType.StartsWith(
                "System.Collections.Generic.IEnumerable<string>",
                StringComparison.Ordinal)
                => IsEnumerableOf(actual, SpecialType.System_String),
            _ => returnType.StartsWith("Umbraco.", StringComparison.Ordinal),
        };
    }

    private static bool IsEnumerableOf(ITypeSymbol type, SpecialType elementType) =>
        type is IArrayTypeSymbol array && array.ElementType.SpecialType == elementType
        || type is INamedTypeSymbol namedType && namedType.AllInterfaces.Any(interfaceType =>
            interfaceType is INamedTypeSymbol named
            && named.Name == "IEnumerable"
            && named.TypeArguments.Length == 1
            && named.TypeArguments[0].SpecialType == elementType);
}
