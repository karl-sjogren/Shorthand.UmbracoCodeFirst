using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Umbraco.Cms.Core.Services;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

[Trait("Category", "Integration")]
[Collection("Umbraco integration")]
public class BlockEditorIntegrationTests(UmbracoIntegrationFixture fixture) : IClassFixture<UmbracoIntegrationFixture> {
    private static readonly Guid _explicitAreaKey = Guid.Parse("99722293-22a3-4ba7-8fd5-a6fa56f3b4df");
    private static readonly Guid _explicitGroupKey = Guid.Parse("15593420-3744-4906-aea4-e1e84534cb0d");

    [Fact]
    public async Task SynchronizeAsync_AllBlockEditors_PersistKeysAndRemainStable_Async() {
        if(!fixture.IsEnabled) {
            return;
        }

        using var scope = fixture.Services.CreateScope();
        var registry = new TestRegistry([
            DataType("integrationBlockList", "Umbraco.BlockList", "Umb.PropertyEditorUi.BlockList"),
            DataType("integrationBlockGrid", "Umbraco.BlockGrid", "Umb.PropertyEditorUi.BlockGrid", grid: true),
            DataType("integrationSingleBlock", "Umbraco.SingleBlock", "Umb.PropertyEditorUi.BlockSingle"),
            DataType("integrationRichBlocks", "Umbraco.RichText", "Umb.PropertyEditorUi.Tiptap"),
            new(CodeFirstDefinitionKind.DocumentType, "integrationBlockElement", "Integration Block Element", null,
                [new("nestedBlocks", "Nested blocks", "integrationBlockList")], Settings: new(IsElement: true)),
            new(CodeFirstDefinitionKind.DocumentType, "integrationBlockSettings", "Integration Block Settings", null,
                [], Settings: new(IsElement: true)),
        ]);
        var synchronizer = new CodeFirstSynchronizer(
            registry,
            scope.ServiceProvider.GetRequiredService<ICodeFirstDefinitionStore>(),
            new SynchronizationReportStore(),
            TimeProvider.System,
            scope.ServiceProvider.GetRequiredService<ILogger<CodeFirstSynchronizer>>());

        var first = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        first.Items.Where(item => item.Status == SynchronizationStatus.Failed)
            .Select(item => $"{item.Alias}: {item.Message}").ShouldBeEmpty();
        var types = scope.ServiceProvider.GetRequiredService<IContentTypeService>();
        var elementKey = types.Get("integrationBlockElement")!.Key;
        var settingsKey = types.Get("integrationBlockSettings")!.Key;
        var dataTypes = scope.ServiceProvider.GetRequiredService<IDataTypeService>();
        var saved = (await dataTypes.GetAllAsync()).Where(dataType =>
            dataType.Name?.StartsWith("Integration Block ", StringComparison.Ordinal) == true).ToArray();
        saved.Length.ShouldBe(4);
        foreach(var dataType in saved) {
            var blocks = JsonSerializer.SerializeToElement(dataType.ConfigurationData["blocks"]);
            var block = blocks.EnumerateArray().Single();
            block.GetProperty("contentElementTypeKey").GetGuid().ShouldBe(elementKey);
            block.GetProperty("settingsElementTypeKey").GetGuid().ShouldBe(settingsKey);
            block.GetProperty("label").GetString().ShouldBe("Example block");
            block.GetProperty("iconColor").GetString().ShouldBe("#123456");
            dataTypes.ValidateConfigurationData(dataType).ShouldBeEmpty();
        }

        var grid = saved.Single(dataType => dataType.EditorAlias == "Umbraco.BlockGrid");
        var gridBlock = JsonSerializer.SerializeToElement(grid.ConfigurationData["blocks"])[0];
        var areaKey = gridBlock.GetProperty("areas")[0].GetProperty("key").GetGuid();
        var groupKey = JsonSerializer.SerializeToElement(grid.ConfigurationData["blockGroups"])[0]
            .GetProperty("key").GetGuid();
        gridBlock.GetProperty("groupKey").GetGuid().ShouldBe(groupKey);
        groupKey.ShouldBe(_explicitGroupKey);
        gridBlock.GetProperty("areas")[0].GetProperty("specifiedAllowance")[0]
            .GetProperty("elementTypeKey").GetGuid().ShouldBe(elementKey);
        var richText = saved.Single(dataType => dataType.EditorAlias == "Umbraco.RichText");
        JsonSerializer.SerializeToElement(richText.ConfigurationData["dimensions"])
            .GetProperty("height").GetInt32().ShouldBe(400);

        foreach(var dataType in saved) {
            dataType.ConfigurationData["manualSetting"] = "preserved";
            var result = await dataTypes.UpdateAsync(dataType, global::Umbraco.Cms.Core.Constants.Security.SuperUserKey);
            result.Success.ShouldBeTrue();
        }

        var newStore = ActivatorUtilities.CreateInstance<UmbracoCodeFirstDefinitionStore>(scope.ServiceProvider);
        var second = await new CodeFirstSynchronizer(
            registry, newStore, new SynchronizationReportStore(), TimeProvider.System,
            scope.ServiceProvider.GetRequiredService<ILogger<CodeFirstSynchronizer>>())
            .SynchronizeAsync(TestContext.Current.CancellationToken);

        second.Items.Where(item => item.Status == SynchronizationStatus.Failed)
            .Select(item => $"{item.Alias}: {item.Message}").ShouldBeEmpty();
        second.Items.ShouldAllBe(item => item.Status == SynchronizationStatus.Unchanged);
        var reloaded = (await dataTypes.GetAllAsync()).Single(dataType => dataType.EditorAlias == "Umbraco.BlockGrid"
            && dataType.Name == "Integration Block integrationBlockGrid");
        JsonSerializer.SerializeToElement(reloaded.ConfigurationData["blocks"])[0]
            .GetProperty("areas")[0].GetProperty("key").GetGuid().ShouldBe(areaKey);

        var updatedRegistry = new TestRegistry([
            DataType("integrationBlockList", "Umbraco.BlockList", "Umb.PropertyEditorUi.BlockList", updated: true),
            DataType("integrationBlockGrid", "Umbraco.BlockGrid", "Umb.PropertyEditorUi.BlockGrid", grid: true, updated: true),
            DataType("integrationSingleBlock", "Umbraco.SingleBlock", "Umb.PropertyEditorUi.BlockSingle", updated: true),
            DataType("integrationRichBlocks", "Umbraco.RichText", "Umb.PropertyEditorUi.Tiptap", updated: true),
            registry.Definitions[4],
            registry.Definitions[5],
        ]);
        var updated = await new CodeFirstSynchronizer(
            updatedRegistry, newStore, new SynchronizationReportStore(), TimeProvider.System,
            scope.ServiceProvider.GetRequiredService<ILogger<CodeFirstSynchronizer>>())
            .SynchronizeAsync(TestContext.Current.CancellationToken);

        updated.Items.Where(item => item.Status == SynchronizationStatus.Failed)
            .Select(item => $"{item.Alias}: {item.Message}").ShouldBeEmpty();
        updated.Items.Where(item => item.Kind == CodeFirstDefinitionKind.DataType)
            .ShouldAllBe(item => item.Status == SynchronizationStatus.Updated);
        var afterUpdate = (await dataTypes.GetAllAsync()).Where(dataType =>
            dataType.Name?.StartsWith("Integration Block ", StringComparison.Ordinal) == true).ToArray();
        foreach(var dataType in afterUpdate) {
            dataType.ConfigurationData["manualSetting"].ShouldBe("preserved");
            JsonSerializer.SerializeToElement(dataType.ConfigurationData["blocks"])[0]
                .GetProperty("label").GetString().ShouldBe("Updated block");
        }

        var updatedGrid = afterUpdate.Single(dataType => dataType.EditorAlias == "Umbraco.BlockGrid");
        var updatedAreas = JsonSerializer.SerializeToElement(updatedGrid.ConfigurationData["blocks"])[0].GetProperty("areas");
        updatedAreas[0].GetProperty("key").GetGuid().ShouldBe(_explicitAreaKey);
        updatedAreas[1].GetProperty("key").GetGuid().ShouldBe(areaKey);

        var savedBlocks = JsonSerializer.Serialize(updatedGrid.ConfigurationData["blocks"]);
        var badGrid = DataType("integrationBlockGrid", "Umbraco.BlockGrid", "Umb.PropertyEditorUi.BlockGrid", grid: true)
            with {
            EditorConfiguration = new CodeFirstEditorConfiguration("Umbraco.BlockGrid", "{}") {
                BlockTypes = [new("integrationBlockElement") { GroupAlias = "missing" }],
                BlockGroups = [new("layout", "Layout")],
            },
        };
        var invalidRegistry = new TestRegistry([badGrid, registry.Definitions[4], registry.Definitions[5]]);
        var invalid = await new CodeFirstSynchronizer(
            invalidRegistry, newStore, new SynchronizationReportStore(), TimeProvider.System,
            scope.ServiceProvider.GetRequiredService<ILogger<CodeFirstSynchronizer>>())
            .SynchronizeAsync(TestContext.Current.CancellationToken);
        invalid.Items.Single(item => item.Alias == badGrid.Alias).Status.ShouldBe(SynchronizationStatus.Failed);
        var preserved = (await dataTypes.GetAllAsync()).Single(dataType => dataType.Name == badGrid.Name);
        JsonSerializer.Serialize(preserved.ConfigurationData["blocks"]).ShouldBe(savedBlocks);
        preserved.ConfigurationData["manualSetting"].ShouldBe("preserved");

        var retry = await new CodeFirstSynchronizer(
            updatedRegistry, newStore, new SynchronizationReportStore(), TimeProvider.System,
            scope.ServiceProvider.GetRequiredService<ILogger<CodeFirstSynchronizer>>())
            .SynchronizeAsync(TestContext.Current.CancellationToken);
        retry.Items.ShouldAllBe(item => item.Status == SynchronizationStatus.Unchanged);

        var emptyList = DataType("integrationBlockList", "Umbraco.BlockList", "Umb.PropertyEditorUi.BlockList")
            with { EditorConfiguration = new CodeFirstEditorConfiguration("Umbraco.BlockList", "{}") { BlockTypes = [] } };
        var emptyRegistry = new TestRegistry([emptyList]);
        var clearSynchronizer = new CodeFirstSynchronizer(
            emptyRegistry, newStore, new SynchronizationReportStore(), TimeProvider.System,
            scope.ServiceProvider.GetRequiredService<ILogger<CodeFirstSynchronizer>>());
        var cleared = await clearSynchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
        cleared.Items.Single().Status.ShouldBe(SynchronizationStatus.Updated);
        var clearedList = (await dataTypes.GetAllAsync()).Single(dataType => dataType.Name == emptyList.Name);
        var clearedBlocks = JsonSerializer.SerializeToElement(clearedList.ConfigurationData["blocks"]);
        (clearedBlocks.ValueKind == JsonValueKind.Null
            || clearedBlocks.ValueKind == JsonValueKind.Array && clearedBlocks.GetArrayLength() == 0).ShouldBeTrue();
        var clearedAgain = await clearSynchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
        clearedAgain.Items.Single().Status.ShouldBe(SynchronizationStatus.Unchanged);

        var rawDefinition = new CodeFirstDefinition(
            CodeFirstDefinitionKind.DataType, "integrationRawBlocks", "Integration Raw Blocks", "Umbraco.BlockList", [],
            ConfigurationJson: """{"blocks":[],"useLiveEditing":true}""",
            EditorUiAlias: "Umb.PropertyEditorUi.BlockList");
        var rawReport = await new CodeFirstSynchronizer(
            new TestRegistry([rawDefinition]), newStore, new SynchronizationReportStore(), TimeProvider.System,
            scope.ServiceProvider.GetRequiredService<ILogger<CodeFirstSynchronizer>>())
            .SynchronizeAsync(TestContext.Current.CancellationToken);
        rawReport.FailedCount.ShouldBe(0);
        var rawSaved = (await dataTypes.GetAllAsync()).Single(dataType => dataType.Name == rawDefinition.Name);
        var rawBlocks = JsonSerializer.SerializeToElement(rawSaved.ConfigurationData["blocks"]);
        (rawBlocks.ValueKind == JsonValueKind.Null
            || rawBlocks.ValueKind == JsonValueKind.Array && rawBlocks.GetArrayLength() == 0).ShouldBeTrue();
        JsonSerializer.SerializeToElement(rawSaved.ConfigurationData["useLiveEditing"]).GetBoolean().ShouldBeTrue();
    }

