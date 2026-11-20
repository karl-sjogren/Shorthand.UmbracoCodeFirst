namespace Shorthand.UmbracoCodeFirst.Metadata;

public sealed record CodeFirstGeneratedSource(string FileName, string Content) {
    public bool IsOriginal { get; init; }
}

public interface ICodeFirstGeneratedSourceCatalog {
    IReadOnlyList<CodeFirstGeneratedSource> GetSources(int definitionIndex);
}
