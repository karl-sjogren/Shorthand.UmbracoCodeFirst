using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;

namespace Shorthand.UmbracoCodeFirst.Tests;

public class CodeFirstSynchronizerTests {
    [Fact]
    public async Task Synchronizes_data_types_before_document_types_Async() {
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

        await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        store.AppliedAliases.ShouldBe(["codeFirstDecimal", "productPage"]);
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
