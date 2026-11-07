namespace Shorthand.UmbracoCodeFirst.Metadata;

public interface ICodeFirstDefinitionRegistry {
    IReadOnlyList<CodeFirstDefinition> Definitions { get; }
}
