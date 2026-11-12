using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using System.Text.Json;

namespace Shorthand.UmbracoCodeFirst.Tests;

public class CodeFirstSynchronizerTests {
    [Fact]
    public async Task SynchronizeAsync_BlockDataType_PreparesThenResolvesElementDependency_Async() {
        // Arrange
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

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        store.Calls.ShouldBe(["prepare:blocks", "apply:element", "apply:blocks"]);
        report.FailedCount.ShouldBe(0);
        report.Items.Single(item => item.Alias == "blocks").Status.ShouldBe(SynchronizationStatus.Created);
    }

    [Fact]
    public async Task SynchronizeAsync_BlockReferenceMissing_ReportsFailureWithoutPreparing_Async() {
        // Arrange
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

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        store.Calls.ShouldBeEmpty();
        report.Items.Single().Status.ShouldBe(SynchronizationStatus.Failed);
        report.Items.Single().Message.ShouldNotBeNull().ShouldContain("missing");
    }

    [Fact]
    public async Task SynchronizeAsync_EmptyBlocks_AppliesWithoutPlaceholder_Async() {
        // Arrange
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "blocks", "Blocks", "Umbraco.BlockList", [],
                EditorConfiguration: new CodeFirstEditorConfiguration("Umbraco.BlockList", "{}") { BlockTypes = [] }),
        ]);
        var store = new BlockRecordingStore();
        var synchronizer = new CodeFirstSynchronizer(
            registry, store, new SynchronizationReportStore(), TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        store.Calls.ShouldBe(["apply:blocks"]);
        report.FailedCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("Managed externally.", "Managed externally.")]
    [InlineData(null, "The definition store skipped this definition without providing a reason.")]
    [InlineData(" ", "The definition store skipped this definition without providing a reason.")]
    public async Task SynchronizeAsync_SkippedStoreResult_ReportsReason_Async(string? message, string expected) {
        // Arrange
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DocumentType, "article", "Article", null, []),
        ]);
        var synchronizer = new CodeFirstSynchronizer(
            registry, new ResultStore(new(SynchronizationStatus.Skipped, message)),
            new SynchronizationReportStore(), TimeProvider.System, NullLogger<CodeFirstSynchronizer>.Instance);

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        report.Items.Single().Message.ShouldBe(expected);
        report.SkippedCount.ShouldBe(1);
    }

    [Theory]
    [InlineData("direct")]
    [InlineData("prepare")]
    [InlineData("deferred")]
    public async Task SynchronizeAsync_SkippedBlockResult_ReportsReason_Async(string stage) {
        // Arrange
        var isDirect = stage == "direct";
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "blocks", "Blocks", "Umbraco.BlockList", [],
                EditorConfiguration: new CodeFirstEditorConfiguration("Umbraco.BlockList", "{}") {
                    BlockTypes = isDirect ? [] : [new CodeFirstBlockType("element")],
                }),
            .. isDirect ? Array.Empty<CodeFirstDefinition>() : [
                new(CodeFirstDefinitionKind.DocumentType, "element", "Element", null, [], Settings: new(IsElement: true)),
            ],
        ]);
        var skipped = new SynchronizationResult(SynchronizationStatus.Skipped, $"Skipped during {stage}.");
        var store = new BlockRecordingStore {
            PrepareResult = stage == "prepare" ? skipped : new(SynchronizationStatus.Created),
            BlockResult = stage == "prepare" ? new(SynchronizationStatus.Updated) : skipped,
        };
        var synchronizer = new CodeFirstSynchronizer(
            registry, store, new SynchronizationReportStore(), TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        var item = report.Items.Single(item => item.Alias == "blocks");
        // Assert
        item.Status.ShouldBe(SynchronizationStatus.Skipped);
        item.Message.ShouldBe($"Skipped during {stage}.");
        if(stage == "prepare") {
            store.Calls.ShouldNotContain("apply:blocks");
        }
    }

    [Theory]
    [InlineData(SynchronizationStatus.Created, SynchronizationStatus.Created)]
    [InlineData(SynchronizationStatus.Unchanged, SynchronizationStatus.Updated)]
    public async Task SynchronizeAsync_BlockApplied_PreservesPreparationMessage_Async(
        SynchronizationStatus preparedStatus,
        SynchronizationStatus expectedStatus) {
        // Arrange
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "blocks", "Blocks", "Umbraco.BlockList", [],
                EditorConfiguration: new CodeFirstEditorConfiguration("Umbraco.BlockList", "{}") {
                    BlockTypes = [new CodeFirstBlockType("element")],
                }),
            new(CodeFirstDefinitionKind.DocumentType, "element", "Element", null, [], Settings: new(IsElement: true)),
        ]);
        var store = new BlockRecordingStore {
            PrepareResult = new(preparedStatus, "Preparation completed."),
        };
        var synchronizer = new CodeFirstSynchronizer(
            registry, store, new SynchronizationReportStore(), TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        var item = report.Items.Single(item => item.Alias == "blocks");
        // Assert
        item.Status.ShouldBe(expectedStatus);
        item.Message.ShouldBe("Preparation completed.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SynchronizeAsync_TypedAndRawBlocks_ReportsConflictBeforePreparing_Async(bool rawInDefinition) {
        // Arrange
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

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        store.Calls.ShouldBeEmpty();
        report.Items.Single().Status.ShouldBe(SynchronizationStatus.Failed);
        report.Items.Single().Message.ShouldNotBeNull().ShouldContain("both typed blocks and raw blocks");
    }

    [Fact]
    public async Task SynchronizeAsync_TypedAndRawSetting_ReportsConflictBeforeApplying_Async() {
        // Arrange
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

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        store.Calls.ShouldBeEmpty();
        report.Items.Single().Status.ShouldBe(SynchronizationStatus.Failed);
        report.Items.Single().Message.ShouldNotBeNull().ShouldContain("validationLimit");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SynchronizeAsync_BlockReferenceNotUniqueElement_FailsWithoutPreparing_Async(bool duplicate) {
        // Arrange
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

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        store.Calls.ShouldNotContain("prepare:blocks");
        report.Items.Single(item => item.Alias == "blocks").Status.ShouldBe(SynchronizationStatus.Failed);
        report.Items.Single(item => item.Alias == "blocks").Message.ShouldNotBeNull().ShouldContain("element");
    }

    [Theory]
    [InlineData(SynchronizationStatus.Failed)]
    [InlineData(SynchronizationStatus.Skipped)]
    public async Task SynchronizeAsync_BlockElementNotApplied_DoesNotApplyBlocks_Async(SynchronizationStatus status) {
        // Arrange
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

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        store.Calls.ShouldBe(["prepare:blocks", "apply:element"]);
        report.Items.Single(item => item.Alias == "blocks").Status.ShouldBe(SynchronizationStatus.Failed);
        report.Items.Single(item => item.Alias == "blocks").Message.ShouldNotBeNull().ShouldContain(status.ToString());
    }

    [Fact]
    public async Task SynchronizeAsync_Cancellation_PropagatesAsync() {
        // Arrange
        var registry = new TestRegistry([new(CodeFirstDefinitionKind.DataType, "text", "Text", "Umbraco.TextBox", [])]);
        var synchronizer = new CodeFirstSynchronizer(
            registry, new CancellingStore(), new SynchronizationReportStore(), TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act & Assert
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
    public async Task SynchronizeAsync_DuplicateDataTypeAliases_FailsBothWithoutApplyingEither_Async() {
        // Arrange
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "codeEditor", "C# editor", "Umbraco.Plain.String", []),
            new(CodeFirstDefinitionKind.DataType, "codeEditor", "JavaScript editor", "Umbraco.Plain.String", []),
            new(CodeFirstDefinitionKind.DataType, "other", "Other", "Umbraco.TextBox", []),
            new(CodeFirstDefinitionKind.DocumentType, "codeEditor", "Document", null, []),
        ]);
        var store = new RecordingStore();
        var synchronizer = new CodeFirstSynchronizer(
            registry, store, new SynchronizationReportStore(), TimeProvider.System,
            NullLogger<CodeFirstSynchronizer>.Instance);

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        store.AppliedAliases.ShouldBe(["other", "codeEditor"]);
        report.FailedCount.ShouldBe(2);
        foreach(var item in report.Items.Where(item => item.Kind == CodeFirstDefinitionKind.DataType && item.Alias == "codeEditor")) {
            item.Status.ShouldBe(SynchronizationStatus.Failed);
            item.Message.ShouldNotBeNull().ShouldContain("set its Alias explicitly");
        }

        report.Items.Single(item => item.Kind == CodeFirstDefinitionKind.DocumentType).Status
            .ShouldBe(SynchronizationStatus.Created);
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
        // Arrange
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

        // Act
        await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        store.AppliedAliases.ShouldBe(["text", "document", "media", "member"]);
    }

    [Fact]
    public async Task SynchronizeAsync_ReportsAllStatusCategories_AndAggregatesCounts_Async() {
        // Arrange
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

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
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
        public SynchronizationResult PrepareResult { get; init; } = new(SynchronizationStatus.Created);
        public SynchronizationResult BlockResult { get; init; } = new(SynchronizationStatus.Updated);

        public Task<SynchronizationResult> PrepareBlockDataTypeAsync(
            CodeFirstDefinition definition,
            CancellationToken cancellationToken) {
            Calls.Add($"prepare:{definition.Alias}");
            return Task.FromResult(PrepareResult);
        }

        public Task<SynchronizationResult> ApplyAsync(
            CodeFirstDefinition definition,
            CancellationToken cancellationToken) {
            Calls.Add($"apply:{definition.Alias}");
            return Task.FromResult(definition.Alias == "element" ? new SynchronizationResult(ElementStatus) : BlockResult);
        }
    }

    private sealed class CancellingStore : ICodeFirstDefinitionStore {
        public Task<SynchronizationResult> ApplyAsync(CodeFirstDefinition definition, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new SynchronizationResult(SynchronizationStatus.Unchanged));
        }
    }

    private sealed class RecordingStore(string? failingAlias = null) : ICodeFirstDefinitionStore {
        public List<string> AppliedAliases { get; } = [];

        public Task<SynchronizationResult> ApplyAsync(
            CodeFirstDefinition definition,
            CancellationToken cancellationToken) {
            AppliedAliases.Add(definition.Alias);
            if(definition.Alias == failingAlias) {
                throw new InvalidOperationException($"Could not apply '{definition.Alias}'.");
            }

            return Task.FromResult(new SynchronizationResult(SynchronizationStatus.Created));
        }
    }

    private sealed class StatusStore(IReadOnlyDictionary<string, SynchronizationStatus> statuses)
        : ICodeFirstDefinitionStore {
        public Task<SynchronizationResult> ApplyAsync(
            CodeFirstDefinition definition,
            CancellationToken cancellationToken) =>
            Task.FromResult(new SynchronizationResult(statuses[definition.Alias]));
    }

    private sealed class ResultStore(SynchronizationResult result) : ICodeFirstDefinitionStore {
        public Task<SynchronizationResult> ApplyAsync(
            CodeFirstDefinition definition,
            CancellationToken cancellationToken) => Task.FromResult(result);
    }
}
