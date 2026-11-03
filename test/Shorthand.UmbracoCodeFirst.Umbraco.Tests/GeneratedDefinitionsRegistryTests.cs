using Shorthand.UmbracoCodeFirst.Generated;
using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

public class GeneratedDefinitionsRegistryTests {
    [Fact]
    public void GeneratedRegistry_SampleDefinitions_ContainsDependenciesAndLayout() {
        // Arrange
        GeneratedCodeFirstDefinitionRegistry registry = new();

        // Act
        var definitions = registry.Definitions;

        // Assert
        definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.DataType)
            .Select(definition => definition.Alias)
            .ShouldContain("codeFirstText");
        definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.DocumentType)
            .Select(definition => definition.Alias)
            .ShouldContain("productPage");

        var productPage = definitions.Single(definition => definition.Alias == "productPage");
        productPage.Properties.ShouldContain(property => property.DataTypeAlias == "codeFirstLongText");
        productPage.EffectiveTabs.Select(tab => tab.Alias).ShouldContain("marketing");
        productPage.EffectiveGroups.Select(group => group.Alias).ShouldContain("pricing");
        (productPage.CompositionAliases ?? []).ShouldContain("article");
    }
}
