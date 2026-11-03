using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

public class GeneratedDefinitionsRegistryTests {
    [Fact]
    public void IntegrationRegistry_ContainsDependenciesAndLayout() {
        var registry = new IntegrationTestDefinitionRegistry();
        var definitions = registry.Definitions;

        definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.DataType)
            .Select(definition => definition.Alias)
            .ShouldContain("integrationText");
        definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.DocumentType)
            .Select(definition => definition.Alias)
            .ShouldContain("integrationComposedPage");

        var composedPage = definitions.Single(
            definition => definition.Alias == "integrationComposedPage");
        composedPage.Properties.ShouldContain(
            property => property.DataTypeAlias == "integrationText");
        composedPage.EffectiveTabs.Select(tab => tab.Alias).ShouldContain("page");
        (composedPage.CompositionAliases ?? [])
            .ShouldContain("integrationNestedComposition");
    }
}
