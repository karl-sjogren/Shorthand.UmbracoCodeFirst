using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

internal static class CodeFirstAnalyzerHelpers {
    public static bool HasDocumentTypeAttribute(ISymbol symbol) =>
        symbol.GetAttributes().Any(HasDocumentTypeAttribute);

    public static bool HasDocumentTypeAttribute(AttributeData attribute) =>
        attribute.AttributeClass?.Name is
            "UCFDocumentTypeAttribute"
            or "UCFElementTypeAttribute"
            or "UCFMediaTypeAttribute"
            or "UCFMemberTypeAttribute"
            || attribute.AttributeClass?.BaseType?.Name == "UCFTypeAttribute";

    public static string GetScopeName(AliasOccurrence occurrence) =>
        occurrence.Symbol.ContainingType?.ToDisplayString() ?? string.Empty;

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
