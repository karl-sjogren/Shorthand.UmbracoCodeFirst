using Microsoft.CodeAnalysis;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

internal static class CodeFirstAttributeParser {
    public static bool TryGetAlias(
        AttributeData attribute,
        ISymbol symbol,
        out AliasOccurrence occurrence) {
        occurrence = null!;
        var attributeName = attribute.AttributeClass?.Name ?? string.Empty;
        if(attributeName is not (
            "UCFDocumentTypeAttribute"
            or "UCFElementTypeAttribute"
            or "UCFMediaTypeAttribute"
            or "UCFMemberTypeAttribute"
            or "UCFDataTypeAttribute"
            or "UCFPropertyAttribute")) {
            return false;
        }

        var hasExplicitAlias = attributeName switch {
            "UCFDocumentTypeAttribute"
                or "UCFElementTypeAttribute"
                or "UCFMediaTypeAttribute"
                or "UCFMemberTypeAttribute" => attribute.ConstructorArguments.Length >= 2,
            "UCFDataTypeAttribute" => attribute.ConstructorArguments.Length >= 3,
            "UCFPropertyAttribute" => attribute.ConstructorArguments.Length >= 3,
            _ => false,
        };
        var nameIndex = attributeName switch {
            "UCFDocumentTypeAttribute"
                or "UCFElementTypeAttribute"
                or "UCFMediaTypeAttribute"
                or "UCFMemberTypeAttribute"
                or "UCFPropertyAttribute" when hasExplicitAlias => 1,
            "UCFDataTypeAttribute" when hasExplicitAlias => 1,
            _ => 0,
        };
        if(attribute.ConstructorArguments.Length <= nameIndex
            || attribute.ConstructorArguments[nameIndex].Value is not string name) {
            return false;
        }

        var alias = hasExplicitAlias
            ? attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty
            : CodeFirstAnalyzerHelpers.ToAlias(name);
        if(alias.Length == 0) {
            return false;
        }

        occurrence = new AliasOccurrence(
            alias,
            symbol,
            attributeName == "UCFPropertyAttribute"
                ? AliasScope.ContainingType
                : AliasScope.Global,
            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                ?? symbol.Locations.FirstOrDefault(),
            CodeFirstAnalyzerHelpers.IsValidAlias(alias));
        return true;
    }

    public static bool TryGetLayout(
        AttributeData attribute,
        ISymbol symbol,
        out AliasOccurrence occurrence) {
        occurrence = null!;
        var attributeName = attribute.AttributeClass?.Name ?? string.Empty;
        if(attributeName is not ("UCFGroupAttribute" or "UCFTabAttribute")) {
            return false;
        }

        var arguments = attribute.ConstructorArguments;
        var nameArgument = arguments.LastOrDefault(static argument => argument.Value is string);
        if(nameArgument.Value is not string name) {
            return false;
        }

        var hasExplicitAlias = arguments.Length > 1
            && arguments[0].Value is string
            && arguments[1].Value is string;
        var alias = !hasExplicitAlias
            ? CodeFirstAnalyzerHelpers.ToAlias(name)
            : arguments[0].Value?.ToString() ?? string.Empty;
        var sortOrder = arguments.LastOrDefault(static argument => argument.Value is int).Value is int value
            ? value
            : 0;

        occurrence = new AliasOccurrence(
            alias,
            symbol,
            AliasScope.ContainingType,
            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                ?? symbol.Locations.FirstOrDefault(),
            CodeFirstAnalyzerHelpers.IsValidAlias(alias),
            attributeName == "UCFGroupAttribute" ? "group" : "tab",
            name,
            sortOrder);
        return true;
    }

    public static bool TryGetValidation(
        AttributeData attribute,
        ISymbol symbol,
        out ValidationOccurrence occurrence) {
        occurrence = null!;
        var attributeClass = attribute.AttributeClass;
        if(attributeClass?.Name != "UCFValidationAttribute"
            && attributeClass?.BaseType?.Name != "UCFValidationAttribute") {
            return false;
        }

        if(attribute.ConstructorArguments.Length == 0
            || attribute.ConstructorArguments[0].Value is not string expression
            || symbol.ContainingType is not INamedTypeSymbol containingType) {
            return false;
        }

        var message = attribute.ConstructorArguments.Length > 1
            ? attribute.ConstructorArguments[1].Value?.ToString() ?? string.Empty
            : string.Empty;
        occurrence = new ValidationOccurrence(
            expression,
            message,
            containingType,
            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation());
        return true;
    }

    public static bool TryGetComposition(
        AttributeData attribute,
        INamedTypeSymbol source,
        out CompositionOccurrence occurrence) {
        occurrence = null!;
        if(attribute.AttributeClass?.Name != "UCFCompositeAttribute"
            || attribute.ConstructorArguments.Length == 0
            || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol target) {
            return false;
        }

        occurrence = new CompositionOccurrence(
            source,
            target,
            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
            target.GetAttributes().Any(CodeFirstAnalyzerHelpers.HasDocumentTypeAttribute),
            source.GetAttributes().Any(static item => item.AttributeClass?.Name == "UCFElementTypeAttribute"),
            target.GetAttributes().Any(static item => item.AttributeClass?.Name == "UCFElementTypeAttribute"));
        return true;
    }

    public static bool TryGetDataType(
        AttributeData attribute,
        ISymbol symbol,
        out DataTypeOccurrence occurrence) {
        occurrence = null!;
        if(attribute.AttributeClass?.Name != "UCFDataTypeAttribute") {
            return false;
        }

        var editorIndex = attribute.ConstructorArguments.Length == 2 ? 1 : 2;
        if(attribute.ConstructorArguments.Length <= editorIndex
            || attribute.ConstructorArguments[editorIndex].Value is not string editorAlias) {
            return false;
        }

        var alias = GetDefinitionAlias(attribute);
        if(alias.Length == 0) {
            return false;
        }

        occurrence = new DataTypeOccurrence(alias, editorAlias, symbol);
        return true;
    }

    public static bool TryGetEditorProperty(
        AttributeData attribute,
        IPropertySymbol property,
        out EditorPropertyOccurrence occurrence) {
        occurrence = null!;
        if(attribute.AttributeClass?.Name != "UCFPropertyAttribute") {
            return false;
        }

        var dataTypeIndex = attribute.ConstructorArguments.Length == 2 ? 1 : 2;
        if(attribute.ConstructorArguments.Length <= dataTypeIndex
            || attribute.ConstructorArguments[dataTypeIndex].Value is not string dataTypeAlias) {
            return false;
        }

        occurrence = new EditorPropertyOccurrence(property, dataTypeAlias);
        return true;
    }

    public static string GetDefinitionAlias(AttributeData attribute) {
        var hasExplicitAlias = attribute.ConstructorArguments.Length >= 3;
        var nameIndex = hasExplicitAlias ? 1 : 0;
        if(attribute.ConstructorArguments.Length <= nameIndex
            || attribute.ConstructorArguments[nameIndex].Value is not string name) {
            return string.Empty;
        }

        return hasExplicitAlias
            ? attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty
            : CodeFirstAnalyzerHelpers.ToAlias(name);
    }
}
