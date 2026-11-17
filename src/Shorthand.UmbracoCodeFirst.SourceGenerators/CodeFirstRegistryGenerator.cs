using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable IDE0055

namespace Shorthand.UmbracoCodeFirst.SourceGenerators;

[Generator]
public sealed partial class CodeFirstRegistryGenerator : IIncrementalGenerator {
    private const string _generatorName = "Shorthand.UmbracoCodeFirst.SourceGenerators";

    private static readonly string _generatorVersion =
        typeof(CodeFirstRegistryGenerator).Assembly.GetName().Version?.ToString()
        ?? throw new InvalidOperationException("The source generator assembly has no version.");

    private const string _documentTypeAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentAttribute";

    private const string _elementTypeAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFElementTypeAttribute";

    private const string _mediaTypeAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFMediaAttribute";

    private const string _memberTypeAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFMemberAttribute";

    private const string _dataTypeAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFDataTypeAttribute";

    private const string _propertyAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFPropertyAttribute";

    private const string _descriptionAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFDescriptionAttribute";

    private const string _groupAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFGroupAttribute";

    private const string _tabAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFTabAttribute";

    private const string _validationAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFValidationAttribute";

    private const string _compositeAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFCompositeAttribute";

    private const string _folderPathAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFFolderPathAttribute";

    private const string _collectionAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFCollectionAttribute";

