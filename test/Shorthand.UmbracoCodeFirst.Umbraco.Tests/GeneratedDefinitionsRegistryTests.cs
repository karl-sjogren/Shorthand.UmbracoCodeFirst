using Shorthand.UmbracoCodeFirst.Generated;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

public class GeneratedDefinitionsRegistryTests {
    [Fact]
    public void AppRegistry_ConfiguredInteger_ContainsEditorAliasesAndSettings() {
        var registry = new GeneratedCodeFirstDefinitionRegistry();
        var definition = registry.Definitions.Single(definition => definition.Alias == "codeFirstInteger");

        definition.EditorAlias.ShouldBe("Umbraco.Integer");
        definition.EditorUiAlias.ShouldBe("Umb.PropertyEditorUi.Integer");
        definition.EditorConfiguration.ShouldNotBeNull();
        definition.EditorConfiguration.EditorId.ShouldBe("Umbraco.Integer");
        definition.EffectiveConfigurationJson.ShouldBe("{\"max\":1000000,\"min\":0}");
    }

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
