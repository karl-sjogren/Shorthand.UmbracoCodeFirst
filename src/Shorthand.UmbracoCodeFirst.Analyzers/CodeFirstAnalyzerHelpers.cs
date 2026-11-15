using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

internal static class CodeFirstAnalyzerHelpers {
    public static bool HasDocumentTypeAttribute(ISymbol symbol) =>
        symbol.GetAttributes().Any(HasDocumentTypeAttribute);

    public static bool HasDocumentTypeAttribute(AttributeData attribute) =>
        attribute.AttributeClass?.Name is
            "UCFDocumentAttribute"
            or "UCFElementTypeAttribute"
            or "UCFMediaAttribute"
            or "UCFMemberAttribute"
            || attribute.AttributeClass?.BaseType?.Name == "UCFTypeAttribute";

    public static string GetScopeName(AliasOccurrence occurrence) =>
        occurrence.Symbol.ContainingType?.ToDisplayString() ?? string.Empty;

    public static string GetLayoutIdentity(AliasOccurrence occurrence, Compilation compilation) {
        if(occurrence.LayoutKind != "group") {
            return occurrence.Alias;
        }

        foreach(var attribute in occurrence.Symbol.GetAttributes()) {
            var type = attribute.AttributeClass;
            if(type?.Name == "UCFTabAttribute"
                && CodeFirstAttributeParser.TryGetLayout(attribute, occurrence.Symbol, out var tab)) {
                return $"{tab.Alias}/{occurrence.Alias}";
            }

            if(type?.BaseType?.Name != "UCFTabAttribute") {
                continue;
            }

            foreach(var syntax in type.DeclaringSyntaxReferences) {
                if(syntax.GetSyntax() is not ClassDeclarationSyntax declaration) {
                    continue;
                }

                var arguments = declaration.BaseList?.Types
                    .SelectMany(static baseType => baseType.ChildNodes().OfType<ArgumentListSyntax>())
                    .FirstOrDefault()
                    ?? declaration.Members.OfType<ConstructorDeclarationSyntax>()
                        .FirstOrDefault(static constructor => constructor.Initializer?.ThisOrBaseKeyword.Text == "base")
                        ?.Initializer?.ArgumentList;
                if(arguments is null) {
                    continue;
                }

                var model = compilation.GetSemanticModel(declaration.SyntaxTree);
                var strings = arguments.Arguments
                    .Select(argument => model.GetConstantValue(argument.Expression))
                    .Where(static value => value.HasValue && value.Value is string)
                    .Select(static value => (string)value.Value!)
                    .ToArray();
                if(strings.Length > 0) {
                    var alias = strings.Length > 1 ? strings[0] : ToAlias(strings[0]);
                    return $"{alias}/{occurrence.Alias}";
                }
            }

            return $"{type.ToDisplayString()}/{occurrence.Alias}";
        }

        return occurrence.Alias;
    }

    public static bool IsValidAlias(string alias) =>
        alias.All(static character => char.IsLetterOrDigit(character)
                                      || char.IsWhiteSpace(character)
                                      || character is '.' or '-'
                                      || IsCombiningMark(character));

    public static string ToAlias(string value) {
        var words = new List<string>();
        var word = new StringBuilder();

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
        return words.Count == 0
            ? string.Empty
            : string.Join(
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

    private static bool IsCombiningMark(char character) {
        var category = char.GetUnicodeCategory(character);
        return category is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.EnclosingMark;
    }
}
