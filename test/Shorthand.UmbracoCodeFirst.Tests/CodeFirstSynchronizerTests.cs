using Shorthand.UmbracoCodeFirst.Contracts;
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
    public async Task SynchronizeAsync_AllowedChildTypes_AppliesChildrenFirst_Async() {
        // Arrange
        var registry = new TestRegistry([
            new(
                CodeFirstDefinitionKind.DocumentType,
                "folder",
                "Folder",
                null,
                [],
                Settings: new(AllowedChildAliases: ["article"])),
            new(
                CodeFirstDefinitionKind.DocumentType,
                "article",
                "Article",
                null,
                []),
        ]);
        var store = new RecordingStore();
        var synchronizer = new CodeFirstSynchronizer(
            registry,
            store,
            new SynchronizationReportStore(),
            TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        // Act
        await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        store.AppliedAliases.ShouldBe(["article", "folder"]);
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

    [Fact]
    public async Task SynchronizeAsync_MediaAndMemberTypes_AreOrderedDeterministically_Async() {
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.MemberType, "member", "Member", null, []),
            new(CodeFirstDefinitionKind.MediaType, "media", "Media", null, []),
            new(CodeFirstDefinitionKind.DocumentType, "document", "Document", null, []),
            new(CodeFirstDefinitionKind.DataType, "text", "Text", "Umbraco.TextBox", []),
        ]);
        var store = new RecordingStore();
        var synchronizer = new CodeFirstSynchronizer(
            registry,
            store,
            new SynchronizationReportStore(),
            TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        store.AppliedAliases.ShouldBe(["text", "document", "media", "member"]);
    }

    [Fact]
    public async Task SynchronizeAsync_ReportsAllStatusCategories_AndAggregatesCounts_Async() {
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "created", "Created", "Umbraco.TextBox", []),
            new(CodeFirstDefinitionKind.DataType, "updated", "Updated", "Umbraco.TextBox", []),
            new(CodeFirstDefinitionKind.DataType, "unchanged", "Unchanged", "Umbraco.TextBox", []),
            new(CodeFirstDefinitionKind.DataType, "skipped", "Skipped", "Umbraco.TextBox", []),
            new(CodeFirstDefinitionKind.DataType, "failed", "Failed", "Umbraco.TextBox", []),
        ]);
        var store = new StatusStore(new Dictionary<string, SynchronizationStatus> {
            ["created"] = SynchronizationStatus.Created,
            ["updated"] = SynchronizationStatus.Updated,
            ["unchanged"] = SynchronizationStatus.Unchanged,
            ["skipped"] = SynchronizationStatus.Skipped,
            ["failed"] = SynchronizationStatus.Failed,
        });
        var synchronizer = new CodeFirstSynchronizer(
            registry,
            store,
            new SynchronizationReportStore(),
            TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        report.CreatedCount.ShouldBe(1);
        report.UpdatedCount.ShouldBe(1);
        report.UnchangedCount.ShouldBe(1);
        report.SkippedCount.ShouldBe(1);
        report.FailedCount.ShouldBe(1);
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

    private sealed class StatusStore(IReadOnlyDictionary<string, SynchronizationStatus> statuses)
        : ICodeFirstDefinitionStore {
        public Task<SynchronizationStatus> ApplyAsync(
            CodeFirstDefinition definition,
            CancellationToken cancellationToken) =>
            Task.FromResult(statuses[definition.Alias]);
    }
}
