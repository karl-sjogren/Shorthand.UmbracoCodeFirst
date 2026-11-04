using Microsoft.CodeAnalysis;

namespace Shorthand.UmbracoCodeFirst.Analyzers;

internal sealed class AliasOccurrence(
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

internal sealed class ValidationOccurrence(
    string expression,
    string message,
    INamedTypeSymbol containingType,
    Location? location) {
    public string Expression { get; } = expression;
    public string Message { get; } = message;
    public INamedTypeSymbol ContainingType { get; } = containingType;
    public Location? Location { get; } = location;
}

internal sealed class CompositionOccurrence(
    INamedTypeSymbol source,
    INamedTypeSymbol target,
    Location? location,
    bool targetHasDocumentType,
    bool sourceIsElement,
    bool targetIsElement) {
    public INamedTypeSymbol Source { get; } = source;
    public INamedTypeSymbol Target { get; } = target;
    public Location? Location { get; } = location;
    public bool TargetHasDocumentType { get; } = targetHasDocumentType;
    public bool SourceIsElement { get; } = sourceIsElement;
    public bool TargetIsElement { get; } = targetIsElement;
}

internal sealed class DataTypeOccurrence(string alias, string editorAlias, ISymbol symbol) {
    public string Alias { get; } = alias;
    public string EditorAlias { get; } = editorAlias;
    public ISymbol Symbol { get; } = symbol;
}

internal sealed class EditorPropertyOccurrence(IPropertySymbol property, string dataTypeAlias) {
    public IPropertySymbol Property { get; } = property;
    public string DataTypeAlias { get; } = dataTypeAlias;
}

internal enum AliasScope {
    Global,
    ContainingType,
}
