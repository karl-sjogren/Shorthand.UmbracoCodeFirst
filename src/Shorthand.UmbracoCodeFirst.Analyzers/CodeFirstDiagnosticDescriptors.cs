using Microsoft.CodeAnalysis;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

internal static class CodeFirstDiagnosticDescriptors {
    private const string _documentationBaseUrl =
        "https://github.com/karl-sjogren/Shorthand.UmbracoCodeFirst/blob/feature/initial-implementation/docs/analyzers";

#pragma warning disable RS2008
    public static readonly DiagnosticDescriptor InvalidAlias = new(
        CodeFirstDiagnosticIds.InvalidAlias,
        "Invalid code-first alias",
        "Alias '{0}' contains characters that are not valid for a code-first alias",
        "CodeFirst",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF100.md",
        customTags: ["CompilationEnd"]);

    public static readonly DiagnosticDescriptor DuplicateAlias = new(
        CodeFirstDiagnosticIds.DuplicateAlias,
        "Duplicate code-first alias",
        "Alias '{0}' is declared more than once in the same code-first scope",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF101.md",
        customTags: ["CompilationEnd"]);

    public static readonly DiagnosticDescriptor DuplicateLayout = new(
        CodeFirstDiagnosticIds.DuplicateLayout,
        "Repeated layout configuration",
        "The {0} alias '{1}' is repeated; use one shared derived attribute",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF102.md",
        customTags: ["CompilationEnd"]);

    public static readonly DiagnosticDescriptor DuplicateValidation = new(
        CodeFirstDiagnosticIds.DuplicateValidation,
        "Repeated validation configuration",
        "The validation '{0}' is repeated; use one shared derived UCFValidation attribute",
        "CodeFirst",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF103.md",
        customTags: ["CompilationEnd"]);

    public static readonly DiagnosticDescriptor UnsupportedPropertyType = new(
        CodeFirstDiagnosticIds.UnsupportedPropertyType,
        "Unsupported code-first property type",
        "Property '{0}' has type '{1}', which is not supported by the code-first contract",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF104.md");

    public static readonly DiagnosticDescriptor PropertyOutsideDocumentType = new(
        CodeFirstDiagnosticIds.PropertyOutsideDocumentType,
        "Code-first property requires a content type",
        "Property '{0}' uses UCFProperty but its containing class is not a UCFDocumentType",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF105.md");

    public static readonly DiagnosticDescriptor InvalidCompositionTarget = new(
        CodeFirstDiagnosticIds.InvalidCompositionTarget,
        "Invalid content composition target",
        "Composition target '{0}' must have UCFDocumentTypeAttribute",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF106.md",
        customTags: ["CompilationEnd"]);

    public static readonly DiagnosticDescriptor InvalidCompositionGraph = new(
        CodeFirstDiagnosticIds.InvalidCompositionGraph,
        "Invalid content composition graph",
        "Composition from '{0}' to '{1}' is duplicated or creates a composition cycle",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF107.md",
        customTags: ["CompilationEnd"]);

    public static readonly DiagnosticDescriptor UnknownEditorAlias = new(
        CodeFirstDiagnosticIds.UnknownEditorAlias,
        "Unknown Umbraco property editor",
        "Data Type '{0}' references editor alias '{1}', which is not in the built-in editor catalog",
        "CodeFirst",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF108.md",
        customTags: ["CompilationEnd"]);

    public static readonly DiagnosticDescriptor IncompatibleEditorType = new(
        CodeFirstDiagnosticIds.IncompatibleEditorType,
        "Incompatible Umbraco property editor type",
        "Property '{0}' has CLR type '{1}', but editor '{2}' documents return type '{3}'",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF109.md",
        customTags: ["CompilationEnd"]);

    public static readonly DiagnosticDescriptor IncompatibleCompositionType = new(
        CodeFirstDiagnosticIds.IncompatibleCompositionType,
        "Incompatible content composition type",
        "Composition from '{0}' to '{1}' must use the same DocumentType or ElementType kind",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF110.md",
        customTags: ["CompilationEnd"]);
#pragma warning restore RS2008
}
