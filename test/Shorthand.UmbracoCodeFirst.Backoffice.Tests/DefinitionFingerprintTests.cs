using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Tests;

public class DefinitionFingerprintTests {
    [Fact]
    public void Create_ChangesWhenTypedBlockConfigurationChanges() {
        // Arrange
        var first = Definition("First");
        var second = Definition("Second");

        // Act
        var firstHash = DefinitionFingerprint.Create([first]);
        var secondHash = DefinitionFingerprint.Create([second]);

        // Assert
        firstHash.ShouldNotBe(secondHash);
    }

    [Fact]
    public void Create_IsIndependentOfDefinitionOrder() {
        // Arrange
        var first = Definition("First");
        var second = first with { Alias = "other" };

        // Act
        var firstHash = DefinitionFingerprint.Create([first, second]);
        var secondHash = DefinitionFingerprint.Create([second, first]);

        // Assert
        firstHash.ShouldBe(secondHash);
    }

    private static CodeFirstDefinition Definition(string label) =>
        new(CodeFirstDefinitionKind.DataType, "blocks", "Blocks", "Umbraco.BlockList", [],
            EditorConfiguration: new CodeFirstEditorConfiguration("Umbraco.BlockList", "{}") {
                BlockTypes = [new("element") { Label = label }],
            });
}
