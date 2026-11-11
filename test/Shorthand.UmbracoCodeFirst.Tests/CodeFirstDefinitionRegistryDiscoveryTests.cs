using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Tests;

public class CodeFirstDefinitionRegistryDiscoveryTests {
    [Fact]
    public void Discover_PrefersGeneratedRegistryOverOtherImplementations() {
        // Arrange
        // Act
        var registry = CodeFirstDefinitionRegistryDiscovery.Discover();

        // Assert
        registry.ShouldBeOfType<GeneratedCodeFirstDefinitionRegistry>();
        registry.Definitions.Select(definition => definition.Alias)
            .ShouldBe(["generatedRegistryDefinition"]);
    }

    public sealed class GeneratedCodeFirstDefinitionRegistry : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = [
            new(
                CodeFirstDefinitionKind.DocumentType,
                "generatedRegistryDefinition",
                "Generated Registry Definition",
                null,
                []),
        ];
    }

    public sealed class OtherRegistry : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = [
            new(
                CodeFirstDefinitionKind.DocumentType,
                "otherRegistryDefinition",
                "Other Registry Definition",
                null,
                []),
        ];
    }
}