    private const string _configurationDefinitionMetadataName =
        "Shorthand.UmbracoCodeFirst.Metadata.CodeFirstEditorConfigurationDefinition";

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var definitions = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax classDeclaration
                    && classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (syntaxContext, _) => {
                    var symbol = (INamedTypeSymbol)syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node)!;
                    return symbol.GetAttributes().Any(IsDefinitionAttribute) ? symbol : null;
                })
            .Where(static symbol => symbol is not null)
            .Select(static (symbol, _) => symbol!);
        var templates = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
            .Collect()
            .Combine(context.AnalyzerConfigOptionsProvider.Select(static (options, _) => {
                options.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var directory);
                options.GlobalOptions.TryGetValue("build_property.UmbracoCodeFirstViewsDirectory", out var viewsDirectory);
                options.GlobalOptions.TryGetValue("build_property.UmbracoCodeFirstGenerateTemplates", out var enabled);
                return (directory, viewsDirectory, enabled: !string.Equals(enabled, "false", StringComparison.OrdinalIgnoreCase));
            }));
        context.RegisterSourceOutput(
            definitions.Collect().Combine(context.CompilationProvider).Combine(templates),
            static (productionContext, input) => {
                var distinctDefinitions = input.Left.Left
                    .Distinct(SymbolEqualityComparer.Default)
                    .Cast<INamedTypeSymbol>()
                    .OrderBy(static symbol => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
                    .ToImmutableArray();

                var (directory, viewsDirectory, enabled) = input.Right.Right;
                var templateDefinitions = ReadTemplates(productionContext, input.Right.Left, directory, viewsDirectory, enabled);
                if(distinctDefinitions.Length == 0 && templateDefinitions.Count == 0) {
                    return;
                }

                if(input.Left.Right.SourceModule.ReferencedAssemblySymbols.Any(static assembly =>
                    assembly.GetTypeByMetadataName("Shorthand.UmbracoCodeFirst.UmbracoTemplates") is not null
                    && assembly.GetTypeByMetadataName("Shorthand.UmbracoCodeFirst.Generated.GeneratedCodeFirstDefinitionRegistry") is not null)) {
                    productionContext.ReportDiagnostic(Diagnostic.Create(_multipleTemplateRegistries, Location.None));
                    return;
                }

                var templateCompilation = input.Left.Right;
                if(templateDefinitions.Count > 0) {
                    var source = BuildTemplateAliases(templateDefinitions);
                    productionContext.AddSource("UmbracoTemplates.g.cs", source);
                    var parseOptions = input.Left.Right.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions;
                    templateCompilation = templateCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(source, parseOptions));
                }

                ReportUnresolvedPropertyAliasArguments(productionContext, distinctDefinitions, input.Left.Right);
                EmitRegistry(productionContext, distinctDefinitions, input.Left.Right, templateCompilation, templateDefinitions);
                EmitPublishedModels(productionContext, distinctDefinitions);
                EmitOtherPropertyAliases(productionContext, distinctDefinitions);
                EmitDataTypeAliases(productionContext, distinctDefinitions);
            });
    }

    private static void AppendGeneratedCodeAttribute(
        IndentedStringBuilder builder,
        bool fullyQualified = false) {
        builder.Append(fullyQualified
            ? "[global::System.CodeDom.Compiler.GeneratedCode("
            : "[GeneratedCode(");
        builder.Append(SymbolDisplay.FormatLiteral(_generatorName, true));
        builder.Append(", ");
        builder.Append(SymbolDisplay.FormatLiteral(_generatorVersion, true));
        builder.AppendLine(")]");
    }

    private static string SanitizeHintName(string value) {
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var character in value) {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.ToString();
    }

    private static string GetStringArgument(AttributeData attribute, int index) =>
        index < attribute.ConstructorArguments.Length
            ? attribute.ConstructorArguments[index].Value?.ToString() ?? string.Empty
            : string.Empty;

    private static string GetNameArgument(AttributeData attribute) =>
        attribute.ConstructorArguments.Length switch {
            _ when IsAttribute(attribute, _dataTypeAttributeMetadataName) && HasDataTypeAliasProperty(attribute)
                => GetStringArgument(attribute, 0),
            1 => GetStringArgument(attribute, 0),
            2 => IsAttribute(attribute, _propertyAttributeMetadataName)
                ? GetStringArgument(attribute, 0)
                : GetStringArgument(attribute, 1),
            _ => GetStringArgument(attribute, 1),
        };

    private static (string Name, string Alias) GetPropertyMetadata(
        AttributeData attribute,
        AttributeSyntax? syntax) {
        var explicitAlias = attribute.ConstructorArguments.Length >= 3;
        var name = GetNameArgument(attribute);
        if (name.Length == 0) {
            name = GetSyntaxStringArgument(syntax, explicitAlias ? 1 : 0);
        }

        var alias = GetNamedStringArgument(attribute, "Alias") is { Length: > 0 } namedAlias
            ? namedAlias
            : GetAliasArgument(attribute, name);
        if (alias.Length == 0) {
            alias = explicitAlias
                ? GetSyntaxStringArgument(syntax, 0)
                : ToAlias(name);
        }

        return (name, alias);
    }

    private static bool HasDataTypeAliasProperty(AttributeData attribute) =>
        IsAttribute(attribute, _dataTypeAttributeMetadataName)
        && attribute.AttributeClass?.GetMembers("Alias").OfType<IPropertySymbol>().Any() is true;

    private static string GetAliasArgument(AttributeData attribute, string name) =>
        attribute.ConstructorArguments.Length switch {
            _ when IsAttribute(attribute, _dataTypeAttributeMetadataName) && HasDataTypeAliasProperty(attribute)
                => GetNamedStringArgument(attribute, "Alias") is { Length: > 0 } alias
                    ? alias
                    : ToAlias(name),
            1 => ToAlias(name),
            2 when IsAttribute(attribute, _propertyAttributeMetadataName) => ToAlias(name),
            2 => GetStringArgument(attribute, 0),
            _ => GetStringArgument(attribute, 0),
        };

    private static AttributeSyntax? FindAttributeSyntax(ISymbol symbol, string metadataName) =>
        symbol.DeclaringSyntaxReferences
            .SelectMany(static reference => reference.GetSyntax().DescendantNodes().OfType<AttributeSyntax>())
            .FirstOrDefault(attribute => attribute.Name.ToString().EndsWith(
                metadataName.Substring(metadataName.LastIndexOf('.') + 1).Replace("Attribute", string.Empty),
                StringComparison.Ordinal));

    private static string GetSyntaxStringArgument(AttributeSyntax? syntax, int index) {
        if (syntax?.ArgumentList?.Arguments
                .Where(static argument => argument.NameColon is null && argument.NameEquals is null)
                .ElementAtOrDefault(index)?.Expression is not ExpressionSyntax expression) {
            return string.Empty;
        }

        return expression is LiteralExpressionSyntax literal
            ? literal.Token.ValueText
            : string.Empty;
    }

    private static AttributeData? FindAttribute(ISymbol symbol, string metadataName) =>
        symbol.GetAttributes().FirstOrDefault(attribute => IsAttribute(attribute, metadataName));

    private static AttributeData? FindDocumentTypeAttribute(ISymbol symbol) =>
        symbol.GetAttributes().FirstOrDefault(attribute =>
            IsDerivedAttribute(attribute, _documentTypeAttributeMetadataName)
            || IsAttribute(attribute, _elementTypeAttributeMetadataName));

    private static AttributeData? FindContentTypeAttribute(ISymbol symbol) =>
        symbol.GetAttributes().FirstOrDefault(attribute =>
            IsDerivedAttribute(attribute, _documentTypeAttributeMetadataName)
            || IsAttribute(
                attribute,
                _elementTypeAttributeMetadataName,
                _mediaTypeAttributeMetadataName,
                _memberTypeAttributeMetadataName));

    private static AttributeData? FindLayoutAttribute(ISymbol symbol, string metadataName) =>
        symbol.GetAttributes().FirstOrDefault(attribute => IsDerivedAttribute(attribute, metadataName));

    private static AttributeData? FindValidationAttribute(ISymbol symbol) =>
        symbol.GetAttributes().FirstOrDefault(attribute => IsDerivedAttribute(attribute, _validationAttributeMetadataName));

    private static ValidationMetadata? GetValidationMetadata(
        AttributeData? attribute,
        Compilation compilation) {
        if (attribute is null) {
            return null;
        }

        IReadOnlyList<object?> constructorArguments = attribute.ConstructorArguments
            .Select(static argument => argument.Value)
            .ToArray();
        if (constructorArguments.Count == 0) {
            constructorArguments = GetBaseConstructorArguments(attribute, compilation);
        }

        if (constructorArguments.Count == 0 || constructorArguments[0] is not string expression) {
            return null;
        }

        return new ValidationMetadata(
            expression,
            constructorArguments.Count > 1 ? constructorArguments[1] as string : null);
    }

    private static GroupMetadata? GetLayoutMetadata(AttributeData? attribute, Compilation compilation) {
        if (attribute is null) {
            return null;
        }

        IReadOnlyList<object?> constructorArguments = attribute.ConstructorArguments
            .Select(static argument => argument.Value)
            .ToArray();
        if (constructorArguments.Count == 0) {
            constructorArguments = GetBaseConstructorArguments(attribute, compilation);
        }

        if (constructorArguments.Count == 0) {
            return null;
        }

        var hasImplicitAlias = constructorArguments.Count == 1
            || constructorArguments.Count == 2 && constructorArguments[1] is int;
        var name = hasImplicitAlias
            ? constructorArguments[0]?.ToString() ?? string.Empty
            : constructorArguments[1]?.ToString() ?? string.Empty;
        var alias = hasImplicitAlias
            ? ToAlias(name)
            : constructorArguments[0]?.ToString() ?? ToAlias(name);
        var sortOrder = constructorArguments.Count > 2 && constructorArguments[2] is int constructorSortOrder
            ? constructorSortOrder
            : constructorArguments.Count == 2 && constructorArguments[1] is int implicitSortOrder
                ? implicitSortOrder
                : GetNamedIntArgument(attribute, "SortOrder");
        return new GroupMetadata(alias, name, sortOrder);
    }

    private static IReadOnlyList<object?> GetBaseConstructorArguments(
        AttributeData attribute,
        Compilation compilation) {
        if (attribute.AttributeClass is null) {
            return Array.Empty<object?>();
        }

        foreach (var syntaxReference in attribute.AttributeClass.DeclaringSyntaxReferences) {
            if (syntaxReference.GetSyntax() is not ClassDeclarationSyntax classDeclaration) {
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var primaryConstructorArguments = classDeclaration.BaseList?
                .Types
                .SelectMany(static baseType => baseType.ChildNodes().OfType<ArgumentListSyntax>())
                .FirstOrDefault();
            if (primaryConstructorArguments is not null) {
                return primaryConstructorArguments.Arguments
                    .Select(argument => semanticModel.GetConstantValue(argument.Expression))
                    .Where(static value => value.HasValue)
                    .Select(static value => value.Value)
                    .ToArray();
            }

            var constructor = classDeclaration.Members
                .OfType<ConstructorDeclarationSyntax>()
                .FirstOrDefault(candidate => candidate.Initializer?.Kind() == SyntaxKind.BaseConstructorInitializer);
            if (constructor?.Initializer is not ConstructorInitializerSyntax initializer) {
                continue;
            }

            return initializer.ArgumentList.Arguments
                .Select(argument => semanticModel.GetConstantValue(argument.Expression))
                .Where(static value => value.HasValue)
                .Select(static value => value.Value)
                .ToArray();
        }

        return Array.Empty<object?>();
    }

    private static bool IsDerivedAttribute(AttributeData attribute, string metadataName) {
        var attributeClass = attribute.AttributeClass;
        while (attributeClass is not null) {
            if (string.Equals(
                    attributeClass.ToDisplayString(),
                    metadataName,
                    StringComparison.Ordinal)) {
                return true;
            }

            attributeClass = attributeClass.BaseType;
        }

        return false;
    }

    private static string GetConstructorStringLiteral(AttributeData? attribute, int index = 0) =>
        attribute is null || attribute.ConstructorArguments.Length <= index
            ? "null"
            : SymbolDisplay.FormatLiteral(GetStringArgument(attribute, index), true);

    private static bool IsAttribute(AttributeData attribute, params string[] metadataNames) =>
        metadataNames.Contains(attribute.AttributeClass?.ToDisplayString(), StringComparer.Ordinal);

    private static bool IsDefinitionAttribute(AttributeData attribute) =>
        IsDerivedAttribute(attribute, _documentTypeAttributeMetadataName)
        || IsAttribute(
            attribute,
            _elementTypeAttributeMetadataName,
            _mediaTypeAttributeMetadataName,
            _memberTypeAttributeMetadataName,
            _dataTypeAttributeMetadataName);

    internal static string ToAlias(string value) {
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

    private static string GetNamedStringArgument(AttributeData attribute, string name) =>
        attribute.NamedArguments.FirstOrDefault(argument => argument.Key == name).Value.Value?.ToString() ?? string.Empty;

    private static string GetNamedStringLiteral(AttributeData attribute, string name) {
        var argument =
            attribute.NamedArguments.FirstOrDefault(candidate => candidate.Key == name);
        return argument.Key is null || argument.Value.IsNull
            ? "null"
            : SymbolDisplay.FormatLiteral(argument.Value.Value?.ToString() ?? string.Empty, true);
    }

    private static int GetNamedIntArgument(AttributeData? attribute, string name) =>
        attribute?.NamedArguments.FirstOrDefault(argument => argument.Key == name).Value.Value is int value ? value : 0;

    private static bool GetNamedBoolArgument(AttributeData? attribute, string name) =>
        attribute?.NamedArguments.FirstOrDefault(argument => argument.Key == name).Value.Value is true;

    private sealed class IndentedStringBuilder {
        private readonly System.Text.StringBuilder _builder = new();
        private bool _atLineStart = true;

        public int Indent { get; set; }

        public void Append(string value) {
            AppendIndent();
            _builder.Append(value);
        }

        public void AppendLine() {
            _builder.AppendLine();
            _atLineStart = true;
        }

        public void AppendLine(string value) {
            AppendIndent();
            _builder.AppendLine(value);
            _atLineStart = true;
        }

        public override string ToString() => _builder.ToString();

        private void AppendIndent() {
            if (!_atLineStart) {
                return;
            }

            _builder.Append(' ', Indent * 4);
            _atLineStart = false;
        }
    }

    private sealed class GroupMetadata(string alias, string name, int sortOrder) {
        public string Alias { get; } = alias;

        public string Name { get; } = name;

        public int SortOrder { get; } = sortOrder;
    }

    private sealed class ValidationMetadata(string expression, string? message) {
        public string Expression { get; } = expression;

        public string? Message { get; } = message;
    }
}
