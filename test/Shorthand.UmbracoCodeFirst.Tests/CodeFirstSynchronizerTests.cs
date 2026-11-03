using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;

namespace Shorthand.UmbracoCodeFirst.Tests;

public class CodeFirstSynchronizerTests {
    [Fact]
    public async Task SynchronizeAsync_DataTypesBeforeDocumentTypes_AppliesDependenciesFirst_Async() {
        // Arrange
        var registry = new TestRegistry([
            new(
                CodeFirstDefinitionKind.DocumentType,
                "productPage",
                "Product Page",
                null,
                [new("price", "Price", "codeFirstDecimal")]),
            new(
                CodeFirstDefinitionKind.DataType,
                "codeFirstDecimal",
                "Code First Decimal",
                "Umbraco.Decimal",
                []),
        ]);
        var store = new RecordingStore();
        var reportStore = new SynchronizationReportStore();
        var synchronizer = new CodeFirstSynchronizer(
            registry,
            store,
            reportStore,
            TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        // Act
        await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        store.AppliedAliases.ShouldBe(["codeFirstDecimal", "productPage"]);
    }

    [Fact]
    public async Task SynchronizeAsync_ComposedDocumentTypes_AppliesCompositionFirst_Async() {
        // Arrange
        var registry = new TestRegistry([
            new(
                CodeFirstDefinitionKind.DocumentType,
                "productPage",
                "Product Page",
                null,
                [],
                CompositionAliases: ["article"]),
            new(
                CodeFirstDefinitionKind.DocumentType,
                "article",
                "Article",
                null,
                []),
        ]);
        var store = new RecordingStore();
        var reportStore = new SynchronizationReportStore();
        var synchronizer = new CodeFirstSynchronizer(
            registry,
            store,
            reportStore,
            TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        // Act
        await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        store.AppliedAliases.ShouldBe(["article", "productPage"]);
    }

    private sealed class TestRegistry(IReadOnlyList<CodeFirstDefinition> definitions)
        : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = definitions;
    }

    private sealed class RecordingStore : ICodeFirstDefinitionStore {
        public List<string> AppliedAliases { get; } = [];

        public Task<SynchronizationStatus> ApplyAsync(
            CodeFirstDefinition definition,
            CancellationToken cancellationToken) {
            AppliedAliases.Add(definition.Alias);
            return Task.FromResult(SynchronizationStatus.Created);
        }
    }
}
