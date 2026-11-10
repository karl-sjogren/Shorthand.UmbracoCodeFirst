using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

public class UmbracoCodeFirstDefinitionStoreConfigurationTests {
    [Fact]
    public async Task ApplyAsync_FailedBlockUpdate_RestoresInMemoryConfiguration_Async() {
        var dataType = A.Fake<IDataType>();
        dataType.Name = "Existing";
        dataType.ParentId = -1;
        dataType.EditorUiAlias = "Umb.PropertyEditorUi.BlockList";
        dataType.ConfigurationData = new Dictionary<string, object> {
            ["shorthand.umbraco-codefirst"] = "blocks",
            ["blocks"] = new[] { "previous" },
            ["manualSetting"] = "preserved",
        };
        var dataTypeService = A.Fake<IDataTypeService>();
        A.CallTo(() => dataTypeService.GetAllAsync())
            .Returns(Task.FromResult<IEnumerable<IDataType>>([dataType]));
        A.CallTo(() => dataTypeService.UpdateAsync(dataType, A<Guid>._))
            .Throws(new InvalidOperationException("Update failed."));
        var store = new UmbracoCodeFirstDefinitionStore(
            dataTypeService, null!, null!, null!, null!, null!, null!, null!, null!,
            null!, null!, null!, null!, NullLogger<UmbracoCodeFirstDefinitionStore>.Instance);
        var definition = new CodeFirstDefinition(
            CodeFirstDefinitionKind.DataType, "blocks", "Changed", "Umbraco.BlockList", [],
            EditorUiAlias: "Umb.PropertyEditorUi.BlockList",
            EditorConfiguration: new CodeFirstEditorConfiguration("Umbraco.BlockList", "{}") { BlockTypes = [] });

        await Should.ThrowAsync<InvalidOperationException>(() => store.ApplyAsync(definition, CancellationToken.None));

        dataType.Name.ShouldBe("Existing");
        dataType.ConfigurationData["blocks"].ShouldBeOfType<string[]>().ShouldBe(["previous"]);
        dataType.ConfigurationData["manualSetting"].ShouldBe("preserved");
    }

    [Theory]
    [InlineData("""{"items":["csharp","javascript"],"other":[1,"two"],"nested":{"items":["a","b"]}}""")]
    [InlineData("""{"items":[]}""")]
    public async Task ApplyAsync_DropdownItems_AreValidStringLists_Async(string json) {
        var dataType = A.Fake<IDataType>();
        var configuration = new Dictionary<string, object> {
            ["shorthand.umbraco-codefirst"] = "codingLanguage",
        };
        A.CallTo(() => dataType.ConfigurationData).Returns(configuration);
        A.CallTo(() => dataType.Name).Returns("Coding language");
        A.CallTo(() => dataType.ParentId).Returns(-1);
        A.CallTo(() => dataType.EditorUiAlias).Returns("Umb.PropertyEditorUi.Dropdown");
        var dataTypeService = A.Fake<IDataTypeService>();
        A.CallTo(() => dataTypeService.GetAllAsync())
            .Returns(Task.FromResult<IEnumerable<IDataType>>([dataType]));
        A.CallTo(() => dataTypeService.UpdateAsync(dataType, A<Guid>._))
            .Returns(Attempt.SucceedWithStatus(DataTypeOperationStatus.Success, dataType));
        var store = new UmbracoCodeFirstDefinitionStore(
            dataTypeService, null!, null!, null!, null!, null!, null!, null!, null!,
            null!, null!, null!, null!, NullLogger<UmbracoCodeFirstDefinitionStore>.Instance);
        var definition = new CodeFirstDefinition(
            CodeFirstDefinitionKind.DataType,
            "codingLanguage",
            "Coding language",
            "Umbraco.DropDown.Flexible",
            [],
            ConfigurationJson: json,
            EditorUiAlias: "Umb.PropertyEditorUi.Dropdown");

        var status = await store.ApplyAsync(definition, CancellationToken.None);

        status.ShouldBe(SynchronizationStatus.Updated);
        var items = configuration["items"].ShouldBeAssignableTo<IEnumerable<string>>();
        var validator = new ValueListUniqueValueValidator(A.Fake<IConfigurationEditorJsonSerializer>());
        validator.Validate(items, null, null, null!).ShouldBeEmpty();
        if(configuration.TryGetValue("other", out var other)) {
            other.ShouldBeOfType<object[]>().ShouldBe([1, "two"]);
            var nested = configuration["nested"].ShouldBeOfType<Dictionary<string, object>>();
            nested["items"].ShouldBeAssignableTo<IEnumerable<string>>().ShouldBe(["a", "b"]);
        }
    }
}
