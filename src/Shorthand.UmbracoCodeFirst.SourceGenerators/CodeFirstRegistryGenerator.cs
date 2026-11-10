using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable IDE0055

namespace Shorthand.UmbracoCodeFirst.SourceGenerators;

[Generator]
public sealed class CodeFirstRegistryGenerator : IIncrementalGenerator {
    private const string _generatorName = "Shorthand.UmbracoCodeFirst.SourceGenerators";

    private static readonly string _generatorVersion =
        typeof(CodeFirstRegistryGenerator).Assembly.GetName().Version?.ToString()
        ?? throw new InvalidOperationException("The source generator assembly has no version.");

    private const string _documentTypeAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFDocumentTypeAttribute";

    private const string _elementTypeAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFElementTypeAttribute";

    private const string _mediaTypeAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFMediaTypeAttribute";

    private const string _memberTypeAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFMemberTypeAttribute";

    private const string _dataTypeAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFDataTypeAttribute";

    private const string _propertyAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFPropertyAttribute";

    private const string _descriptionAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFDescriptionAttribute";

    private const string _groupAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFGroupAttribute";

    private const string _tabAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFTabAttribute";

    private const string _validationAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFValidationAttribute";

    private const string _compositeAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFCompositeAttribute";

    private const string _folderPathAttributeMetadataName = "Shorthand.UmbracoCodeFirst.Attributes.UCFFolderPathAttribute";

