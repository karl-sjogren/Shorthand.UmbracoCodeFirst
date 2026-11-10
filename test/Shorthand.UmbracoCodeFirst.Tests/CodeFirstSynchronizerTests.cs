using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using System.Text.Json;

namespace Shorthand.UmbracoCodeFirst.Tests;

public class CodeFirstSynchronizerTests {
    [Fact]
    public async Task SynchronizeAsync_BlockDataType_PreparesThenResolvesElementDependency_Async() {
        var editor = new CodeFirstEditorConfiguration("Umbraco.BlockList", "{}") {
            BlockTypes = [new CodeFirstBlockType("element")],
        };
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "blocks", "Blocks", "Umbraco.BlockList", [],
                EditorConfiguration: editor),
            new(CodeFirstDefinitionKind.DocumentType, "element", "Element", null,
                [new("nested", "Nested", "blocks")], Settings: new(IsElement: true)),
        ]);
        var store = new BlockRecordingStore();
        var synchronizer = new CodeFirstSynchronizer(
            registry, store, new SynchronizationReportStore(), TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        store.Calls.ShouldBe(["prepare:blocks", "apply:element", "apply:blocks"]);
        report.FailedCount.ShouldBe(0);
        report.Items.Single(item => item.Alias == "blocks").Status.ShouldBe(SynchronizationStatus.Created);
    }

    [Fact]
    public async Task SynchronizeAsync_BlockReferenceMissing_ReportsFailureWithoutPreparing_Async() {
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "blocks", "Blocks", "Umbraco.BlockList", [],
                EditorConfiguration: new CodeFirstEditorConfiguration("Umbraco.BlockList", "{}") {
                    BlockTypes = [new CodeFirstBlockType("missing")],
                }),
        ]);
        var store = new BlockRecordingStore();
        var synchronizer = new CodeFirstSynchronizer(
            registry, store, new SynchronizationReportStore(), TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        store.Calls.ShouldBeEmpty();
        report.Items.Single().Status.ShouldBe(SynchronizationStatus.Failed);
        report.Items.Single().Message.ShouldNotBeNull().ShouldContain("missing");
    }

    [Fact]
    public async Task SynchronizeAsync_EmptyBlocks_AppliesWithoutPlaceholder_Async() {
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "blocks", "Blocks", "Umbraco.BlockList", [],
                EditorConfiguration: new CodeFirstEditorConfiguration("Umbraco.BlockList", "{}") { BlockTypes = [] }),
        ]);
        var store = new BlockRecordingStore();
        var synchronizer = new CodeFirstSynchronizer(
            registry, store, new SynchronizationReportStore(), TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        store.Calls.ShouldBe(["apply:blocks"]);
        report.FailedCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SynchronizeAsync_TypedAndRawBlocks_ReportsConflictBeforePreparing_Async(bool rawInDefinition) {
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "blocks", "Blocks", "Umbraco.BlockList", [],
                ConfigurationJson: rawInDefinition ? """{"blocks":[]}""" : null,
                EditorConfiguration: new CodeFirstEditorConfiguration(
                    "Umbraco.BlockList", rawInDefinition ? "{}" : """{"blocks":[]}""") {
                    BlockTypes = [],
                }),
        ]);
        var store = new BlockRecordingStore();
        var synchronizer = new CodeFirstSynchronizer(
            registry, store, new SynchronizationReportStore(), TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        store.Calls.ShouldBeEmpty();
        report.Items.Single().Status.ShouldBe(SynchronizationStatus.Failed);
        report.Items.Single().Message.ShouldNotBeNull().ShouldContain("both typed blocks and raw blocks");
    }

    [Fact]
    public async Task SynchronizeAsync_TypedAndRawSetting_ReportsConflictBeforeApplying_Async() {
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "blocks", "Blocks", "Umbraco.BlockList", [],
                ConfigurationJson: """{"validationLimit":{"max":2}}""",
                EditorConfiguration: new CodeFirstEditorConfiguration("Umbraco.BlockList", "{}") {
                    TypedSettings = new Dictionary<string, JsonElement> {
                        ["validationLimit"] = JsonSerializer.SerializeToElement(new { max = 4 }),
                    },
                }),
        ]);
        var store = new BlockRecordingStore();
        var synchronizer = new CodeFirstSynchronizer(
            registry, store, new SynchronizationReportStore(), TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        store.Calls.ShouldBeEmpty();
        report.Items.Single().Status.ShouldBe(SynchronizationStatus.Failed);
        report.Items.Single().Message.ShouldNotBeNull().ShouldContain("validationLimit");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SynchronizeAsync_BlockReferenceNotUniqueElement_FailsWithoutPreparing_Async(bool duplicate) {
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "blocks", "Blocks", "Umbraco.BlockList", [],
                EditorConfiguration: new CodeFirstEditorConfiguration("Umbraco.BlockList", "{}") {
                    BlockTypes = [new("element")],
                }),
            new(CodeFirstDefinitionKind.DocumentType, "element", "Element", null, [],
                Settings: new(IsElement: duplicate)),
            .. duplicate
                ? new[] { new CodeFirstDefinition(CodeFirstDefinitionKind.DocumentType, "element", "Duplicate", null, [], Settings: new(IsElement: true)) }
                : Array.Empty<CodeFirstDefinition>(),
        ]);
        var store = new BlockRecordingStore();
        var synchronizer = new CodeFirstSynchronizer(
            registry, store, new SynchronizationReportStore(), TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        store.Calls.ShouldNotContain("prepare:blocks");
        report.Items.Single(item => item.Alias == "blocks").Status.ShouldBe(SynchronizationStatus.Failed);
        report.Items.Single(item => item.Alias == "blocks").Message.ShouldNotBeNull().ShouldContain("element");
    }

    [Theory]
    [InlineData(SynchronizationStatus.Failed)]
    [InlineData(SynchronizationStatus.Skipped)]
    public async Task SynchronizeAsync_BlockElementNotApplied_DoesNotApplyBlocks_Async(SynchronizationStatus status) {
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "blocks", "Blocks", "Umbraco.BlockList", [],
                EditorConfiguration: new CodeFirstEditorConfiguration("Umbraco.BlockList", "{}") {
                    BlockTypes = [new("element")],
                }),
            new(CodeFirstDefinitionKind.DocumentType, "element", "Element", null, [], Settings: new(IsElement: true)),
        ]);
        var store = new BlockRecordingStore { ElementStatus = status };
        var synchronizer = new CodeFirstSynchronizer(
            registry, store, new SynchronizationReportStore(), TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        store.Calls.ShouldBe(["prepare:blocks", "apply:element"]);
        report.Items.Single(item => item.Alias == "blocks").Status.ShouldBe(SynchronizationStatus.Failed);
        report.Items.Single(item => item.Alias == "blocks").Message.ShouldNotBeNull().ShouldContain(status.ToString());
    }

    [Fact]
    public async Task SynchronizeAsync_Cancellation_PropagatesAsync() {
        var registry = new TestRegistry([new(CodeFirstDefinitionKind.DataType, "text", "Text", "Umbraco.TextBox", [])]);
        var synchronizer = new CodeFirstSynchronizer(
            registry, new CancellingStore(), new SynchronizationReportStore(), TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => synchronizer.SynchronizeAsync(cancellation.Token));
    }

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

    private sealed class BlockRecordingStore : ICodeFirstDefinitionStore, ICodeFirstBlockDefinitionStore {
        public List<string> Calls { get; } = [];
        public SynchronizationStatus ElementStatus { get; init; } = SynchronizationStatus.Updated;

        public Task<SynchronizationStatus> PrepareBlockDataTypeAsync(
            CodeFirstDefinition definition,
            CancellationToken cancellationToken) {
            Calls.Add($"prepare:{definition.Alias}");
            return Task.FromResult(SynchronizationStatus.Created);
        }

        public Task<SynchronizationStatus> ApplyAsync(
            CodeFirstDefinition definition,
            CancellationToken cancellationToken) {
            Calls.Add($"apply:{definition.Alias}");
            return Task.FromResult(definition.Alias == "element" ? ElementStatus : SynchronizationStatus.Updated);
        }
    }

    private sealed class CancellingStore : ICodeFirstDefinitionStore {
        public Task<SynchronizationStatus> ApplyAsync(CodeFirstDefinition definition, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(SynchronizationStatus.Unchanged);
        }
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
