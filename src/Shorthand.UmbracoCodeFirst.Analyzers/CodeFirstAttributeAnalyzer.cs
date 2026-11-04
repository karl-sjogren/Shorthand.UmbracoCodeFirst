using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CodeFirstAttributeAnalyzer : DiagnosticAnalyzer {
    private const string _documentationBaseUrl =
        "https://github.com/karl-sjogren/Shorthand.UmbracoCodeFirst/blob/feature/initial-implementation/docs/analyzers";

    public const string InvalidAliasId = "UCF100";
    public const string DuplicateAliasId = "UCF101";
    public const string DuplicateLayoutId = "UCF102";
    public const string DuplicateValidationId = "UCF103";
    public const string UnsupportedPropertyTypeId = "UCF104";
    public const string PropertyOutsideContentTypeId = "UCF105";
    public const string InvalidCompositionTargetId = "UCF106";
    public const string InvalidCompositionGraphId = "UCF107";
    public const string UnknownEditorAliasId = "UCF108";
    public const string IncompatibleEditorTypeId = "UCF109";

#pragma warning disable RS2008
    private static readonly DiagnosticDescriptor _invalidAlias = new(
        InvalidAliasId,
        "Invalid code-first alias",
        "Alias '{0}' contains characters that are not valid for a code-first alias",
        "CodeFirst",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF100.md",
        customTags: new[] { "CompilationEnd" });

    private static readonly DiagnosticDescriptor _duplicateAlias = new(
        DuplicateAliasId,
        "Duplicate code-first alias",
        "Alias '{0}' is declared more than once in the same code-first scope",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF101.md",
        customTags: new[] { "CompilationEnd" });

    private static readonly DiagnosticDescriptor _duplicateLayout = new(
        DuplicateLayoutId,
        "Repeated layout configuration",
        "The {0} alias '{1}' is repeated; use one shared derived attribute",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF102.md",
        customTags: new[] { "CompilationEnd" });

    private static readonly DiagnosticDescriptor _duplicateValidation = new(
        DuplicateValidationId,
        "Repeated validation configuration",
        "The validation '{0}' is repeated; use one shared derived UCFValidation attribute",
        "CodeFirst",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF103.md",
        customTags: new[] { "CompilationEnd" });

    private static readonly DiagnosticDescriptor _unsupportedPropertyType = new(
        UnsupportedPropertyTypeId,
        "Unsupported code-first property type",
        "Property '{0}' has type '{1}', which is not supported by the code-first contract",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF104.md");

    private static readonly DiagnosticDescriptor _propertyOutsideContentType = new(
        PropertyOutsideContentTypeId,
        "Code-first property requires a content type",
        "Property '{0}' uses UCFProperty but its containing class is not a UCFContentType",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF105.md");

    private static readonly DiagnosticDescriptor _invalidCompositionTarget = new(
        InvalidCompositionTargetId,
        "Invalid content composition target",
        "Composition target '{0}' must have UCFContentTypeAttribute",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF106.md",
        customTags: new[] { "CompilationEnd" });

    private static readonly DiagnosticDescriptor _invalidCompositionGraph = new(
        InvalidCompositionGraphId,
        "Invalid content composition graph",
        "Composition from '{0}' to '{1}' is duplicated or creates a composition cycle",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF107.md",
        customTags: new[] { "CompilationEnd" });

    private static readonly DiagnosticDescriptor _unknownEditorAlias = new(
        UnknownEditorAliasId,
        "Unknown Umbraco property editor",
        "Data Type '{0}' references editor alias '{1}', which is not in the built-in editor catalog",
        "CodeFirst",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF108.md",
        customTags: new[] { "CompilationEnd" });

    private static readonly DiagnosticDescriptor _incompatibleEditorType = new(
        IncompatibleEditorTypeId,
        "Incompatible Umbraco property editor type",
        "Property '{0}' has CLR type '{1}', but editor '{2}' documents return type '{3}'",
        "CodeFirst",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: _documentationBaseUrl + "/UCF109.md",
        customTags: new[] { "CompilationEnd" });
#pragma warning restore RS2008

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [
            _invalidAlias,
            _duplicateAlias,
            _duplicateLayout,
            _duplicateValidation,
            _unsupportedPropertyType,
            _propertyOutsideContentType,
            _invalidCompositionTarget,
            _invalidCompositionGraph,
            _unknownEditorAlias,
            _incompatibleEditorType,
        ];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilationContext => {
            var editorCatalog = EditorCatalog.Load(compilationContext.Options.AdditionalFiles);
            var occurrences = new ConcurrentBag<AliasOccurrence>();
            var validations = new ConcurrentBag<ValidationOccurrence>();
            var propertyDiagnostics = new ConcurrentBag<PropertyDiagnosticOccurrence>();
            var compositions = new ConcurrentBag<CompositionOccurrence>();
            var dataTypes = new ConcurrentBag<DataTypeOccurrence>();
            var editorProperties = new ConcurrentBag<EditorPropertyOccurrence>();

            compilationContext.RegisterSymbolAction(
                symbolContext => Collect(
                    symbolContext.Symbol,
                    occurrences,
                    validations,
                    propertyDiagnostics,
                    compositions,
                    dataTypes,
                    editorProperties),
                SymbolKind.NamedType,
                SymbolKind.Property);
            compilationContext.RegisterCompilationEndAction(
                endContext => Report(
                    endContext,
                    occurrences,
                    validations,
                    propertyDiagnostics,
                    compositions,
                    dataTypes,
                    editorProperties,
                    editorCatalog));
        });
    }

    private static void Collect(
        ISymbol symbol,
        ConcurrentBag<AliasOccurrence> occurrences,
        ConcurrentBag<ValidationOccurrence> validations,
        ConcurrentBag<PropertyDiagnosticOccurrence> propertyDiagnostics,
        ConcurrentBag<CompositionOccurrence> compositions,
        ConcurrentBag<DataTypeOccurrence> dataTypes,
        ConcurrentBag<EditorPropertyOccurrence> editorProperties) {
        foreach(var attribute in symbol.GetAttributes()) {
            var attributeName = attribute.AttributeClass?.Name ?? string.Empty;
            if(attributeName is "UCFContentTypeAttribute" or "UCFDataTypeAttribute") {
                AddAlias(attribute, symbol, occurrences, AliasScope.Global);
                if(attributeName == "UCFDataTypeAttribute") {
                    AddDataType(attribute, symbol, dataTypes);
                }
            } else if(attributeName == "UCFCompositeAttribute"
                      && symbol is INamedTypeSymbol contentType) {
                CollectComposition(contentType, attribute, compositions);
            } else if(attributeName is "UCFPropertyAttribute") {
                AddAlias(attribute, symbol, occurrences, AliasScope.ContainingType);
                if(symbol is IPropertySymbol property) {
                    CollectPropertyContractDiagnostics(property, propertyDiagnostics);
                    AddEditorProperty(attribute, property, editorProperties);
                }
            } else if(attributeName is "UCFGroupAttribute" or "UCFTabAttribute") {
                AddLayout(attribute, symbol, occurrences, attributeName == "UCFGroupAttribute");
            } else if(attributeName == "UCFValidationAttribute"
                      || attribute.AttributeClass?.BaseType?.Name == "UCFValidationAttribute") {
                AddValidation(attribute, symbol, validations);
            }
        }
    }

    private static void CollectComposition(
        INamedTypeSymbol source,
        AttributeData attribute,
        ConcurrentBag<CompositionOccurrence> compositions) {
        if(attribute.ConstructorArguments.Length == 0
            || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol target) {
            return;
        }

        compositions.Add(new CompositionOccurrence(
            source,
            target,
            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
            target.GetAttributes().Any(HasContentTypeAttribute)));
    }

    private static void AddDataType(
        AttributeData attribute,
        ISymbol symbol,
        ConcurrentBag<DataTypeOccurrence> dataTypes) {
        var editorIndex = attribute.ConstructorArguments.Length == 2 ? 1 : 2;
        if(attribute.ConstructorArguments.Length <= editorIndex
            || attribute.ConstructorArguments[editorIndex].Value is not string editorAlias) {
            return;
        }

        var alias = GetDefinitionAlias(attribute);
        if(alias.Length > 0) {
            dataTypes.Add(new DataTypeOccurrence(alias, editorAlias, symbol));
        }
    }

    private static void AddEditorProperty(
        AttributeData attribute,
        IPropertySymbol property,
        ConcurrentBag<EditorPropertyOccurrence> editorProperties) {
        var dataTypeIndex = attribute.ConstructorArguments.Length == 2 ? 1 : 2;
        if(attribute.ConstructorArguments.Length > dataTypeIndex
            && attribute.ConstructorArguments[dataTypeIndex].Value is string dataTypeAlias) {
            editorProperties.Add(new EditorPropertyOccurrence(property, dataTypeAlias));
        }
    }

    private static string GetDefinitionAlias(AttributeData attribute) {
        var hasExplicitAlias = attribute.ConstructorArguments.Length >= 3;
        var nameIndex = hasExplicitAlias ? 1 : 0;
        if(attribute.ConstructorArguments.Length <= nameIndex
            || attribute.ConstructorArguments[nameIndex].Value is not string name) {
            return string.Empty;
        }

        return hasExplicitAlias
            ? attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty
            : ToAlias(name);
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
        var attributeName = attribute.AttributeClass?.Name ?? string.Empty;
        var hasExplicitAlias = attributeName switch {
            "UCFContentTypeAttribute" => attribute.ConstructorArguments.Length >= 2,
            "UCFDataTypeAttribute" => attribute.ConstructorArguments.Length >= 3,
            "UCFPropertyAttribute" => attribute.ConstructorArguments.Length >= 3,
            _ => false,
        };
        var nameIndex = attributeName switch {
            "UCFPropertyAttribute" => hasExplicitAlias ? 1 : 0,
            _ => hasExplicitAlias ? 1 : 0,
        };
        if(attribute.ConstructorArguments.Length <= nameIndex
            || attribute.ConstructorArguments[nameIndex].Value is not string name) {
            return;
        }

        var alias = hasExplicitAlias
            ? attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty
            : ToAlias(name);
        if(alias.Length == 0) {
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
        if(attribute.ConstructorArguments.Length == 0
            || attribute.ConstructorArguments[attribute.ConstructorArguments.Length - 1].Value
                is not string name) {
            return;
        }

        var alias = attribute.ConstructorArguments.Length == 1
            ? ToAlias(name)
            : attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
        var sortOrder = attribute.ConstructorArguments.Length > 2
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

        var message = attribute.ConstructorArguments.Length > 1
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
        ConcurrentBag<PropertyDiagnosticOccurrence> propertyDiagnostics,
        ConcurrentBag<CompositionOccurrence> compositions,
        ConcurrentBag<DataTypeOccurrence> dataTypes,
        ConcurrentBag<EditorPropertyOccurrence> editorProperties,
        EditorCatalog editorCatalog) {
        ReportCompositionDiagnostics(context, compositions);
        ReportEditorDiagnostics(context, dataTypes, editorProperties, editorCatalog);

        foreach(var diagnostic in propertyDiagnostics) {
            context.ReportDiagnostic(Diagnostic.Create(
                diagnostic.Descriptor,
                diagnostic.Property.Locations.FirstOrDefault(),
                diagnostic.Arguments));
        }

        foreach(var occurrence in occurrences.Where(static occurrence => !occurrence.IsValid)) {
            context.ReportDiagnostic(Diagnostic.Create(
                _invalidAlias,
                occurrence.Location ?? occurrence.Symbol.Locations.FirstOrDefault(),
                occurrence.Alias));
        }

        foreach(var group in occurrences.Where(static occurrence =>
                    occurrence.Symbol is not IPropertySymbol || occurrence.LayoutKind is not null)
                    .GroupBy(static occurrence => $"{occurrence.Scope}:{GetScopeName(occurrence)}:{occurrence.Alias}")) {
            var entries = group.ToArray();
            if(entries.Length < 2) {
                continue;
            }

            var first = entries[0];
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

        ReportPropertyAliasDiagnostics(context, occurrences, compositions);

        foreach(var group in validations
                    .GroupBy(static validation => $"{validation.ContainingType.ToDisplayString()}:{validation.Expression}:{validation.Message}")) {
            var entries = group.ToArray();
            if(entries.Length > 1) {
                context.ReportDiagnostic(Diagnostic.Create(
                    _duplicateValidation,
                    entries[1].Location,
                    entries[0].Expression));
            }
        }
    }

    private static void ReportEditorDiagnostics(
        CompilationAnalysisContext context,
        IEnumerable<DataTypeOccurrence> dataTypes,
        IEnumerable<EditorPropertyOccurrence> editorProperties,
        EditorCatalog editorCatalog) {
        if(editorCatalog.IsEmpty) {
            return;
        }

        var editorByDataType = dataTypes
            .GroupBy(static dataType => dataType.Alias, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);

        foreach(var dataType in editorByDataType.Values) {
            if(!editorCatalog.TryGet(dataType.EditorAlias, out _)) {
                context.ReportDiagnostic(Diagnostic.Create(
                    _unknownEditorAlias,
                    dataType.Symbol.Locations.FirstOrDefault(),
                    dataType.Alias,
                    dataType.EditorAlias));
            }
        }

        foreach(var property in editorProperties) {
            if(!editorByDataType.TryGetValue(property.DataTypeAlias, out var dataType)
                || !editorCatalog.TryGet(dataType.EditorAlias, out var editor)
                || IsCompatible(property.Property.Type, editor.ReturnType)) {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                _incompatibleEditorType,
                property.Property.Locations.FirstOrDefault(),
                property.Property.Name,
                property.Property.Type.ToDisplayString(),
                dataType.EditorAlias,
                editor.ReturnType));
        }
    }

    private static bool IsCompatible(ITypeSymbol type, string returnType) {
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
            _ when returnType.StartsWith("System.Collections.Generic.IEnumerable<string>", StringComparison.Ordinal)
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

    private static void ReportPropertyAliasDiagnostics(
        CompilationAnalysisContext context,
        ConcurrentBag<AliasOccurrence> occurrences,
        ConcurrentBag<CompositionOccurrence> compositions) {
        var propertyOccurrences = occurrences
            .Where(static occurrence => occurrence.Symbol is IPropertySymbol)
            .ToArray();

        foreach(var group in propertyOccurrences
                    .GroupBy(static occurrence =>
                        $"{GetScopeName(occurrence)}:{occurrence.Alias}")) {
            var entries = group.ToArray();
            if(entries.Length > 1) {
                ReportDuplicateAlias(context, entries[1]);
            }
        }

        Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>> graph =
            new(SymbolEqualityComparer.Default);
        foreach(var composition in compositions.Where(static composition => composition.TargetHasContentType)) {
            if(!graph.TryGetValue(composition.Source, out var targets)) {
                targets = [];
                graph.Add(composition.Source, targets);
            }

            targets.Add(composition.Target);
        }

        var contentTypes = propertyOccurrences
            .Select(static occurrence => occurrence.Symbol.ContainingType)
            .OfType<INamedTypeSymbol>()
            .Where(static type => type.GetAttributes().Any(HasContentTypeAttribute))
            .Concat(graph.Keys)
            .Concat(graph.Values.SelectMany(static targets => targets))
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach(var contentType in contentTypes) {
            HashSet<INamedTypeSymbol> effectiveTypes = new(SymbolEqualityComparer.Default);
            CollectEffectiveCompositionTypes(contentType, graph, effectiveTypes);

            foreach(var group in propertyOccurrences
                        .Where(occurrence => occurrence.Symbol.ContainingType is INamedTypeSymbol containingType
                                             && effectiveTypes.Contains(containingType))
                        .GroupBy(static occurrence => occurrence.Alias)) {
                var entries = group.ToArray();
                if(entries.Select(static entry => entry.Symbol.ContainingType)
                    .Distinct(SymbolEqualityComparer.Default)
                    .Count() > 1) {
                    ReportDuplicateAlias(context, entries[1]);
                }
            }
        }
    }

    private static void CollectEffectiveCompositionTypes(
        INamedTypeSymbol source,
        IReadOnlyDictionary<INamedTypeSymbol, List<INamedTypeSymbol>> graph,
        HashSet<INamedTypeSymbol> effectiveTypes) {
        if(!effectiveTypes.Add(source)
            || !graph.TryGetValue(source, out var targets)) {
            return;
        }

        foreach(var target in targets) {
            CollectEffectiveCompositionTypes(target, graph, effectiveTypes);
        }
    }

    private static void ReportDuplicateAlias(
        CompilationAnalysisContext context,
        AliasOccurrence occurrence) {
        context.ReportDiagnostic(Diagnostic.Create(
            _duplicateAlias,
            occurrence.Location ?? occurrence.Symbol.Locations.FirstOrDefault(),
            occurrence.Alias));
    }

    private static void ReportCompositionDiagnostics(
        CompilationAnalysisContext context,
        ConcurrentBag<CompositionOccurrence> compositions) {
        foreach(var occurrence in compositions.Where(
                    static occurrence => !occurrence.TargetHasContentType)) {
            context.ReportDiagnostic(Diagnostic.Create(
                _invalidCompositionTarget,
                occurrence.Location ?? occurrence.Source.Locations.FirstOrDefault(),
                occurrence.Target.Name));
        }

        var validCompositions = compositions
            .Where(static occurrence => occurrence.TargetHasContentType)
            .ToArray();
        foreach(var duplicate in validCompositions
                    .GroupBy(static occurrence =>
                        $"{occurrence.Source.ToDisplayString()}:{occurrence.Target.ToDisplayString()}")) {
            var entries = duplicate.ToArray();
            if(entries.Length > 1) {
                ReportInvalidComposition(context, entries[1]);
            }
        }

        foreach(var occurrence in validCompositions.Where(
                    static occurrence => SymbolEqualityComparer.Default.Equals(
                        occurrence.Source,
                        occurrence.Target))) {
            ReportInvalidComposition(context, occurrence);
        }

        Dictionary<INamedTypeSymbol, List<CompositionOccurrence>> graph =
            new(SymbolEqualityComparer.Default);
        foreach(var occurrence in validCompositions) {
            if(!graph.TryGetValue(occurrence.Source, out var edges)) {
                edges = [];
                graph.Add(occurrence.Source, edges);
            }

            edges.Add(occurrence);
        }

        HashSet<INamedTypeSymbol> visiting = new(SymbolEqualityComparer.Default);
        HashSet<INamedTypeSymbol> visited = new(SymbolEqualityComparer.Default);
        foreach(var source in graph.Keys) {
            VisitCompositionGraph(source, graph, visiting, visited, context);
        }
    }

    private static void VisitCompositionGraph(
        INamedTypeSymbol source,
        IReadOnlyDictionary<INamedTypeSymbol, List<CompositionOccurrence>> graph,
        HashSet<INamedTypeSymbol> visiting,
        HashSet<INamedTypeSymbol> visited,
        CompilationAnalysisContext context) {
        if(visited.Contains(source) || !visiting.Add(source)) {
            return;
        }

        if(graph.TryGetValue(source, out var edges)) {
            foreach(var edge in edges) {
                if(visiting.Contains(edge.Target)) {
                    ReportInvalidComposition(context, edge);
                    continue;
                }

                VisitCompositionGraph(edge.Target, graph, visiting, visited, context);
            }
        }

        visiting.Remove(source);
        visited.Add(source);
    }

    private static void ReportInvalidComposition(
        CompilationAnalysisContext context,
        CompositionOccurrence occurrence) {
        context.ReportDiagnostic(Diagnostic.Create(
            _invalidCompositionGraph,
            occurrence.Location ?? occurrence.Source.Locations.FirstOrDefault(),
            occurrence.Source.Name,
            occurrence.Target.Name));
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
        var category = char.GetUnicodeCategory(character);
        return category is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.EnclosingMark;
    }

    private static string ToAlias(string value) {
        var words = new List<string>();
        var word = new System.Text.StringBuilder();

        for(var index = 0; index < value.Length; index++) {
            var character = value[index];
            if(!char.IsLetterOrDigit(character)) {
                AddWord();
                continue;
            }

            var startsNewWord = word.Length > 0
                && char.IsUpper(character)
                && (char.IsLower(value[index - 1])
                    || char.IsDigit(value[index - 1])
                    || (index + 1 < value.Length && char.IsLower(value[index + 1])));
            if(startsNewWord) {
                AddWord();
            }

            word.Append(character);
        }

        AddWord();
        if(words.Count == 0) {
            return string.Empty;
        }

        return string.Join(
            string.Empty,
            new[] { words[0].ToLowerInvariant() }
                .Concat(words.Skip(1).Select(word =>
                    char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant())));

        void AddWord() {
            if(word.Length > 0) {
                words.Add(word.ToString());
                word.Clear();
            }
        }
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

    private sealed class CompositionOccurrence(
        INamedTypeSymbol source,
        INamedTypeSymbol target,
        Location? location,
        bool targetHasContentType) {
        public INamedTypeSymbol Source { get; } = source;
        public INamedTypeSymbol Target { get; } = target;
        public Location? Location { get; } = location;
        public bool TargetHasContentType { get; } = targetHasContentType;
    }

    private sealed class DataTypeOccurrence(string alias, string editorAlias, ISymbol symbol) {
        public string Alias { get; } = alias;
        public string EditorAlias { get; } = editorAlias;
        public ISymbol Symbol { get; } = symbol;
    }

    private sealed class EditorPropertyOccurrence(IPropertySymbol property, string dataTypeAlias) {
        public IPropertySymbol Property { get; } = property;
        public string DataTypeAlias { get; } = dataTypeAlias;
    }

    private sealed class EditorCatalog {
        private readonly ImmutableDictionary<string, EditorMetadata> _editors;

        private EditorCatalog(ImmutableDictionary<string, EditorMetadata> editors) {
            _editors = editors;
        }

        public bool IsEmpty => _editors.IsEmpty;

        public bool TryGet(string alias, out EditorMetadata metadata) =>
            _editors.TryGetValue(alias, out metadata!);

        public static EditorCatalog Load(ImmutableArray<AdditionalText> additionalFiles) {
            var metadataFile = additionalFiles.FirstOrDefault(static file =>
                file.Path.EndsWith(
                    "umbraco-property-editors.json",
                    StringComparison.OrdinalIgnoreCase));
            if(metadataFile is null) {
                return new EditorCatalog(ImmutableDictionary<string, EditorMetadata>.Empty);
            }

            var content = metadataFile.GetText()?.ToString() ?? string.Empty;
            if(content.Length == 0) {
                return new EditorCatalog(ImmutableDictionary<string, EditorMetadata>.Empty);
            }

            try {
                using var document = JsonDocument.Parse(content);
                if(!document.RootElement.TryGetProperty("editors", out var editors)
                    || editors.ValueKind != JsonValueKind.Array) {
                    return new EditorCatalog(ImmutableDictionary<string, EditorMetadata>.Empty);
                }

                var builder = ImmutableDictionary.CreateBuilder<string, EditorMetadata>(
                    StringComparer.Ordinal);
                foreach(var editor in editors.EnumerateArray()) {
                    var alias = GetJsonString(editor, "alias");
                    var returnType = GetJsonString(editor, "returnType");
                    if(!string.IsNullOrWhiteSpace(alias) && !string.IsNullOrWhiteSpace(returnType)) {
                        builder[alias!] = new EditorMetadata(alias!, returnType!);
                    }
                }

                return new EditorCatalog(builder.ToImmutable());
            } catch(JsonException) {
                return new EditorCatalog(ImmutableDictionary<string, EditorMetadata>.Empty);
            }
        }

        private static string? GetJsonString(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var property)
                && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
    }

    private sealed class EditorMetadata(string alias, string returnType) {
        public string Alias { get; } = alias;
        public string ReturnType { get; } = returnType;
    }

    private enum AliasScope {
        Global,
        ContainingType,
    }
}
