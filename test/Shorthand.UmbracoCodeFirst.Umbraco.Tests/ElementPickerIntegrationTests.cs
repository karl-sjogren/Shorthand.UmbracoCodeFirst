using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Generated;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.PropertyEditors;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;
using Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.Documents;
using Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.Elements;
using Umbraco.Cms.Core.Services;
using Xunit.Sdk;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

[Trait("Category", "Integration")]
[Collection("Umbraco integration")]
public class ElementPickerIntegrationTests(UmbracoIntegrationFixture fixture) : IClassFixture<UmbracoIntegrationFixture> {
    [Fact]
    public async Task SampleElementPicker_UsesElementTypeKeyAndSelectionLimit_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var synchronizer = new CodeFirstSynchronizer(
            new GeneratedCodeFirstDefinitionRegistry(),
            services.GetRequiredService<ICodeFirstDefinitionStore>(),
            new SynchronizationReportStore(),
            TimeProvider.System,
            services.GetRequiredService<ILogger<CodeFirstSynchronizer>>());

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        report.Items.Where(item => item.Status == SynchronizationStatus.Failed)
            .Select(item => $"{item.Alias}: {item.Message}").ShouldBeEmpty();
        var type = services.GetRequiredService<IContentTypeService>().Get(SiteSettingsElement.ModelTypeAlias).ShouldNotBeNull();
        type.IsElement.ShouldBeTrue();
        type.AllowedInLibrary.ShouldBeTrue();
        var dataTypes = services.GetRequiredService<IDataTypeService>();
        var dataType = (await dataTypes.GetAllAsync())
            .Single(item => item.Name == "Site Settings Picker");
        dataType.EditorAlias.ShouldBe("Umbraco.ElementPicker");
        dataType.EditorUiAlias.ShouldBe("Umb.PropertyEditorUi.ElementPicker");
        dataType.ConfigurationData["allowedContentTypes"].ShouldBe(type.Key.ToString());
        JsonSerializer.SerializeToElement(dataType.ConfigurationData["validationLimit"])
            .GetProperty("max").GetInt32().ShouldBe(1);
        dataTypes.ValidateConfigurationData(dataType).ShouldBeEmpty();
        var startPage = services.GetRequiredService<IContentTypeService>().Get(StartPage.ModelTypeAlias).ShouldNotBeNull();
        startPage.PropertyTypes.Single(property => property.Alias == "siteSettings").DataTypeKey.ShouldBe(dataType.Key);
        var item = report.Items.Single(entry => entry.Alias == UmbracoBlogSiteSettingsPickerDataType.Alias);
        item.Status.ShouldNotBe(SynchronizationStatus.Failed);
        var second = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
        second.Items.Single(entry => entry.Alias == UmbracoBlogSiteSettingsPickerDataType.Alias)
            .Status.ShouldBe(SynchronizationStatus.Unchanged);
    }

    [Fact]
    public async Task ElementPicker_MultipleAliases_PersistsElementKeys_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "integrationMultiPicker", "Integration Multi Picker",
                UmbracoEditors.ElementPicker.Alias, [],
                EditorUiAlias: UmbracoEditors.ElementPicker.UiAlias,
                EditorConfiguration: CodeFirstEditorConfiguration.FromType(
                    typeof(MultiElementPickerConfiguration), UmbracoEditors.ElementPicker.Alias)),
            new(CodeFirstDefinitionKind.DocumentType, "integrationPickerFirst", "Picker First", null,
                [], Settings: new(IsElement: true)),
            new(CodeFirstDefinitionKind.DocumentType, "integrationPickerSecond", "Picker Second", null,
                [], Settings: new(IsElement: true)),
        ]);
        var synchronizer = new CodeFirstSynchronizer(
            registry, services.GetRequiredService<ICodeFirstDefinitionStore>(),
            new SynchronizationReportStore(), TimeProvider.System,
            services.GetRequiredService<ILogger<CodeFirstSynchronizer>>());

        // Act
        var first = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
        var second = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        first.Items.Where(item => item.Status == SynchronizationStatus.Failed)
            .Select(item => $"{item.Alias}: {item.Message}").ShouldBeEmpty();
        second.Items.Single(item => item.Alias == "integrationMultiPicker").Status.ShouldBe(SynchronizationStatus.Unchanged);
        var contentTypes = services.GetRequiredService<IContentTypeService>();
        var firstKey = contentTypes.Get("integrationPickerFirst").ShouldNotBeNull().Key;
        var secondKey = contentTypes.Get("integrationPickerSecond").ShouldNotBeNull().Key;
        var dataTypes = services.GetRequiredService<IDataTypeService>();
        var dataType = (await dataTypes.GetAllAsync()).Single(item => item.Name == "Integration Multi Picker");
        dataType.ConfigurationData["allowedContentTypes"].ShouldBe($"{firstKey},{secondKey}");
        dataTypes.ValidateConfigurationData(dataType).ShouldBeEmpty();
    }

    private sealed class MultiElementPickerConfiguration : UmbracoEditors.ElementPicker.Configuration {
        public MultiElementPickerConfiguration() {
            Filter = ["integrationPickerFirst", "integrationPickerSecond"];
        }
    }

    private sealed class TestRegistry(IReadOnlyList<CodeFirstDefinition> definitions) : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = definitions;
    }
}
