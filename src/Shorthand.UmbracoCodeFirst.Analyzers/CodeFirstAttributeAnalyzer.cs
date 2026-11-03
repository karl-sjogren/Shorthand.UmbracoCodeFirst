using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CodeFirstAttributeAnalyzer : DiagnosticAnalyzer {
    public const string InvalidAliasId = "UCF100";
    public const string DuplicateAliasId = "UCF101";
    public const string DuplicateLayoutId = "UCF102";
    public const string DuplicateValidationId = "UCF103";
    public const string UnsupportedPropertyTypeId = "UCF104";
    public const string PropertyOutsideContentTypeId = "UCF105";

#pragma warning disable RS2008
    private static readonly DiagnosticDescriptor _invalidAlias = new(
        InvalidAliasId,
        "Invalid code-first alias",
        "Alias '{0}' contains characters that are not valid for a code-first alias",
        "CodeFirst",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: new[] { "CompilationEnd" });

    private static readonly DiagnosticDescriptor _duplicateAlias = new(
        DuplicateAliasId,
        "Duplicate code-first alias",
        "Alias '{0}' is declared more than once in the same code-first scope",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: new[] { "CompilationEnd" });

    private static readonly DiagnosticDescriptor _duplicateLayout = new(
        DuplicateLayoutId,
        "Repeated layout configuration",
        "The {0} alias '{1}' is repeated; use one shared derived attribute",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: new[] { "CompilationEnd" });

    private static readonly DiagnosticDescriptor _duplicateValidation = new(
        DuplicateValidationId,
        "Repeated validation configuration",
        "The validation '{0}' is repeated; use one shared derived UCFValidation attribute",
        "CodeFirst",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        customTags: new[] { "CompilationEnd" });

    private static readonly DiagnosticDescriptor _unsupportedPropertyType = new(
        UnsupportedPropertyTypeId,
        "Unsupported code-first property type",
        "Property '{0}' has type '{1}', which is not supported by the code-first contract",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor _propertyOutsideContentType = new(
        PropertyOutsideContentTypeId,
        "Code-first property requires a content type",
        "Property '{0}' uses UCFProperty but its containing class is not a UCFContentType",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
#pragma warning restore RS2008

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [
            _invalidAlias,
            _duplicateAlias,
            _duplicateLayout,
            _duplicateValidation,
            _unsupportedPropertyType,
            _propertyOutsideContentType,
        ];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilationContext => {
            var occurrences = new ConcurrentBag<AliasOccurrence>();
            var validations = new ConcurrentBag<ValidationOccurrence>();
            var propertyDiagnostics = new ConcurrentBag<PropertyDiagnosticOccurrence>();

            compilationContext.RegisterSymbolAction(
                symbolContext => Collect(symbolContext.Symbol, occurrences, validations, propertyDiagnostics),
                SymbolKind.NamedType,
                SymbolKind.Property);
            compilationContext.RegisterCompilationEndAction(
                endContext => Report(endContext, occurrences, validations, propertyDiagnostics));
        });
    }

    private static void Collect(
        ISymbol symbol,
        ConcurrentBag<AliasOccurrence> occurrences,
        ConcurrentBag<ValidationOccurrence> validations,
        ConcurrentBag<PropertyDiagnosticOccurrence> propertyDiagnostics) {
        foreach(AttributeData attribute in symbol.GetAttributes()) {
            string attributeName = attribute.AttributeClass?.Name ?? string.Empty;
            if(attributeName is "UCFContentTypeAttribute" or "UCFDataTypeAttribute") {
                AddAlias(attribute, symbol, occurrences, AliasScope.Global);
            } else if(attributeName is "UCFPropertyAttribute") {
                AddAlias(attribute, symbol, occurrences, AliasScope.ContainingType);
                if(symbol is IPropertySymbol property) {
                    CollectPropertyContractDiagnostics(property, propertyDiagnostics);
                }
            } else if(attributeName is "UCFGroupAttribute" or "UCFTabAttribute") {
                AddLayout(attribute, symbol, occurrences, attributeName == "UCFGroupAttribute");
            } else if(attributeName == "UCFValidationAttribute"
                      || attribute.AttributeClass?.BaseType?.Name == "UCFValidationAttribute") {
                AddValidation(attribute, symbol, validations);
            }
        }
    }

    private static void CollectPropertyContractDiagnostics(
        IPropertySymbol property,
        ConcurrentBag<PropertyDiagnosticOccurrence> diagnostics) {
        if(property.ContainingType is null
            || !property.ContainingType.GetAttributes().Any(HasContentTypeAttribute)) {
            diagnostics.Add(new PropertyDiagnosticOccurrence(
                _propertyOutsideContentType,
                property,
                [property.Name]));
        }

        if(property.Type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer
            || property.Type.SpecialType == SpecialType.System_Void) {
            diagnostics.Add(new PropertyDiagnosticOccurrence(
                _unsupportedPropertyType,
                property,
                [property.Name, property.Type.ToDisplayString()]));
        }
    }

    private static void AddAlias(
        AttributeData attribute,
        ISymbol symbol,
        ConcurrentBag<AliasOccurrence> occurrences,
        AliasScope scope) {
        int aliasIndex = attribute.AttributeClass?.Name == "UCFPropertyAttribute"
            ? 2
            : 0;
        if(attribute.AttributeConstructor?.Parameters.Length <= aliasIndex
            || attribute.ConstructorArguments.Length <= aliasIndex
            || attribute.ConstructorArguments[aliasIndex].Value is not string alias
            || attribute.ConstructorArguments.Length == 1
            || (attribute.AttributeClass?.Name is "UCFContentTypeAttribute" or "UCFDataTypeAttribute"
                && attribute.ConstructorArguments.Length < 2)) {
            return;
        }

        occurrences.Add(new AliasOccurrence(
            alias,
            symbol,
            scope,
            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
            IsValidAlias(alias)));
    }

    private static void AddLayout(
        AttributeData attribute,
        ISymbol symbol,
        ConcurrentBag<AliasOccurrence> occurrences,
        bool isGroup) {
        if(attribute.ConstructorArguments.Length < 2
            || attribute.ConstructorArguments[0].Value is not string alias
            || attribute.ConstructorArguments[1].Value is not string name) {
            return;
        }

        int sortOrder = attribute.ConstructorArguments.Length > 2
            && attribute.ConstructorArguments[2].Value is int value
            ? value
            : 0;
        occurrences.Add(new AliasOccurrence(
            alias,
            symbol,
            AliasScope.ContainingType,
            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
            IsValidAlias(alias),
            isGroup ? "group" : "tab",
            name,
            sortOrder));
    }

    private static void AddValidation(
        AttributeData attribute,
        ISymbol symbol,
        ConcurrentBag<ValidationOccurrence> validations) {
        if(attribute.ConstructorArguments.Length == 0
            || attribute.ConstructorArguments[0].Value is not string expression) {
            return;
        }

        string message = attribute.ConstructorArguments.Length > 1
            ? attribute.ConstructorArguments[1].Value?.ToString() ?? string.Empty
            : string.Empty;
        validations.Add(new ValidationOccurrence(
            expression,
            message,
            symbol.ContainingType,
            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()));
    }

    private static void Report(
        CompilationAnalysisContext context,
        ConcurrentBag<AliasOccurrence> occurrences,
        ConcurrentBag<ValidationOccurrence> validations,
        ConcurrentBag<PropertyDiagnosticOccurrence> propertyDiagnostics) {
        foreach(PropertyDiagnosticOccurrence diagnostic in propertyDiagnostics) {
            context.ReportDiagnostic(Diagnostic.Create(
                diagnostic.Descriptor,
                diagnostic.Property.Locations.FirstOrDefault(),
                diagnostic.Arguments));
        }

        foreach(AliasOccurrence occurrence in occurrences.Where(static occurrence => !occurrence.IsValid)) {
            context.ReportDiagnostic(Diagnostic.Create(
                _invalidAlias,
                occurrence.Location ?? occurrence.Symbol.Locations.FirstOrDefault(),
                occurrence.Alias));
        }

        foreach(IGrouping<string, AliasOccurrence> group in occurrences
                    .GroupBy(static occurrence => $"{occurrence.Scope}:{GetScopeName(occurrence)}:{occurrence.Alias}")) {
            AliasOccurrence[] entries = group.ToArray();
            if(entries.Length < 2) {
                continue;
            }

            AliasOccurrence first = entries[0];
            if(first.LayoutKind is not null) {
                context.ReportDiagnostic(Diagnostic.Create(
                    _duplicateLayout,
                    entries[1].Location,
                    first.LayoutKind,
                    first.Alias));
            } else {
                context.ReportDiagnostic(Diagnostic.Create(
                    _duplicateAlias,
                    entries[1].Location,
                    first.Alias));
            }
        }

        foreach(IGrouping<string, ValidationOccurrence> group in validations
                    .GroupBy(static validation => $"{validation.ContainingType.ToDisplayString()}:{validation.Expression}:{validation.Message}")) {
            ValidationOccurrence[] entries = group.ToArray();
            if(entries.Length > 1) {
                context.ReportDiagnostic(Diagnostic.Create(
                    _duplicateValidation,
                    entries[1].Location,
                    entries[0].Expression));
            }
        }
    }

    private static string GetScopeName(AliasOccurrence occurrence) =>
        occurrence.Symbol.ContainingType?.ToDisplayString() ?? string.Empty;

    private static bool HasContentTypeAttribute(AttributeData attribute) =>
        attribute.AttributeClass?.Name == "UCFContentTypeAttribute";

    private static bool IsValidAlias(string alias) =>
        alias.All(static character => char.IsLetterOrDigit(character)
                                      || char.IsWhiteSpace(character)
                                      || character is '.' or '-'
                                      || IsCombiningMark(character));

    private static bool IsCombiningMark(char character) {
        UnicodeCategory category = char.GetUnicodeCategory(character);
        return category is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.EnclosingMark;
    }

    private sealed class AliasOccurrence(
        string alias,
        ISymbol symbol,
        AliasScope scope,
        Location? location,
        bool isValid,
        string? layoutKind = null,
        string? layoutName = null,
        int sortOrder = 0) {
        public string Alias { get; } = alias;
        public ISymbol Symbol { get; } = symbol;
        public AliasScope Scope { get; } = scope;
        public Location? Location { get; } = location;
        public bool IsValid { get; } = isValid;
        public string? LayoutKind { get; } = layoutKind;
        public string? LayoutName { get; } = layoutName;
        public int SortOrder { get; } = sortOrder;
    }

    private sealed class ValidationOccurrence(
        string expression,
        string message,
        INamedTypeSymbol containingType,
        Location? location) {
        public string Expression { get; } = expression;
        public string Message { get; } = message;
        public INamedTypeSymbol ContainingType { get; } = containingType;
        public Location? Location { get; } = location;
    }

    private sealed class PropertyDiagnosticOccurrence(
        DiagnosticDescriptor descriptor,
        IPropertySymbol property,
        object[] arguments) {
        public DiagnosticDescriptor Descriptor { get; } = descriptor;
        public IPropertySymbol Property { get; } = property;
        public object[] Arguments { get; } = arguments;
    }

    private enum AliasScope {
        Global,
        ContainingType,
    }
}