    private static CodeFirstDefinition DataType(string alias, string editor, string ui, bool grid = false, bool updated = false) {
        var block = new CodeFirstBlockType("integrationBlockElement") {
            Settings = new("integrationBlockSettings"),
            Label = updated ? "Updated block" : "Example block",
            IconColor = "#123456",
        };
        if(grid) {
            block.AllowAtRoot = true;
            block.AllowInAreas = true;
            block.GroupAlias = "layout";
            var main = new CodeFirstBlockArea("main") {
                ColumnSpan = 6,
                SpecifiedAllowance = [new() { Element = new("integrationBlockElement"), GroupAlias = "layout" }],
            };
            block.Areas = updated ? [new("secondary") { Key = _explicitAreaKey }, main] : [main];
        }

        Dictionary<string, JsonElement>? typedSettings = editor switch {
            "Umbraco.BlockList" or "Umbraco.BlockGrid" => new() {
                ["validationLimit"] = JsonSerializer.SerializeToElement(new { min = 0, max = 10 }),
            },
            "Umbraco.RichText" => new() {
                ["dimensions"] = JsonSerializer.SerializeToElement(new { height = 400 }),
                ["toolbar"] = JsonSerializer.SerializeToElement(new[] { new[] { new[] { "Umb.Tiptap.Toolbar.Bold" } } }),
                ["statusbar"] = JsonSerializer.SerializeToElement(new[] { new[] { "Umb.Tiptap.Statusbar.WordCount" } }),
                ["stylesheets"] = JsonSerializer.SerializeToElement(new[] { "/editor.css" }),
            },
            _ => null,
        };
        return new(CodeFirstDefinitionKind.DataType, alias, $"Integration Block {alias}", editor, [],
            EditorUiAlias: ui,
            EditorConfiguration: new CodeFirstEditorConfiguration(editor, "{}") {
                BlockTypes = [block],
                BlockGroups = grid ? [new("layout", "Layout") { Key = _explicitGroupKey }] : null,
                TypedSettings = typedSettings,
            });
    }

    private sealed class TestRegistry(IReadOnlyList<CodeFirstDefinition> definitions) : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = definitions;
    }
}