    private const string _configurationDefinitionMetadataName =
        "Shorthand.UmbracoCodeFirst.Metadata.CodeFirstEditorConfigurationDefinition";

#pragma warning disable RS2008
    private static readonly DiagnosticDescriptor _unsupportedPropertyType = new(
        "UCF001",
        "Unsupported published model property type",
        "Property '{0}' on content type '{1}' has type '{2}', which cannot be generated as a published model property",
        "CodeFirst",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

#pragma warning disable RS2008
    private static readonly DiagnosticDescriptor _invalidConfigurationType = new(
        "UCF111",
        "Invalid Data Type configuration type",
        "Configuration type '{0}' for Data Type '{1}' must expose either a public constant string named 'Json' or supported public constant settings",
        "CodeFirst",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
#pragma warning restore RS2008

#pragma warning disable RS2008
    private static readonly DiagnosticDescriptor _unsupportedConfigurationSetting = new(
        "UCF112",
        "Unsupported Data Type configuration setting",
        "Configuration setting '{0}' on '{1}' has type '{2}', which cannot be serialized as editor configuration",
        "CodeFirst",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
#pragma warning restore RS2008
#pragma warning restore RS2008

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
        context.RegisterSourceOutput(
            definitions.Collect().Combine(context.CompilationProvider),
            static (productionContext, input) => {
                var distinctDefinitions = input.Left
                    .Distinct(SymbolEqualityComparer.Default)
                    .Cast<INamedTypeSymbol>()
                    .OrderBy(static symbol => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
                    .ToImmutableArray();

                if(distinctDefinitions.Length == 0) {
                    return;
                }

                EmitRegistry(productionContext, distinctDefinitions, input.Right);
                EmitPublishedModels(productionContext, distinctDefinitions);
                EmitDataTypeAliases(productionContext, distinctDefinitions);
            });
    }

    private static void EmitRegistry(
        SourceProductionContext context,
        ImmutableArray<INamedTypeSymbol> definitionSymbols,
        Compilation compilation) {
        IEnumerable<INamedTypeSymbol> definitions = definitionSymbols
            .OrderBy(static symbol => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal);

        var builder = new IndentedStringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("namespace Shorthand.UmbracoCodeFirst.Generated;");
        builder.AppendLine();
        AppendGeneratedCodeAttribute(builder, fullyQualified: true);
        builder.AppendLine("public sealed class GeneratedCodeFirstDefinitionRegistry : global::Shorthand.UmbracoCodeFirst.Metadata.ICodeFirstDefinitionRegistry {");
        builder.Indent++;
        builder.Append("public global::System.Collections.Generic.IReadOnlyList<global::Shorthand.UmbracoCodeFirst.Metadata.CodeFirstDefinition> ");
        builder.AppendLine("Definitions { get; } = [");
        builder.Indent++;

        var dataTypeAliases = BuildDataTypeAliasMap(definitionSymbols);
        foreach (var definition in definitions) {
            AppendDefinition(builder, definition, compilation, dataTypeAliases, context);
        }

        builder.Indent--;
        builder.AppendLine("];");
        builder.Indent--;
        builder.AppendLine("}");

        context.AddSource("CodeFirstRegistry.g.cs", builder.ToString());
    }

    private static void EmitPublishedModels(
        SourceProductionContext context,
        ImmutableArray<INamedTypeSymbol> definitions) {
        foreach (var definition in definitions) {
            if (FindDocumentTypeAttribute(definition) is not AttributeData documentTypeAttribute) {
                continue;
            }

            var source = BuildPublishedModel(definition, documentTypeAttribute, context);
            if (source.Length == 0) {
                continue;
            }

            var hintName = $"{SanitizeHintName(definition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))}.PublishedModel.g.cs";
            context.AddSource(hintName, source);
        }
    }

    private static void EmitDataTypeAliases(
        SourceProductionContext context,
        ImmutableArray<INamedTypeSymbol> definitions) {
        foreach (var definition in definitions) {
            if (FindAttribute(definition, _dataTypeAttributeMetadataName) is not AttributeData dataTypeAttribute) {
                continue;
            }

            var namespaceName = definition.ContainingNamespace.IsGlobalNamespace
                ? null
                : definition.ContainingNamespace.ToDisplayString();
            var alias = GetAliasArgument(dataTypeAttribute, GetNameArgument(dataTypeAttribute));
            var builder = new IndentedStringBuilder();

            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("#nullable enable");
            builder.AppendLine();

            if (namespaceName is not null) {
                builder.Append("namespace ");
                builder.Append(namespaceName);
                builder.AppendLine(";");
                builder.AppendLine();
            }

            builder.Append("public partial class ");
            builder.Append(definition.Name);
            builder.AppendLine(" {");
            builder.Indent++;
            AppendGeneratedCodeAttribute(builder, fullyQualified: true);
            builder.Append("public const string Alias = ");
            builder.Append(SymbolDisplay.FormatLiteral(alias, true));
            builder.AppendLine(";");
            builder.Indent--;
            builder.AppendLine("}");

            var hintName = $"{SanitizeHintName(definition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))}.DataType.g.cs";
            context.AddSource(hintName, builder.ToString());
        }
    }

    private static string BuildPublishedModel(
        INamedTypeSymbol definition,
        AttributeData documentTypeAttribute,
        SourceProductionContext context) {
        var namespaceName = definition.ContainingNamespace.IsGlobalNamespace
            ? null
            : definition.ContainingNamespace.ToDisplayString();
        var name = GetNameArgument(documentTypeAttribute);
        var alias = GetAliasArgument(documentTypeAttribute, name);
        var isElement = IsAttribute(documentTypeAttribute, _elementTypeAttributeMetadataName);
        var builder = new IndentedStringBuilder();

        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("using System;");
        builder.AppendLine("using System.CodeDom.Compiler;");
        builder.AppendLine("using System.Diagnostics.CodeAnalysis;");
        builder.AppendLine("using System.Linq.Expressions;");
        builder.AppendLine("using Umbraco.Cms.Core;");
        builder.AppendLine("using Umbraco.Cms.Core.Models.PublishedContent;");
        builder.AppendLine("using Umbraco.Cms.Core.PublishedCache;");
        builder.AppendLine("using Umbraco.Cms.Infrastructure.ModelsBuilder;");
        builder.AppendLine();

        if (namespaceName is not null) {
            builder.Append("namespace ");
            builder.Append(namespaceName);
            builder.AppendLine(";");
            builder.AppendLine();
        }

        builder.Append("[PublishedModel(");
        builder.Append(SymbolDisplay.FormatLiteral(alias, true));
        builder.AppendLine(")]");
        builder.Append("public partial class ");
        builder.Append(definition.Name);
        builder.AppendLine(" : PublishedContentModel {");
        builder.Indent++;
        builder.AppendLine("#pragma warning disable 0109");
        AppendGeneratedCodeAttribute(builder);
        builder.Append("public new const string ModelTypeAlias = ");
        builder.Append(SymbolDisplay.FormatLiteral(alias, true));
        builder.AppendLine(";");
        builder.AppendLine();
        AppendGeneratedCodeAttribute(builder);
        builder.Append("public new const PublishedItemType ModelItemType = PublishedItemType.");
        builder.AppendLine(isElement ? "Element;" : "Content;");
        builder.AppendLine();
        AppendGeneratedCodeAttribute(builder);
        builder.AppendLine("[return: MaybeNull]");
        builder.AppendLine("public new static IPublishedContentType GetModelContentType(IPublishedContentTypeCache contentTypeCache)");
        builder.AppendLine("    => PublishedModelUtility.GetModelContentType(contentTypeCache, ModelItemType, ModelTypeAlias);");
        builder.AppendLine();
        AppendGeneratedCodeAttribute(builder);
        builder.AppendLine("[return: MaybeNull]");
        builder.Append("public static IPublishedPropertyType GetModelPropertyType<TValue>(IPublishedContentTypeCache contentTypeCache, ");
        builder.Append("Expression<Func<");
        builder.Append(definition.Name);
        builder.AppendLine(", TValue>> selector)");
        builder.AppendLine("    => PublishedModelUtility.GetModelPropertyType(GetModelContentType(contentTypeCache)!, selector);");
        builder.AppendLine();
        builder.AppendLine("#pragma warning restore 0109");
        builder.AppendLine();
        AppendGeneratedCodeAttribute(builder);
        builder.AppendLine("private readonly IPublishedValueFallback _publishedValueFallback;");
        builder.AppendLine();
        AppendGeneratedCodeAttribute(builder);
        builder.Append("public ");
        builder.Append(definition.Name);
        builder.AppendLine("(IPublishedContent content, IPublishedValueFallback publishedValueFallback)");
        builder.AppendLine("    : base(content, publishedValueFallback) {");
        builder.Indent++;
        builder.AppendLine("_publishedValueFallback = publishedValueFallback;");
        builder.Indent--;
        builder.AppendLine("}");
        builder.AppendLine();

        foreach (var property in definition.GetMembers().OfType<IPropertySymbol>()
                     .Where(static property => FindAttribute(property, _propertyAttributeMetadataName) is not null)
                     .OrderBy(static property => property.Name, StringComparer.Ordinal)) {
            if (!IsSupportedPublishedPropertyType(property.Type)) {
                context.ReportDiagnostic(Diagnostic.Create(
                    _unsupportedPropertyType,
                    property.Locations.FirstOrDefault(),
                    property.Name,
                    definition.Name,
                    property.Type.ToDisplayString()));
                continue;
            }

            var propertyAttribute = FindAttribute(property, _propertyAttributeMetadataName)!;
            var propertySyntax = FindAttributeSyntax(property, _propertyAttributeMetadataName);
            var (propertyName, propertyAlias) = GetPropertyMetadata(propertyAttribute, propertySyntax);
            var propertyType = property.Type.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
                    .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
            var descriptionAttribute = FindAttribute(property, _descriptionAttributeMetadataName);
            var summary = descriptionAttribute is null
                ? propertyName
                : $"{propertyName}: {GetStringArgument(descriptionAttribute, 0)}";

            builder.AppendLine();
            builder.AppendLine("/// <summary>");
            builder.Append("///   ");
            builder.AppendLine(summary);
            builder.AppendLine("/// </summary>");
            AppendGeneratedCodeAttribute(builder);
            builder.AppendLine("[MaybeNull]");
            builder.Append("[ImplementPropertyType(");
            builder.Append(SymbolDisplay.FormatLiteral(propertyAlias, true));
            builder.AppendLine(")]");
            builder.Append("public partial ");
            builder.Append(propertyType);
            builder.Append(" ");
            builder.Append(property.Name);
            builder.Append(" => this.Value<");
            builder.Append(propertyType);
            builder.Append(">(_publishedValueFallback, ");
            builder.Append(SymbolDisplay.FormatLiteral(propertyAlias, true));
            builder.AppendLine(");");
        }

        builder.Indent--;
        builder.AppendLine("}");
        return builder.ToString();
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

    private static bool IsSupportedPublishedPropertyType(ITypeSymbol type) =>
        type.TypeKind is not (TypeKind.Pointer or TypeKind.FunctionPointer)
        && type.SpecialType != SpecialType.System_Void;

    private static string SanitizeHintName(string value) {
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var character in value) {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.ToString();
    }

    private static IReadOnlyDictionary<string, string> BuildDataTypeAliasMap(
        ImmutableArray<INamedTypeSymbol> definitions) {
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var definition in definitions) {
            if (FindAttribute(definition, _dataTypeAttributeMetadataName) is not AttributeData dataTypeAttribute) {
                continue;
            }

            var alias = GetAliasArgument(dataTypeAttribute, GetNameArgument(dataTypeAttribute));
            aliases[definition.Name] = alias;
            aliases[definition.ToDisplayString()] = alias;
        }

        return aliases;
    }

    private static void AppendDefinition(
        IndentedStringBuilder builder,
        INamedTypeSymbol definition,
        Compilation compilation,
        IReadOnlyDictionary<string, string> dataTypeAliases,
        SourceProductionContext context) {
        var contentTypeAttribute = FindContentTypeAttribute(definition);
        if (contentTypeAttribute is not null) {
            var name = GetNameArgument(contentTypeAttribute);
            var alias = GetAliasArgument(contentTypeAttribute, name);
            var kind = IsAttribute(contentTypeAttribute, _mediaTypeAttributeMetadataName)
                ? "MediaType"
                : IsAttribute(contentTypeAttribute, _memberTypeAttributeMetadataName)
                    ? "MemberType"
                    : "DocumentType";
            builder.Append("new global::Shorthand.UmbracoCodeFirst.Metadata.CodeFirstDefinition(");
            builder.Append("global::Shorthand.UmbracoCodeFirst.Metadata.CodeFirstDefinitionKind.");
            builder.Append(kind);
            builder.Append(", ");
            builder.Append(
                SymbolDisplay.FormatLiteral(alias, true));
            builder.Append(", ");
            builder.Append(
                SymbolDisplay.FormatLiteral(name, true));
            builder.AppendLine(", null, [");
            builder.Indent++;

            foreach (var property in definition.GetMembers().OfType<IPropertySymbol>()) {
                var propertyAttribute = property.GetAttributes().FirstOrDefault(
                    attribute => IsAttribute(attribute, _propertyAttributeMetadataName));
                if (propertyAttribute is null) {
                    continue;
                }

                var propertySyntax = FindAttributeSyntax(property, _propertyAttributeMetadataName);
                var (propertyName, propertyAlias) = GetPropertyMetadata(propertyAttribute, propertySyntax);

                var descriptionAttribute = FindAttribute(property, _descriptionAttributeMetadataName);
                var groupAttribute = FindLayoutAttribute(property, _groupAttributeMetadataName);
                var groupMetadata = GetLayoutMetadata(groupAttribute, compilation);
                var tabAttribute = FindLayoutAttribute(property, _tabAttributeMetadataName);
                var tabMetadata = GetLayoutMetadata(tabAttribute, compilation);
                var validationAttribute = FindValidationAttribute(property);
                var validationMetadata = GetValidationMetadata(validationAttribute, compilation);
                builder.Append("new global::Shorthand.UmbracoCodeFirst.Metadata.CodeFirstPropertyDefinition(");
                builder.Append(
                    SymbolDisplay.FormatLiteral(propertyAlias, true));
                builder.Append(", ");
                builder.Append(
                    SymbolDisplay.FormatLiteral(propertyName, true));
                builder.Append(", ");
                builder.Append(
                    SymbolDisplay.FormatLiteral(
                        GetDataTypeArgument(propertyAttribute, dataTypeAliases, propertySyntax),
                        true));
                builder.Append(", ");
                builder.Append(GetConstructorStringLiteral(descriptionAttribute));
                builder.Append(", ");
                builder.Append(groupMetadata is null
                    ? "null"
                    : SymbolDisplay.FormatLiteral(groupMetadata.Alias, true));
                builder.Append(", ");
                builder.Append(groupMetadata is null
                    ? "null"
                    : SymbolDisplay.FormatLiteral(groupMetadata.Name, true));
                builder.Append(", ");
                builder.Append((groupMetadata?.SortOrder ?? 0).ToString());
                builder.Append(", ");
                builder.Append(tabMetadata is null
                    ? "null"
                    : SymbolDisplay.FormatLiteral(tabMetadata.Alias, true));
                builder.Append(", ");
                builder.Append(tabMetadata is null
                    ? "null"
                    : SymbolDisplay.FormatLiteral(tabMetadata.Name, true));
                builder.Append(", ");
                builder.Append((tabMetadata?.SortOrder ?? 0).ToString());
                builder.Append(", ");
                builder.Append(GetNamedIntArgument(propertyAttribute, "SortOrder").ToString());
                builder.Append(", ");
                builder.Append(GetNamedBoolArgument(propertyAttribute, "Mandatory").ToString().ToLowerInvariant());
                builder.Append(", ");
                builder.Append(validationMetadata is null
                    ? "null"
                    : SymbolDisplay.FormatLiteral(validationMetadata.Expression, true));
                builder.Append(", ");
                builder.Append(validationMetadata?.Message is null
                    ? "null"
                    : SymbolDisplay.FormatLiteral(validationMetadata.Message, true));
                builder.Append(", ");
                builder.Append(GetNamedBoolArgument(propertyAttribute, "LabelOnTop").ToString().ToLowerInvariant());
                builder.AppendLine("),");
            }

            builder.Indent--;
            builder.Append("], ");
            builder.Append(BuildTypeSettings(contentTypeAttribute, definition));
            builder.Append(", null, null, null, ");
            builder.Append(BuildCompositionAliases(definition));
            builder.Append(", ");
            builder.Append(BuildFolderPath(definition));
            builder.AppendLine("),");
            return;
        }

        var dataTypeAttribute = definition.GetAttributes().FirstOrDefault(
            attribute => IsAttribute(attribute, _dataTypeAttributeMetadataName));
        if (dataTypeAttribute is null) {
            return;
        }

        builder.Append("new global::Shorthand.UmbracoCodeFirst.Metadata.CodeFirstDefinition(");
        builder.Append("global::Shorthand.UmbracoCodeFirst.Metadata.CodeFirstDefinitionKind.DataType, ");
        builder.Append(
            SymbolDisplay.FormatLiteral(GetAliasArgument(dataTypeAttribute, GetNameArgument(dataTypeAttribute)), true));
        builder.Append(", ");
        builder.Append(
            SymbolDisplay.FormatLiteral(GetNameArgument(dataTypeAttribute), true));
        builder.Append(", ");
        builder.Append(
            SymbolDisplay.FormatLiteral(GetDataTypeEditorArgument(dataTypeAttribute), true));
        builder.Append(
            ", global::System.Array.Empty<global::Shorthand.UmbracoCodeFirst.Metadata.CodeFirstPropertyDefinition>(), null, null, ");
        builder.Append(GetConfigurationJsonLiteral(dataTypeAttribute, definition, context));
        builder.Append(", ");
        builder.Append(GetDataTypeEditorUiArgument(dataTypeAttribute));
        builder.Append(", null, ");
        builder.Append(BuildFolderPath(definition));
        builder.Append(", ");
        builder.Append(BuildEditorConfiguration(dataTypeAttribute, definition, context));
        builder.AppendLine("),");
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

        var alias = GetAliasArgument(attribute, name);
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

    private static string GetDataTypeArgument(
        AttributeData attribute,
        IReadOnlyDictionary<string, string> dataTypeAliases,
        AttributeSyntax? syntax = null) {
        var argumentIndex = attribute.ConstructorArguments.Length == 2 ? 1 : 2;
        if (attribute.ConstructorArguments.Length > argumentIndex) {
            var value = attribute.ConstructorArguments[argumentIndex].Value?.ToString();
            if (value is { Length: > 0 }) {
                return value;
            }
        }

        syntax ??= attribute.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
        if (syntax?.ArgumentList?.Arguments
                .Where(static argument => argument.NameColon is null && argument.NameEquals is null)
                .LastOrDefault()?.Expression is MemberAccessExpressionSyntax memberAccess
            && memberAccess.Name.Identifier.ValueText == "Alias") {
            var typeName = memberAccess.Expression.ToString();
            var simpleTypeName = typeName.Substring(typeName.LastIndexOf('.') + 1);
            if (dataTypeAliases.TryGetValue(typeName, out var alias)
                || dataTypeAliases.TryGetValue(simpleTypeName, out alias)) {
                return alias;
            }
        }

        return string.Empty;
    }

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

    private static string GetDataTypeEditorArgument(AttributeData attribute) =>
        HasDataTypeAliasProperty(attribute)
            ? attribute.ConstructorArguments[1].Kind == TypedConstantKind.Type
                ? GetEditorTypeValueArgument(attribute, 1, "Alias")
                : GetStringArgument(attribute, 1)
            : attribute.ConstructorArguments.Length == 2
                ? GetStringArgument(attribute, 1)
                : GetStringArgument(attribute, 2);

    private static string GetDataTypeEditorUiArgument(AttributeData attribute) =>
        HasDataTypeAliasProperty(attribute)
            ? attribute.ConstructorArguments[1].Kind == TypedConstantKind.Type
                ? SymbolDisplay.FormatLiteral(GetEditorTypeValueArgument(attribute, 1, "UiAlias"), true)
                : SymbolDisplay.FormatLiteral(GetStringArgument(attribute, 2), true)
            : GetNamedStringLiteral(attribute, "EditorUiAlias");

    private static string GetEditorTypeValueArgument(AttributeData attribute, int index, string memberName) {
        var editorType = (INamedTypeSymbol)attribute.ConstructorArguments[index].Value!;
        var member = editorType.GetMembers(memberName)
            .OfType<IFieldSymbol>()
            .Single(field => field.IsStatic && field.HasConstantValue);

        return (string)member.ConstantValue!;
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

    private static string GetConfigurationJsonLiteral(
        AttributeData attribute,
        INamedTypeSymbol definition,
        SourceProductionContext context) {
        if(GetConfigurationType(attribute) is { } typedConfigurationType) {
            var typedJson = BuildConfigurationJson(
                typedConfigurationType,
                attribute,
                definition,
                context);
            if(typedJson is not null) {
                return SymbolDisplay.FormatLiteral(typedJson, true);
            }

            return "null";
        }

        var jsonArgument = attribute.NamedArguments
            .FirstOrDefault(candidate => candidate.Key == "ConfigurationJson");
        if(jsonArgument.Key is not null && !jsonArgument.Value.IsNull) {
            return SymbolDisplay.FormatLiteral(
                jsonArgument.Value.Value?.ToString() ?? string.Empty,
                true);
        }

        return "null";
    }

    private static INamedTypeSymbol? GetConfigurationType(AttributeData attribute) {
        var namedArgument = attribute.NamedArguments
            .FirstOrDefault(candidate => candidate.Key == "ConfigurationType");
        if(namedArgument.Key is not null) {
            return namedArgument.Value.Value as INamedTypeSymbol;
        }

        return attribute.ConstructorArguments.Length >= 3
            && attribute.ConstructorArguments[1].Kind == TypedConstantKind.Type
            && attribute.ConstructorArguments[2].Kind == TypedConstantKind.Type
                ? attribute.ConstructorArguments[2].Value as INamedTypeSymbol
                : null;
    }

    private static string BuildEditorConfiguration(
        AttributeData attribute,
        INamedTypeSymbol definition,
        SourceProductionContext context) {
        if(GetConfigurationType(attribute) is not { } configurationType) {
            return "null";
        }

        if(IsConfigurationDefinition(configurationType)) {
            var runtimeEditorIdField = configurationType.GetMembers("EditorId")
                .OfType<IFieldSymbol>()
                .FirstOrDefault(field => field.IsStatic && field.HasConstantValue);
            var runtimeSchemaVersionField = configurationType.GetMembers("SchemaVersion")
                .OfType<IFieldSymbol>()
                .FirstOrDefault(field => field.IsStatic && field.HasConstantValue);
            var runtimeEditorId = runtimeEditorIdField?.ConstantValue as string ?? GetDataTypeEditorArgument(attribute);
            var runtimeSchemaVersion = runtimeSchemaVersionField?.ConstantValue is int runtimeVersion
                ? runtimeVersion
                : 1;
            return $"global::Shorthand.UmbracoCodeFirst.Metadata.CodeFirstEditorConfiguration.FromType(typeof({configurationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}), {SymbolDisplay.FormatLiteral(runtimeEditorId, true)}, {runtimeSchemaVersion})";
        }

        var json = BuildConfigurationJson(configurationType, attribute, definition, context);
        if(json is null) {
            context.ReportDiagnostic(Diagnostic.Create(
                _invalidConfigurationType,
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                configurationType.ToDisplayString(),
                GetNameArgument(attribute)));
            return "null";
        }

        var editorIdField = configurationType.GetMembers("EditorId")
            .OfType<IFieldSymbol>()
            .FirstOrDefault(field => field.IsStatic && field.HasConstantValue);
        var schemaVersionField = configurationType.GetMembers("SchemaVersion")
            .OfType<IFieldSymbol>()
            .FirstOrDefault(field => field.IsStatic && field.HasConstantValue);
        var editorId = editorIdField?.ConstantValue as string ?? GetDataTypeEditorArgument(attribute);
        var schemaVersion = schemaVersionField?.ConstantValue is int version ? version : 1;

        return $"new global::Shorthand.UmbracoCodeFirst.Metadata.CodeFirstEditorConfiguration({SymbolDisplay.FormatLiteral(editorId, true)}, {SymbolDisplay.FormatLiteral(json, true)}, {schemaVersion})";
    }

    private static bool IsConfigurationDefinition(INamedTypeSymbol type) {
        for(var current = type.BaseType; current is not null; current = current.BaseType) {
            if(string.Equals(
                current.ToDisplayString(),
                _configurationDefinitionMetadataName,
                StringComparison.Ordinal)) {
                return true;
            }
        }

        return false;
    }

    private static string? BuildConfigurationJson(
        INamedTypeSymbol configurationType,
        AttributeData attribute,
        INamedTypeSymbol definition,
        SourceProductionContext context) {
        var jsonField = configurationType.GetMembers("Json")
            .OfType<IFieldSymbol>()
            .FirstOrDefault(field => field.IsStatic && field.HasConstantValue);
        if(jsonField?.ConstantValue is string json) {
            return json;
        }

        var fields = configurationType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(field =>
                field.DeclaredAccessibility == Accessibility.Public
                && field.IsStatic
                && field.IsConst
                && field.Name is not "EditorId" and not "SchemaVersion")
            .OrderBy(field => field.Name, StringComparer.Ordinal)
            .ToArray();
        if(fields.Length == 0) {
            return null;
        }

        var values = new List<string>(fields.Length);
        foreach(var field in fields) {
            var value = FormatConfigurationValue(field, out var supported);
            if(!supported) {
                context.ReportDiagnostic(Diagnostic.Create(
                    _unsupportedConfigurationSetting,
                    field.Locations.FirstOrDefault(),
                    field.Name,
                    configurationType.ToDisplayString(),
                    field.Type.ToDisplayString()));
                return null;
            }

            values.Add($"{SymbolDisplay.FormatLiteral(ToCamelCase(field.Name), true)}:{value}");
        }

        return $"{{{string.Join(",", values)}}}";
    }

    private static string FormatConfigurationValue(IFieldSymbol field, out bool supported) {
        supported = true;
        if(field.Type.SpecialType == SpecialType.System_String) {
            return SymbolDisplay.FormatLiteral(field.ConstantValue?.ToString() ?? string.Empty, true);
        }

        if(field.Type.SpecialType == SpecialType.System_Boolean) {
            return ((bool)field.ConstantValue!).ToString().ToLowerInvariant();
        }

        if(field.Type.SpecialType is SpecialType.System_Byte
            or SpecialType.System_SByte
            or SpecialType.System_Int16
            or SpecialType.System_UInt16
            or SpecialType.System_Int32
            or SpecialType.System_UInt32
            or SpecialType.System_Int64
            or SpecialType.System_UInt64
            or SpecialType.System_Single
            or SpecialType.System_Double
            or SpecialType.System_Decimal) {
            return Convert.ToString(field.ConstantValue, CultureInfo.InvariantCulture)!;
        }

        supported = false;
        return string.Empty;
    }

    private static string ToCamelCase(string value) =>
        value.Length == 0
            ? value
            : char.ToLowerInvariant(value[0]) + value.Substring(1);

    private static int GetNamedIntArgument(AttributeData? attribute, string name) =>
        attribute?.NamedArguments.FirstOrDefault(argument => argument.Key == name).Value.Value is int value ? value : 0;

    private static bool GetNamedBoolArgument(AttributeData? attribute, string name) =>
        attribute?.NamedArguments.FirstOrDefault(argument => argument.Key == name).Value.Value is true;

    private static string BuildTypeSettings(AttributeData attribute, INamedTypeSymbol definition) {
        var allowedTemplates = BuildStringArray(attribute, "AllowedTemplateAliases");
        var allowedChildren = BuildStringArray(attribute, "AllowedChildAliases");
        var descriptionAttribute = FindAttribute(definition, _descriptionAttributeMetadataName);
        return $$"""
            new global::Shorthand.UmbracoCodeFirst.Metadata.CodeFirstTypeSettings(
                {{GetConstructorStringLiteral(descriptionAttribute)}},
                {{GetNamedStringLiteral(attribute, "Icon")}},
                {{IsAttribute(attribute, _elementTypeAttributeMetadataName).ToString().ToLowerInvariant()}},
                {{GetNamedBoolArgument(attribute, "AllowAsRoot").ToString().ToLowerInvariant()}},
                {{GetNamedBoolArgument(attribute, "AllowInLibrary").ToString().ToLowerInvariant()}},
                {{GetNamedStringLiteral(attribute, "DefaultTemplateAlias")}},
                {{allowedTemplates}},
                {{allowedChildren}},
                {{GetNamedStringLiteral(attribute, "Variations")}},
                {{GetNamedBoolArgument(attribute, "IsListView").ToString().ToLowerInvariant()}})
            """;
    }

    private static string BuildCompositionAliases(INamedTypeSymbol definition) {
        var aliases = definition.GetAttributes()
            .Where(attribute => IsAttribute(attribute, _compositeAttributeMetadataName))
            .Select(attribute => attribute.ConstructorArguments.FirstOrDefault())
            .Where(argument => argument.Kind == TypedConstantKind.Type
                               && argument.Value is INamedTypeSymbol)
            .Select(argument => {
                var target = (INamedTypeSymbol)argument.Value!;
                var contentTypeAttribute = FindDocumentTypeAttribute(target);
                var targetName = contentTypeAttribute is null
                    ? target.Name
                    : GetNameArgument(contentTypeAttribute);
                return contentTypeAttribute is null
                    ? ToAlias(targetName)
                    : GetAliasArgument(contentTypeAttribute, targetName);
            })
            .Distinct(StringComparer.Ordinal)
            .OrderBy(alias => alias, StringComparer.Ordinal)
            .ToArray();

        if (aliases.Length == 0) {
            return "global::System.Array.Empty<string>()";
        }

        return $"new global::System.String[] {{ {string.Join(", ", aliases.Select(alias => SymbolDisplay.FormatLiteral(alias, true)))} }}";
    }

    private static string BuildFolderPath(INamedTypeSymbol definition) {
        var attribute = FindAttribute(definition, _folderPathAttributeMetadataName);
        return attribute is null || attribute.ConstructorArguments.Length == 0
            ? "null"
            : SymbolDisplay.FormatLiteral(
                attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty,
                true);
    }

    private static string BuildStringArray(AttributeData attribute, string name) {
        var value = attribute.NamedArguments.FirstOrDefault(argument => argument.Key == name).Value;
        if(value.Kind != TypedConstantKind.Array) {
            return "global::System.Array.Empty<string>()";
        }

        var values = string.Join(", ", value.Values.Select(item => SymbolDisplay.FormatLiteral(item.Value?.ToString() ?? string.Empty, true)));
        return $"new global::System.String[] {{ {values} }}";
    }

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
