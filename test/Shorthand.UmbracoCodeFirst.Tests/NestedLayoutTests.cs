using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Tests;

public class NestedLayoutTests {
    [Fact]
    public void EffectiveGroups_ScopesGroupAliasesByTab() {
        // Arrange
        var definition = new CodeFirstDefinition(
            CodeFirstDefinitionKind.DocumentType, "page", "Page", null, [
                new("first", "First", "text", GroupAlias: "details", GroupName: "Details", TabAlias: "content"),
                new("second", "Second", "text", GroupAlias: "details", GroupName: "Details", TabAlias: "settings"),
                new("third", "Third", "text", GroupAlias: "details", GroupName: "Details"),
                new("fourth", "Fourth", "text", TabAlias: "content"),
            ]);

        // Act
        var groups = definition.EffectiveGroups;

        // Assert
        groups.Select(group => group.Alias).ShouldBe(["content/details", "details", "settings/details"]);
        definition.Properties.Select(property => property.EffectiveGroupAlias)
            .ShouldBe(["content/details", "settings/details", "details", "content"]);
    }

    [Fact]
    public void EffectiveGroups_PreservesExplicitGroupsAndAddsNestedGroups() {
        // Arrange
        var explicitGroups = new[] { new CodeFirstGroupDefinition("explicit", "Explicit", 1) };
        var definition = new CodeFirstDefinition(
            CodeFirstDefinitionKind.DocumentType, "page", "Page", null,
            [new("title", "Title", "text", GroupAlias: "details", TabAlias: "content")],
            Groups: explicitGroups);

        // Act & Assert
        definition.EffectiveGroups.Select(group => group.Alias).ShouldBe(["explicit", "content/details"]);
    }
}
