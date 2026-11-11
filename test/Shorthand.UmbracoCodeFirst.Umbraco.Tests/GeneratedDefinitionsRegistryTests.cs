using System.Text.Json;
using Shorthand.UmbracoCodeFirst.Generated;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

public class GeneratedDefinitionsRegistryTests {
    [Fact]
    public void AppRegistry_ConfiguredInteger_ContainsEditorAliasesAndSettings() {
        // Arrange
        var registry = new GeneratedCodeFirstDefinitionRegistry();
        // Act
        var definition = registry.Definitions.Single(definition => definition.Alias == "codeFirstInteger");

        // Assert
        definition.EditorAlias.ShouldBe("Umbraco.Integer");
        definition.EditorUiAlias.ShouldBe("Umb.PropertyEditorUi.Integer");
        definition.EditorConfiguration.ShouldNotBeNull();
        definition.EditorConfiguration.EditorId.ShouldBe("Umbraco.Integer");
        definition.EffectiveConfigurationJson.ShouldBe("{\"max\":1000000,\"min\":0}");
    }

    [Fact]
    public void AppRegistry_CodeEditor_UsesPlainStringSchemaAndNumericHeight() {
        // Arrange
        var registry = new GeneratedCodeFirstDefinitionRegistry();

        // Act
        var definition = registry.Definitions.Single(definition => definition.Name == "Code Editor");

        // Assert
        definition.EditorAlias.ShouldBe("Umbraco.Plain.String");
        definition.EditorUiAlias.ShouldBe("Umb.PropertyEditorUi.CodeEditor");
        definition.EffectiveConfigurationJson.ShouldBe("{\"height\":600,\"lineNumbers\":true}");
    }

    [Fact]
    public void AppRegistry_RichText_ConfiguresToolbarAndFormattingExtensions() {
        // Arrange
        var registry = new GeneratedCodeFirstDefinitionRegistry();

        // Act
        var definition = registry.Definitions.Single(definition => definition.Name == "Rich Text");

        // Assert
        definition.EditorAlias.ShouldBe("Umbraco.RichText");
        definition.EditorUiAlias.ShouldBe("Umb.PropertyEditorUi.Tiptap");
        var configuration = definition.EditorConfiguration.ShouldNotBeNull();
        using var json = JsonDocument.Parse(configuration.Json);
        var extensions = json.RootElement.GetProperty("extensions").EnumerateArray()
            .Select(item => item.GetString()).ToArray();
        extensions.ShouldContain("Umb.Tiptap.Bold");
        extensions.ShouldContain("Umb.Tiptap.Italic");
        var toolbar = configuration.TypedSettings.ShouldNotBeNull()["toolbar"];
        toolbar[0].EnumerateArray()
            .SelectMany(group => group.EnumerateArray().Select(item => item.GetString()))
            .ShouldContain("Umb.Tiptap.Toolbar.Bold");
    }

    [Fact]
    public void IntegrationRegistry_ContainsDependenciesAndLayout() {
        // Arrange
        var registry = new IntegrationTestDefinitionRegistry();
        // Act
        var definitions = registry.Definitions;

        // Assert
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
