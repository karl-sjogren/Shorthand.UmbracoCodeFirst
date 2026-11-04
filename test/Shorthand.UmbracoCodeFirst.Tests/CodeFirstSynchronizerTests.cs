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

    [Fact]
    public async Task SynchronizeAsync_WhenOneDefinitionFails_ContinuesAndReportsFailure_Async() {
        // Arrange
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "broken", "Broken", "Umbraco.TextBox", []),
            new(CodeFirstDefinitionKind.DataType, "valid", "Valid", "Umbraco.TextBox", []),
        ]);
        var store = new RecordingStore("broken");
        var reportStore = new SynchronizationReportStore();
        var synchronizer = new CodeFirstSynchronizer(
            registry,
            store,
            reportStore,
            TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        store.AppliedAliases.ShouldBe(["broken", "valid"]);
        report.FailedCount.ShouldBe(1);
        report.Items.Single(item => item.Alias == "broken")
            .ShouldSatisfyAllConditions(
                item => item.Status.ShouldBe(SynchronizationStatus.Failed),
                item => item.Message.ShouldBe("Could not apply 'broken'."));
        report.Items.Single(item => item.Alias == "valid")
            .Status
            .ShouldBe(SynchronizationStatus.Created);
        reportStore.Current.ShouldBe(report);
    }

    [Fact]
    public async Task SynchronizeAsync_CyclicCompositions_AppliesEachDefinitionOnce_Async() {
        // Arrange
        var registry = new TestRegistry([
            new(
                CodeFirstDefinitionKind.DocumentType,
                "article",
                "Article",
                null,
                [],
                CompositionAliases: ["page"]),
            new(
                CodeFirstDefinitionKind.DocumentType,
                "page",
                "Page",
                null,
                [],
                CompositionAliases: ["article"]),
        ]);
        var store = new RecordingStore();
        var synchronizer = new CodeFirstSynchronizer(
            registry,
            store,
            new SynchronizationReportStore(),
            TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        store.AppliedAliases.ShouldBe(["page", "article"]);
        report.FailedCount.ShouldBe(0);
    }

    private sealed class TestRegistry(IReadOnlyList<CodeFirstDefinition> definitions)
        : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = definitions;
    }

    private sealed class RecordingStore(string? failingAlias = null) : ICodeFirstDefinitionStore {
        public List<string> AppliedAliases { get; } = [];

        public Task<SynchronizationStatus> ApplyAsync(
            CodeFirstDefinition definition,
            CancellationToken cancellationToken) {
            AppliedAliases.Add(definition.Alias);
            if(definition.Alias == failingAlias) {
                throw new InvalidOperationException($"Could not apply '{definition.Alias}'.");
            }

            return Task.FromResult(SynchronizationStatus.Created);
        }
    }
}
