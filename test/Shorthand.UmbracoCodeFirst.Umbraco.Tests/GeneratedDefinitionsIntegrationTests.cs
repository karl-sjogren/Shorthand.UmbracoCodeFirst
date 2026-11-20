using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shorthand.UmbracoCodeFirst.Backoffice.Contracts;
using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Generated;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.PropertyEditors;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;
using Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.Documents;
using Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.Elements;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.DocumentTypes;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.Elements;
using Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Editors;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Core.Web;
using Xunit.Sdk;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

[Trait("Category", "Integration")]
[Collection("Umbraco integration")]
public class GeneratedDefinitionsIntegrationTests(UmbracoIntegrationFixture fixture)
    : IClassFixture<UmbracoIntegrationFixture> {
    [Fact]
    public async Task Store_MediaPicker_PersistsSettingsAndReferencesBuiltInWithoutChangingIt_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
        var dataTypes = services.GetRequiredService<IDataTypeService>();
        var contentTypes = services.GetRequiredService<IContentTypeService>();
        var token = TestContext.Current.CancellationToken;
        var builtInKey = Guid.Parse(Constants.DataTypes.Guids.MediaPicker3SingleImage);
        var builtIn = (await dataTypes.GetAllAsync()).Single(item => item.Key == builtInKey);
        var originalConfiguration = JsonSerializer.Serialize(builtIn.ConfigurationData);
        var picker = new GeneratedCodeFirstDefinitionRegistry().Definitions
            .Single(item => item.Alias == UmbracoBlogMediaPickerDataType.Alias) with {
            Alias = "integrationMediaPicker",
            Name = "Integration Media Picker",
        };
        var document = new CodeFirstDefinition(
            CodeFirstDefinitionKind.DocumentType, "integrationMediaPickerPage", "Media Picker Page", null,
            [new("image", "Image", "", DataTypeKey: builtInKey.ToString())]);

        // Act
        var pickerResult = await store.ApplyAsync(picker, token);
        var documentResult = await store.ApplyAsync(document, token);
        var repeated = await store.ApplyAsync(document, token);
        var savedPicker = (await dataTypes.GetAllAsync()).Single(item => item.Name == picker.Name);
        var savedBuiltIn = (await dataTypes.GetAllAsync()).Single(item => item.Key == builtInKey);
        var savedDocument = contentTypes.Get(document.Alias).ShouldNotBeNull();

        // Assert
        pickerResult.Status.ShouldBe(SynchronizationStatus.Created);
        documentResult.Status.ShouldBe(SynchronizationStatus.Created);
        repeated.Status.ShouldBe(SynchronizationStatus.Unchanged);
        savedDocument.PropertyTypes.Single(property => property.Alias == "image").DataTypeKey.ShouldBe(builtInKey);
        JsonSerializer.Serialize(savedBuiltIn.ConfigurationData).ShouldBe(originalConfiguration);
        dataTypes.ValidateConfigurationData(savedPicker).ShouldBeEmpty();
        savedPicker.ConfigurationData["filter"].ShouldBe(Constants.MediaTypes.Guids.Image);
        savedPicker.ConfigurationData["multiple"].ShouldBe(true);
        JsonSerializer.SerializeToElement(savedPicker.ConfigurationData["validationLimit"]).GetProperty("max").GetInt32().ShouldBe(3);
        JsonSerializer.SerializeToElement(savedPicker.ConfigurationData["crops"])[0].GetProperty("alias").GetString().ShouldBe("thumbnail");

        // Arrange
        var otherKey = Guid.Parse(Constants.DataTypes.Guids.MediaPicker3MultipleImages);
        var changed = document with {
            Properties = [new("image", "Image", "", DataTypeKey: otherKey.ToString())],
        };

        // Act
        var changedResult = await store.ApplyAsync(changed, token);
        var changedDocument = contentTypes.Get(document.Alias).ShouldNotBeNull();

        // Assert
        changedResult.Status.ShouldBe(SynchronizationStatus.Updated);
        changedDocument.PropertyTypes.Single(property => property.Alias == "image").DataTypeKey.ShouldBe(otherKey);

        // Arrange
        var missingKey = Guid.NewGuid().ToString();
        var missing = document with {
            Alias = "integrationMissingMediaPickerPage",
            Properties = [new("image", "Image", "", DataTypeKey: missingKey)],
        };

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => store.ApplyAsync(missing, token));

        // Assert
        exception.Message.ShouldContain(missingKey);
    }

    [Fact]
    public async Task Store_NestedGroups_ScopesAliasesAndMovesPropertiesWithoutLosingManualStructure_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        await SynchronizeTestDefinitionsAsync(services);
        var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
        var contentTypes = services.GetRequiredService<IContentTypeService>();
        var token = TestContext.Current.CancellationToken;
        var initial = new CodeFirstDefinition(
            CodeFirstDefinitionKind.DocumentType, "integrationNestedLayouts", "Nested layouts", null, [
                new("first", "First", IntegrationTextDataType.Alias, GroupAlias: "details", GroupName: "Details"),
                new("second", "Second", IntegrationTextDataType.Alias, GroupAlias: "details", GroupName: "Details"),
                new("root", "Root", IntegrationTextDataType.Alias, GroupAlias: "details", GroupName: "Details"),
            ]);
        (await store.ApplyAsync(initial, token)).Status.ShouldBe(SynchronizationStatus.Created);
        var original = contentTypes.Get(initial.Alias).ShouldNotBeNull();
        original.AddPropertyGroup("manualTab", "Manual tab");
        original.PropertyGroups.Single(group => group.Alias == "manualTab").Type = PropertyGroupType.Tab;
        original.AddPropertyGroup("manualTab/manualGroup", "Manual group");
        original.AddPropertyGroup("manualEmpty", "ManualEmpty");
        original.AddPropertyGroup("manualReserved", "Manual reserved");
        original.PropertyGroups.Single(group => group.Alias == "manualReserved").Type = PropertyGroupType.Group;
        var dataType = (await services.GetRequiredService<IDataTypeService>().GetAllAsync())
            .Single(item => item.Name == "Integration Text");
        original.AddPropertyType(
            new PropertyType(services.GetRequiredService<IShortStringHelper>(), dataType, "manualText") {
                Name = "Manual text",
            },
            "manualReserved");
        (await contentTypes.UpdateAsync(original, Constants.Security.SuperUserKey)).Success.ShouldBeTrue();
        var updated = initial with {
            Properties = [
                initial.Properties[0] with { TabAlias = "content", TabName = "Content", TabSortOrder = 1, GroupSortOrder = 2 },
                initial.Properties[1] with { TabAlias = "settings", TabName = "Settings" },
                initial.Properties[2],
                new("fourth", "Fourth", IntegrationTextDataType.Alias,
                    GroupAlias: "same", GroupName: "Same", TabAlias: "same", TabName: "Same"),
            ],
        };

        // Act
        var first = await store.ApplyAsync(updated, token);
        var persisted = contentTypes.Get(initial.Alias).ShouldNotBeNull();
        var repeated = await store.ApplyAsync(updated, token);

        // Assert
        first.Status.ShouldBe(SynchronizationStatus.Updated);
        repeated.Status.ShouldBe(SynchronizationStatus.Unchanged);
        persisted.PropertyGroups.Single(group => group.Alias == "content").Type.ShouldBe(PropertyGroupType.Tab);
        persisted.PropertyGroups.Single(group => group.Alias == "content").SortOrder.ShouldBe(1);
        var contentGroup = persisted.PropertyGroups.Single(group => group.Alias == "content/details");
        contentGroup.Type.ShouldBe(PropertyGroupType.Group);
        contentGroup.SortOrder.ShouldBe(2);
        contentGroup.PropertyTypes.ShouldNotBeNull().Select(property => property.Alias).ShouldBe(["first"]);
        persisted.PropertyGroups.Single(group => group.Alias == "settings/details").PropertyTypes
            .ShouldNotBeNull().Select(property => property.Alias).ShouldBe(["second"]);
        persisted.PropertyGroups.Single(group => group.Alias == "details").PropertyTypes
            .ShouldNotBeNull().Select(property => property.Alias).ShouldBe(["root"]);
        persisted.PropertyGroups.Single(group => group.Alias == "same").Type.ShouldBe(PropertyGroupType.Tab);
        persisted.PropertyGroups.Single(group => group.Alias == "same/same").PropertyTypes
            .ShouldNotBeNull().Select(property => property.Alias).ShouldBe(["fourth"]);
        persisted.PropertyGroups.Select(group => group.Alias).ShouldContain("manualTab");
        persisted.PropertyGroups.Select(group => group.Alias).ShouldContain("manualTab/manualGroup");
        persisted.PropertyGroups.Select(group => group.Alias).ShouldContain("manualEmpty");

        // Arrange
        var conflicting = updated with {
            Properties = [
                updated.Properties[0],
                updated.Properties[1] with { TabAlias = "content", TabName = "Content", TabSortOrder = 1, GroupName = "Other" },
                updated.Properties[2],
            ],
        };

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await store.ApplyAsync(conflicting, token));
        var afterConflict = contentTypes.Get(initial.Alias).ShouldNotBeNull();

        // Assert
        exception.Message.ShouldContain("content/details");
        afterConflict.PropertyGroups.Single(group => group.Alias == "settings/details").PropertyTypes
            .ShouldNotBeNull().Select(property => property.Alias).ShouldBe(["second"]);

        // Arrange
        var manualConflict = updated with {
            Properties = [
                .. updated.Properties,
                new("blocked", "Blocked", IntegrationTextDataType.Alias,
                    TabAlias: "manualReserved", TabName: "Manual reserved"),
            ],
        };

        // Act
        var manualException = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await store.ApplyAsync(manualConflict, token));
        var afterManualConflict = contentTypes.Get(initial.Alias).ShouldNotBeNull();

        // Assert
        manualException.Message.ShouldContain("manualReserved");
        var manualGroup = afterManualConflict.PropertyGroups.Single(group => group.Alias == "manualReserved");
        manualGroup.Type.ShouldBe(PropertyGroupType.Group);
        manualGroup.PropertyTypes.ShouldNotBeNull().Select(property => property.Alias).ShouldBe(["manualText"]);

        // Act
        var movedBack = await store.ApplyAsync(initial, token);
        var restored = contentTypes.Get(initial.Alias).ShouldNotBeNull();
        var nestedAgain = await store.ApplyAsync(updated, token);
        var final = contentTypes.Get(initial.Alias).ShouldNotBeNull();

        // Assert
        movedBack.Status.ShouldBe(SynchronizationStatus.Updated);
        restored.PropertyGroups.Single(group => group.Alias == "details").PropertyTypes
            .ShouldNotBeNull().Select(property => property.Alias).Order().ShouldBe(["first", "root", "second"]);
        nestedAgain.Status.ShouldBe(SynchronizationStatus.Updated);
        final.PropertyGroups.Single(group => group.Alias == "content/details").PropertyTypes
            .ShouldNotBeNull().Select(property => property.Alias).ShouldBe(["first"]);
    }

    [Fact]
    public async Task Store_RichTextToolbar_UpdatesExistingDataType_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var definition = new GeneratedCodeFirstDefinitionRegistry().Definitions
            .Single(item => item.Alias == UmbracoBlogRichTextDataType.Alias) with {
            Alias = "integrationRichTextToolbar",
            Name = "Integration Rich Text Toolbar",
        };
        var original = definition with {
            EditorConfiguration = CodeFirstEditorConfiguration.FromJson("Umbraco.RichText", "{}"),
        };
        var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
        var dataTypes = services.GetRequiredService<IDataTypeService>();
        var token = TestContext.Current.CancellationToken;
        await store.ApplyAsync(original, token);
        var dataType = (await dataTypes.GetAllAsync()).Single(item => item.Name == definition.Name);
        dataType.ConfigurationData["manualSetting"] = "preserved";
        (await dataTypes.UpdateAsync(dataType, Constants.Security.SuperUserKey)).Success.ShouldBeTrue();

        // Act
        var restartedStore = ActivatorUtilities.CreateInstance<UmbracoCodeFirstDefinitionStore>(services);
        var result = await restartedStore.ApplyAsync(definition, token);
        var saved = (await dataTypes.GetAllAsync()).Single(item => item.Name == definition.Name);
        var repeated = await restartedStore.ApplyAsync(definition, token);

        // Assert
        result.Status.ShouldBe(SynchronizationStatus.Updated);
        repeated.Status.ShouldBe(SynchronizationStatus.Unchanged);
        saved.Key.ShouldBe(dataType.Key);
        saved.ConfigurationData["manualSetting"].ShouldBe("preserved");
        dataTypes.ValidateConfigurationData(saved).ShouldBeEmpty();
        JsonSerializer.SerializeToElement(saved.ConfigurationData["extensions"]).EnumerateArray()
            .Select(item => item.GetString()).ShouldContain("Umb.Tiptap.Bold");
        JsonSerializer.SerializeToElement(saved.ConfigurationData["toolbar"])[0].EnumerateArray()
            .SelectMany(group => group.EnumerateArray().Select(item => item.GetString()))
            .ShouldContain("Umb.Tiptap.Toolbar.Bold");
    }

    [Fact]
    public async Task Store_CustomizedRichTextDefaults_PersistsCollectionChanges_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var definition = new GeneratedCodeFirstDefinitionRegistry().Definitions
            .Single(item => item.Alias == UmbracoBlogRichTextDataType.Alias) with {
            Alias = "integrationCustomizedRichText",
            Name = "Integration Customized Rich Text",
            EditorConfiguration = CodeFirstEditorConfiguration.FromType(
                typeof(IntegrationCustomizedRichTextConfiguration), "Umbraco.RichText"),
        };
        var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
        var dataTypes = services.GetRequiredService<IDataTypeService>();

        // Act
        var result = await store.ApplyAsync(definition, TestContext.Current.CancellationToken);
        var saved = (await dataTypes.GetAllAsync()).Single(item => item.Name == definition.Name);

        // Assert
        result.Status.ShouldBe(SynchronizationStatus.Created);
        dataTypes.ValidateConfigurationData(saved).ShouldBeEmpty();
        var extensions = JsonSerializer.SerializeToElement(saved.ConfigurationData["extensions"])
            .EnumerateArray().Select(item => item.GetString()).ToArray();
        extensions.ShouldNotContain("Umb.Tiptap.Bold");
        extensions.ShouldContain("Umb.Tiptap.Strike");
        var toolbar = JsonSerializer.SerializeToElement(saved.ConfigurationData["toolbar"])[0][1]
            .EnumerateArray().Select(item => item.GetString()).ToArray();
        toolbar.ShouldNotContain("Umb.Tiptap.Toolbar.Bold");
        toolbar.ShouldContain("Umb.Tiptap.Toolbar.Strike");
    }

    [Fact]
    public async Task Store_ChangedDataTypeEditor_UpdatesTagsEditorAndPreservesValues_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var original = new CodeFirstDefinition(
            CodeFirstDefinitionKind.DataType,
            "integrationChangedTags",
            "Integration Changed Tags",
            UmbracoEditors.TextBox.Alias,
            [],
            EditorUiAlias: UmbracoEditors.Tags.UiAlias,
            ConfigurationJson: """{"group":"integration"}""");
        var tagsDefinition = new GeneratedCodeFirstDefinitionRegistry().Definitions
            .Single(definition => definition.Alias == UmbracoBlogTagsDataType.Alias);
        var corrected = original with { EditorAlias = tagsDefinition.EditorAlias };
        var document = new CodeFirstDefinition(
            CodeFirstDefinitionKind.DocumentType,
            "integrationChangedTagsDocument",
            "Integration Changed Tags Document",
            null,
            [new("tags", "Tags", original.Alias)],
            Settings: new(AllowAsRoot: true));
        var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
        var contentService = services.GetRequiredService<IContentService>();
        var dataTypes = services.GetRequiredService<IDataTypeService>();
        var token = TestContext.Current.CancellationToken;
        await store.ApplyAsync(original, token);
        await store.ApplyAsync(document, token);
        var content = contentService.Create("Tags test", -1, document.Alias);
        const string storedValue = """["example"]""";
        content.SetValue("tags", storedValue);
        contentService.Save(content).Success.ShouldBeTrue();
        var malformedContent = contentService.Create("Malformed tags test", -1, document.Alias);
        const string malformedValue = "System.Collections.Generic.List`1[System.String]";
        malformedContent.SetValue("tags", malformedValue);
        contentService.Save(malformedContent).Success.ShouldBeTrue();

        // Act
        var restartedStore = ActivatorUtilities.CreateInstance<UmbracoCodeFirstDefinitionStore>(services);
        var result = await restartedStore.ApplyAsync(corrected, token);
        var dataType = (await dataTypes.GetAllAsync()).Single(item => item.Name == original.Name);
        var reopened = contentService.GetById(content.Id)!;
        var property = reopened.Properties["tags"]!;
        var editors = services.GetRequiredService<PropertyEditorCollection>();
        var valueEditor = editors[property.PropertyType.PropertyEditorAlias]!.GetValueEditor();
        var editorValue = valueEditor.ToEditor(property);
        var savedValue = valueEditor.FromEditor(new ContentPropertyData(new[] { "example" }, dataType.ConfigurationObject), null);
        var malformedReopened = contentService.GetById(malformedContent.Id)!;
        var malformedEditorValue = valueEditor.ToEditor(malformedReopened.Properties["tags"]!);

        // Assert
        result.Status.ShouldBe(SynchronizationStatus.Updated);
        dataType.EditorAlias.ShouldBe(UmbracoEditors.Tags.Alias);
        dataType.EditorUiAlias.ShouldBe(UmbracoEditors.Tags.UiAlias);
        dataType.ConfigurationData["group"].ShouldBe("integration");
        dataTypes.ValidateConfigurationData(dataType).ShouldBeEmpty();
        property.PropertyType.PropertyEditorAlias.ShouldBe(UmbracoEditors.Tags.Alias);
        reopened.GetValue<string>("tags").ShouldBe(storedValue);
        editorValue.ShouldBeOfType<string[]>().ShouldBe(["example"]);
        savedValue.ShouldBe(storedValue);
        malformedReopened.GetValue<string>("tags").ShouldBe(malformedValue);
        malformedEditorValue.ShouldBeNull();
    }

    [Fact]
    public async Task Store_UnknownReplacementEditor_LeavesDataTypeUnchanged_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var definition = new CodeFirstDefinition(
            CodeFirstDefinitionKind.DataType,
            "integrationUnknownEditor",
            "Integration Known Editor",
            UmbracoEditors.TextBox.Alias,
            [],
            EditorUiAlias: UmbracoEditors.TextBox.UiAlias);
        var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
        var dataTypes = services.GetRequiredService<IDataTypeService>();
        var token = TestContext.Current.CancellationToken;
        await store.ApplyAsync(definition, token);

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => store.ApplyAsync(
            definition with { Name = "Changed name", EditorAlias = "Missing.Editor" }, token));
        var dataType = (await dataTypes.GetAllAsync()).Single(item => item.Name == definition.Name);

        // Assert
        exception.Message.ShouldContain("Missing.Editor");
        dataType.EditorAlias.ShouldBe(UmbracoEditors.TextBox.Alias);
        dataType.EditorUiAlias.ShouldBe(UmbracoEditors.TextBox.UiAlias);
    }

    [Fact]
    public async Task AppRegistry_AllowedChildren_ArePersistedAndRestored_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var registry = new GeneratedCodeFirstDefinitionRegistry();
        var indexDefinition = registry.Definitions.Single(definition => definition.Alias == BlogIndex.ModelTypeAlias);
        var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
        var synchronizer = new CodeFirstSynchronizer(
            registry, store, new SynchronizationReportStore(), TimeProvider.System,
            services.GetRequiredService<ILogger<CodeFirstSynchronizer>>());
        var contentTypes = services.GetRequiredService<IContentTypeService>();
        var token = TestContext.Current.CancellationToken;

        // Act
        var initialReport = await synchronizer.SynchronizeAsync(token);
        var firstChildren = contentTypes.Get(BlogIndex.ModelTypeAlias)?.AllowedContentTypes?.ToArray();
        var cleared = await store.ApplyAsync(indexDefinition with {
            Settings = indexDefinition.Settings! with { AllowedChildAliases = [] },
        }, token);
        var restartedStore = ActivatorUtilities.CreateInstance<UmbracoCodeFirstDefinitionStore>(services);
        var restartedSynchronizer = new CodeFirstSynchronizer(
            registry, restartedStore, new SynchronizationReportStore(), TimeProvider.System,
            services.GetRequiredService<ILogger<CodeFirstSynchronizer>>());
        var restoredReport = await restartedSynchronizer.SynchronizeAsync(token);
        var restoredChildren = contentTypes.Get(BlogIndex.ModelTypeAlias)?.AllowedContentTypes?.ToArray();
        var repeatedReport = await restartedSynchronizer.SynchronizeAsync(token);
        var child = contentTypes.Get(BlogPost.ModelTypeAlias);

        // Assert
        indexDefinition.Settings!.AllowedChildAliases.ShouldBe([BlogPost.ModelTypeAlias]);
        initialReport.FailedCount.ShouldBe(0);
        firstChildren.ShouldNotBeNull().Select(item => item.Alias).ShouldBe([BlogPost.ModelTypeAlias]);
        cleared.Status.ShouldBe(SynchronizationStatus.Updated);
        restoredReport.FailedCount.ShouldBe(0);
        restoredReport.Items.Single(item => item.Alias == BlogIndex.ModelTypeAlias).Status.ShouldBe(SynchronizationStatus.Updated);
        child.ShouldNotBeNull();
        restoredChildren.ShouldNotBeNull().Single().ShouldSatisfyAllConditions(
            item => item.Alias.ShouldBe(BlogPost.ModelTypeAlias),
            item => item.Key.ShouldBe(child.Key));
        repeatedReport.Items.Single(item => item.Alias == BlogIndex.ModelTypeAlias).Status.ShouldBe(SynchronizationStatus.Unchanged);
    }

    [Fact]
    public async Task BlogIndex_Collection_SortsPostsByPublishDateDescending_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var registry = new GeneratedCodeFirstDefinitionRegistry();
        var synchronizer = new CodeFirstSynchronizer(
            registry, services.GetRequiredService<ICodeFirstDefinitionStore>(),
            new SynchronizationReportStore(), TimeProvider.System,
            services.GetRequiredService<ILogger<CodeFirstSynchronizer>>());
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
        report.FailedCount.ShouldBe(0);
        var contentService = services.GetRequiredService<IContentService>();
        var index = contentService.Create("Blog", -1, BlogIndex.ModelTypeAlias);
        contentService.Save(index).Success.ShouldBeTrue();
        var older = contentService.Create("Older", index.Id, BlogPost.ModelTypeAlias);
        older.SetValue(BlogPost.PropertyAliases.MainHeading, "Older title");
        older.SetValue(BlogPost.PropertyAliases.PublishDate, new DateTime(2024, 1, 1));
        contentService.Save(older).Success.ShouldBeTrue();
        var newer = contentService.Create("Newer", index.Id, BlogPost.ModelTypeAlias);
        newer.SetValue(BlogPost.PropertyAliases.MainHeading, "Newer title");
        newer.SetValue(BlogPost.PropertyAliases.PublishDate, new DateTime(2025, 1, 1));
        contentService.Save(newer).Success.ShouldBeTrue();
        var user = await services.GetRequiredService<IUserService>().GetAsync(Constants.Security.SuperUserKey)
            ?? throw new InvalidOperationException("The integration user was not found.");

        // Act
        var result = await services.GetRequiredService<IContentListViewService>().GetListViewItemsByKeyAsync(
            user, index.Key, null, BlogPost.PropertyAliases.PublishDate, null, Direction.Descending, null, 0, 10);

        // Assert
        result.Success.ShouldBeTrue();
        contentService.GetById(older.Id)!.GetValue(BlogPost.PropertyAliases.PublishDate).ShouldBe(new DateTime(2024, 1, 1));
        contentService.GetById(newer.Id)!.GetValue(BlogPost.PropertyAliases.PublishDate).ShouldBe(new DateTime(2025, 1, 1));
        result.Result.ShouldNotBeNull().ListViewConfiguration.IncludeProperties
            .Single(column => column.Alias == BlogPost.PropertyAliases.PublishDate).IsSystem.ShouldBeFalse();
        result.Result.ShouldNotBeNull().Items.Items.Select(item => item.Name).ShouldBe(["Newer", "Older"]);
    }

    [Fact]
    public async Task Store_DateDataType_UsesDateStorageWithoutChangingExistingStorage_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var definition = new CodeFirstDefinition(
            CodeFirstDefinitionKind.DataType,
            "integrationDateStorage",
            "Integration Date Storage",
            UmbracoEditors.DateTime.Alias,
            [],
            EditorUiAlias: UmbracoEditors.DateTime.UiAlias);
        var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
        var dataTypes = services.GetRequiredService<IDataTypeService>();
        var token = TestContext.Current.CancellationToken;

        // Act
        await store.ApplyAsync(definition, token);
        var created = (await dataTypes.GetAllAsync()).Single(item => item.Name == definition.Name);

        // Assert
        created.DatabaseType.ShouldBe(ValueStorageType.Date);

        // Arrange
        created.DatabaseType = ValueStorageType.Ntext;
        (await dataTypes.UpdateAsync(created, Constants.Security.SuperUserKey)).Success.ShouldBeTrue();
        var restartedStore = ActivatorUtilities.CreateInstance<UmbracoCodeFirstDefinitionStore>(services);

        // Act
        await restartedStore.ApplyAsync(definition, token);
        var existing = (await dataTypes.GetAllAsync()).Single(item => item.Name == definition.Name);

        // Assert
        existing.DatabaseType.ShouldBe(ValueStorageType.Ntext);
    }

    [Fact]
    public async Task AppRegistry_CodeBlockDependencies_SynchronizeAndPersist_Async() {
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
        var templates = services.GetRequiredService<ITemplateService>();
        var contentTypes = services.GetRequiredService<IContentTypeService>();
        foreach(var (documentAlias, templateAlias) in new[] {
            ("landingPage", UmbracoTemplates.LandingPage),
            ("productPage", UmbracoTemplates.ProductPage),
        }) {
            var template = (await templates.GetAsync(templateAlias)).ShouldNotBeNull();
            var document = contentTypes.Get(documentAlias).ShouldNotBeNull();
            document.DefaultTemplate.ShouldNotBeNull().Key.ShouldBe(template.Key);
            document.AllowedTemplates.ShouldNotBeNull().Select(item => item.Key).ShouldContain(template.Key);
        }

        report.Items.Where(item => item.Alias == UmbracoBlogCodeEditorDataType.Alias
            || item.Alias == CodeBlock.ModelTypeAlias
            || item.Alias == UmbracoBlogPostContentDataType.Alias)
            .ShouldSatisfyAllConditions(
                items => items.Count().ShouldBe(3),
                items => items.ShouldAllBe(item => item.Status == SynchronizationStatus.Created
                    || item.Status == SynchronizationStatus.Updated || item.Status == SynchronizationStatus.Unchanged));

        var dataTypes = services.GetRequiredService<IDataTypeService>();
        var dataType = (await dataTypes.GetAllAsync()).Single(item => item.Name == "Code Editor");
        dataType.EditorAlias.ShouldBe("Umbraco.Plain.String");
        dataType.EditorUiAlias.ShouldBe("Umb.PropertyEditorUi.CodeEditor");
        JsonSerializer.SerializeToElement(dataType.ConfigurationData["height"]).GetInt32().ShouldBe(600);
        dataTypes.ValidateConfigurationData(dataType).ShouldBeEmpty();

        var element = services.GetRequiredService<IContentTypeService>().Get(CodeBlock.ModelTypeAlias);
        element.ShouldNotBeNull();
        element.PropertyTypes.Single(property => property.Alias == "code").DataTypeKey.ShouldBe(dataType.Key);
        var publishedTypes = services.GetRequiredService<IPublishedContentTypeCache>();
        CodeBlock.GetModelContentType(publishedTypes).ShouldNotBeNull().Alias.ShouldBe(CodeBlock.ModelTypeAlias);
        CodeBlock.GetModelPropertyType(publishedTypes, model => model.Code).ShouldNotBeNull().Alias.ShouldBe("code");
        Article.GetModelPropertyType(publishedTypes, model => model.SeoCompositeTitle)
            .ShouldNotBeNull().Alias.ShouldBe(Article.PropertyAliases.SeoCompositeTitle);
        CoolElement.GetModelPropertyType(publishedTypes, model => model.CopyrightYear)
            .ShouldNotBeNull().Alias.ShouldBe(CoolElement.PropertyAliases.CopyrightYear);
        var blockList = (await dataTypes.GetAllAsync()).Single(item => item.Name == "Blog Post Content");
        JsonSerializer.SerializeToElement(blockList.ConfigurationData["blocks"]).EnumerateArray()
            .ShouldContain(block => block.GetProperty("contentElementTypeKey").GetGuid() == element.Key);
    }

    [Fact]
    public async Task AppRegistry_ComposedPublishedModel_ReadsPublishedValue_Async() {
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
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
        report.FailedCount.ShouldBe(0);
        var contentService = services.GetRequiredService<IContentService>();
        var content = contentService.Create("Composite article", -1, Article.ModelTypeAlias);
        content.SetValue(Article.PropertyAliases.Title, "Article title");
        content.SetValue(Article.PropertyAliases.SeoCompositeTitle, "Published SEO title");
        content.SetValue(Article.PropertyAliases.Sparkles, "Published sparkles");
        contentService.SaveAndPublish(content, []).Success.ShouldBeTrue();

        // Act
        using var context = services.GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext();
        var published = (await context.UmbracoContext.Content!.GetByIdAsync(content.Key)).ShouldNotBeNull();
        var model = new Article(published, services.GetRequiredService<IPublishedValueFallback>());
        ISeoComposite composite = model;

        // Assert
        model.SeoCompositeTitle.ShouldBe("Published SEO title");
        composite.SeoCompositeTitle.ShouldBe("Published SEO title");
        model.Sparkles.ShouldBe("Published sparkles");
        Article.GetModelPropertyType(services.GetRequiredService<IPublishedContentTypeCache>(), item => item.SeoCompositeTitle)
            .ShouldNotBeNull().Alias.ShouldBe("seoCompositeTitle");
    }

    [Fact]
    public async Task SynchronizeAsync_ChangedPropertyDataType_UpdatesExistingElement_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var definitions = new GeneratedCodeFirstDefinitionRegistry().Definitions;
        var editor = definitions.Single(definition => definition.Alias == UmbracoBlogCodeEditorDataType.Alias);
        var language = definitions.Single(definition => definition.Alias == UmbracoBlogCodingLanguageDataType.Alias);
        var element = definitions.Single(definition => definition.Alias == CodeBlock.ModelTypeAlias) with {
            Alias = "integrationChangingEditor",
            Name = "Integration Changing Editor",
        };
        var original = element with {
            Properties = element.Properties.Select(property => property.Alias == "code"
                ? property with { DataTypeAlias = language.Alias }
                : property).ToArray(),
        };
        var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
        var contentTypes = services.GetRequiredService<IContentTypeService>();
        var dataTypes = services.GetRequiredService<IDataTypeService>();
        var token = TestContext.Current.CancellationToken;
        await store.ApplyAsync(language, token);
        await store.ApplyAsync(editor, token);
        await store.ApplyAsync(original, token);
        var originalProperty = contentTypes.Get(element.Alias)!.PropertyTypes.Single(property => property.Alias == "code");
        var propertyKey = originalProperty.Key;
        var languageType = (await dataTypes.GetAllAsync()).Single(dataType => dataType.Name == language.Name);
        var editorType = (await dataTypes.GetAllAsync()).Single(dataType => dataType.Name == editor.Name);
        var synchronizer = new CodeFirstSynchronizer(
            new TestRegistry([language, editor, element]),
            store,
            new SynchronizationReportStore(),
            TimeProvider.System,
            services.GetRequiredService<ILogger<CodeFirstSynchronizer>>());

        // Act
        var report = await synchronizer.SynchronizeAsync(token);
        var updated = contentTypes.Get(element.Alias);
        var repeated = await synchronizer.SynchronizeAsync(token);

        // Assert
        originalProperty.DataTypeKey.ShouldBe(languageType.Key);
        languageType.DatabaseType.ShouldBe(editorType.DatabaseType);
        report.Items.Single(item => item.Alias == element.Alias).Status.ShouldBe(SynchronizationStatus.Updated);
        updated.ShouldNotBeNull();
        var code = updated.PropertyTypes.Single(property => property.Alias == "code");
        code.Key.ShouldBe(propertyKey);
        code.DataTypeKey.ShouldBe(editorType.Key);
        code.DataTypeId.ShouldBe(editorType.Id);
        code.PropertyEditorAlias.ShouldBe(editorType.EditorAlias);
        code.ValueStorageType.ShouldBe(editorType.DatabaseType);
        updated.PropertyTypes.Single(property => property.Alias == "language").DataTypeKey.ShouldBe(languageType.Key);
        repeated.FailedCount.ShouldBe(0);
        contentTypes.Get(element.Alias)!.PropertyTypes.Single(property => property.Alias == "code")
            .DataTypeKey.ShouldBe(editorType.Key);
    }

    [Fact]
    public async Task SynchronizeAsync_ChangedPropertyStorageType_FailsWithoutUpdatingElement_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var integerDefinition = new IntegrationTestDefinitionRegistry().Definitions
            .Single(definition => definition.Alias == IntegrationIntegerDataType.Alias);
        var integer = integerDefinition with {
            Alias = "integrationIncompatibleInteger",
            Name = "Integration Incompatible Integer",
        };
        var editor = new GeneratedCodeFirstDefinitionRegistry().Definitions
            .Single(definition => definition.Alias == UmbracoBlogCodeEditorDataType.Alias);
        var original = new CodeFirstDefinition(
            CodeFirstDefinitionKind.DocumentType,
            "integrationIncompatibleProperty",
            "Integration Incompatible Property",
            null,
            [new("value", "Original value", integer.Alias)],
            Settings: new(IsElement: true));
        var updated = original with {
            Name = "Updated name",
            Properties = [new("value", "Updated value", editor.Alias)],
        };
        var store = ActivatorUtilities.CreateInstance<UmbracoCodeFirstDefinitionStore>(services);
        var contentTypes = services.GetRequiredService<IContentTypeService>();
        var token = TestContext.Current.CancellationToken;
        await store.ApplyAsync(integer, token);
        await store.ApplyAsync(editor, token);
        var dataTypes = services.GetRequiredService<IDataTypeService>();
        var integerType = (await dataTypes.GetAllAsync()).Single(dataType => dataType.Name == integer.Name);
        integerType.DatabaseType = ValueStorageType.Integer;
        var dataTypeUpdate = await dataTypes.UpdateAsync(integerType, Constants.Security.SuperUserKey);
        dataTypeUpdate.Success.ShouldBeTrue();
        store = ActivatorUtilities.CreateInstance<UmbracoCodeFirstDefinitionStore>(services);
        await store.ApplyAsync(original, token);
        var originalProperty = contentTypes.Get(original.Alias)!.PropertyTypes.Single();
        var editorType = (await dataTypes.GetAllAsync())
            .Single(dataType => dataType.Name == editor.Name);
        originalProperty.ValueStorageType.ShouldNotBe(editorType.DatabaseType);
        var synchronizer = new CodeFirstSynchronizer(
            new TestRegistry([integer, editor, updated]),
            store,
            new SynchronizationReportStore(),
            TimeProvider.System,
            services.GetRequiredService<ILogger<CodeFirstSynchronizer>>());

        // Act
        var report = await synchronizer.SynchronizeAsync(token);
        var persisted = contentTypes.Get(original.Alias);

        // Assert
        var item = report.Items.Single(item => item.Alias == original.Alias);
        item.Status.ShouldBe(SynchronizationStatus.Failed);
        item.Message.ShouldNotBeNull().ShouldContain("storage type");
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe(original.Name);
        persisted.PropertyTypes.Single().ShouldSatisfyAllConditions(
            property => property.Name.ShouldBe("Original value"),
            property => property.DataTypeKey.ShouldBe(originalProperty.DataTypeKey),
            property => property.Key.ShouldBe(originalProperty.Key));
    }

    [Fact]
    public async Task SynchronizeAsync_ChangedPropertyDataType_PreservesExistingStoredValue_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var definitions = new GeneratedCodeFirstDefinitionRegistry().Definitions;
        var language = definitions.Single(definition => definition.Alias == UmbracoBlogCodingLanguageDataType.Alias);
        var editor = definitions.Single(definition => definition.Alias == UmbracoBlogCodeEditorDataType.Alias);
        var original = new CodeFirstDefinition(
            CodeFirstDefinitionKind.DocumentType,
            "integrationPropertyValue",
            "Integration Property Value",
            null,
            [new("value", "Value", language.Alias)],
            Settings: new(AllowAsRoot: true));
        var changed = original with { Properties = [new("value", "Value", editor.Alias)] };
        var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
        var contentService = services.GetRequiredService<IContentService>();
        var token = TestContext.Current.CancellationToken;
        await store.ApplyAsync(language, token);
        await store.ApplyAsync(editor, token);
        await store.ApplyAsync(original, token);
        var content = contentService.Create("Test content", -1, original.Alias);
        const string value = """["csharp"]""";
        content.SetValue("value", value);
        contentService.Save(content).Success.ShouldBeTrue();

        // Act
        var result = await store.ApplyAsync(changed, token);
        var persisted = contentService.GetById(content.Id);

        // Assert
        result.Status.ShouldBe(SynchronizationStatus.Updated);
        persisted.ShouldNotBeNull();
        persisted.GetValue<string>("value").ShouldBe(value);
    }

    [Fact]
    public async Task Store_DropdownConfiguration_CreatesAndUpdatesDataType_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ICodeFirstDefinitionStore>();
        var dataTypeService = scope.ServiceProvider.GetRequiredService<IDataTypeService>();
        var definition = new CodeFirstDefinition(
            CodeFirstDefinitionKind.DataType,
            "integrationCodingLanguage",
            "Integration Coding Language",
            UmbracoEditors.Dropdown.Alias,
            [],
            EditorUiAlias: UmbracoEditors.Dropdown.UiAlias,
            EditorConfiguration: CodeFirstEditorConfiguration.FromType(
                typeof(IntegrationDropdownConfiguration),
                UmbracoEditors.Dropdown.Alias));

        // Act
        var created = await store.ApplyAsync(definition, TestContext.Current.CancellationToken);
        // Assert
        created.Status.ShouldBe(SynchronizationStatus.Created);

        var createdDataType = (await dataTypeService.GetAllAsync())
            .Single(dataType => dataType.Name == definition.Name);
        JsonSerializer.SerializeToElement(createdDataType.ConfigurationData["items"])
            .EnumerateArray().Select(item => item.GetString())
            .ShouldBe(["csharp", "javascript"]);

        var updatedDefinition = definition with {
            ConfigurationJson = """{"items":["csharp","typescript","bash"],"multiple":false}""",
            EditorConfiguration = null,
        };
        var restartedStore = ActivatorUtilities.CreateInstance<UmbracoCodeFirstDefinitionStore>(scope.ServiceProvider);
        var updated = await restartedStore.ApplyAsync(updatedDefinition, TestContext.Current.CancellationToken);
        updated.Status.ShouldBe(SynchronizationStatus.Updated);

        var persisted = (await dataTypeService.GetAllAsync())
            .Single(dataType => dataType.Name == definition.Name);
        persisted.EditorAlias.ShouldBe(UmbracoEditors.Dropdown.Alias);
        persisted.EditorUiAlias.ShouldBe(UmbracoEditors.Dropdown.UiAlias);
        dataTypeService.ValidateConfigurationData(persisted).ShouldBeEmpty();
        JsonSerializer.SerializeToElement(persisted.ConfigurationData["items"])
            .EnumerateArray().Select(item => item.GetString())
            .ShouldBe(["csharp", "typescript", "bash"]);
    }

    public sealed class IntegrationDropdownConfiguration : UmbracoEditors.Dropdown.Configuration {
        public IntegrationDropdownConfiguration() {
            Items = JsonSerializer.SerializeToElement(new[] { "csharp", "javascript" });
            Multiple = false;
        }
    }

    public sealed class IntegrationCustomizedRichTextConfiguration : UmbracoEditors.RichTextEditor.Configuration {
        public IntegrationCustomizedRichTextConfiguration() {
            Extensions?.Remove("Umb.Tiptap.Bold");
            ToolbarRows?[0][1].Remove("Umb.Tiptap.Toolbar.Bold");
            ToolbarRows?[0][1].Add("Umb.Tiptap.Toolbar.Strike");
        }
    }

    [Fact]
    public async Task UmbracoHost_WithSqlServerContainer_PersistsGeneratedDefinitions_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();

        // Act
        await SynchronizeTestDefinitionsAsync(scope.ServiceProvider);
        var dataTypeService = scope.ServiceProvider.GetRequiredService<IDataTypeService>();
        var dataTypeContainerService =
            scope.ServiceProvider.GetRequiredService<IDataTypeContainerService>();
        var contentTypeService = scope.ServiceProvider.GetRequiredService<IContentTypeService>();
        var contentTypeContainerService =
            scope.ServiceProvider.GetRequiredService<IContentTypeContainerService>();
        var mediaTypeService = scope.ServiceProvider.GetRequiredService<IMediaTypeService>();
        var memberTypeService = scope.ServiceProvider.GetRequiredService<IMemberTypeService>();
        var mediaTypeContainerService =
            scope.ServiceProvider.GetRequiredService<IMediaTypeContainerService>();
        var memberTypeContainerService =
            scope.ServiceProvider.GetRequiredService<IMemberTypeContainerService>();
        IReadOnlyList<IDataType> dataTypes = (await dataTypeService.GetAllAsync()).ToList();
        var composedPage = contentTypeService.Get("integrationComposedPage");
        var richDocument = contentTypeService.Get("integrationRichDocument");
        var element = contentTypeService.Get("integrationElement");
        var media = mediaTypeService.Get("integrationMedia");
        var member = memberTypeService.Get("integrationMember");
        var dataTypeContainers = await dataTypeContainerService.GetAllAsync();
        var contentTypeContainers = await contentTypeContainerService.GetAllAsync();
        var dataTypeRootFolder = dataTypeContainers.Single(container =>
            container.Name == "Integration"
            && container.ParentId == -1);
        var dataTypeFolder = dataTypeContainers.Single(container =>
            container.Name == "Editors"
            && container.ParentId == dataTypeRootFolder.Id);
        var richContentTypeRootFolder = contentTypeContainers.Single(container =>
            container.Name == "Integration"
            && container.ParentId == -1);
        var richContentTypeFolder = contentTypeContainers.Single(container =>
            container.Name == "Rich"
            && container.ParentId == richContentTypeRootFolder.Id);
        var elementContentTypeFolder = contentTypeContainers.Single(container =>
            container.Name == "Elements"
            && container.ParentId == richContentTypeRootFolder.Id);
        var contentTypeRootFolder = contentTypeContainers.Single(container =>
            container.Name == "Integration"
            && container.ParentId == -1);
        var contentTypeFolder = contentTypeContainers.Single(container =>
            container.Name == "Pages"
            && container.ParentId == contentTypeRootFolder.Id);
        var mediaFolder = (await mediaTypeContainerService.GetAllAsync()).Single(container =>
            container.Name == "Media"
            && container.ParentId != -1);
        var memberFolder = (await memberTypeContainerService.GetAllAsync()).Single(container =>
            container.Name == "Members"
            && container.ParentId != -1);

        // Assert
        dataTypes.Select(dataType => dataType.Name).ShouldContain("Integration Text");
        dataTypes.Single(dataType => dataType.Name == "Integration Integer")
            .ShouldSatisfyAllConditions(
                dataType => dataType.EditorAlias.ShouldBe("Umbraco.Integer"),
                dataType => dataType.EditorUiAlias.ShouldBe("Umb.PropertyEditorUi.Integer"));
        composedPage.ShouldNotBeNull();
        dataTypeFolder.ShouldNotBeNull();
        contentTypeFolder.ShouldNotBeNull();
        dataTypes.Single(dataType => dataType.Name == "Integration Text")
            .ParentId
            .ShouldBe(dataTypeFolder.Id);
        composedPage!.ParentId.ShouldBe(contentTypeFolder.Id);
        composedPage.PropertyTypes.Select(property => property.Alias).ShouldContain("pageTitle");
        composedPage.PropertyGroups.Select(group => group.Alias).ShouldContain("page");
        composedPage.CompositionAliases().ShouldContain("integrationNestedComposition");
        composedPage.CompositionAliases().ShouldContain("integrationMetadataComposition");
        richDocument.ShouldNotBeNull();
        richDocument!.Description.ShouldBe("Initial rich document description");
        richDocument.Icon.ShouldBe("icon-document");
        richDocument.AllowedAsRoot.ShouldBeTrue();
        richDocument.ParentId.ShouldBe(richContentTypeFolder.Id);
        richDocument.PropertyTypes.Single(property => property.Alias == "headline")
            .ShouldSatisfyAllConditions(
                property => property.Name.ShouldBe("Headline"),
                property => property.Description.ShouldBe("Initial headline description"),
                property => property.Mandatory.ShouldBeTrue(),
                property => property.ValidationRegExp.ShouldBe(@"^.{3,}$"),
                property => property.ValidationRegExpMessage.ShouldBe(
                    "Headline must contain at least three characters."));
        element.ShouldNotBeNull();
        element!.IsElement.ShouldBeTrue();
        element.AllowedInLibrary.ShouldBeTrue();
        element.ParentId.ShouldBe(elementContentTypeFolder.Id);
        media.ShouldNotBeNull();
        media!.Icon.ShouldBe("icon-picture");
        media.AllowedAsRoot.ShouldBeTrue();
        media.ParentId.ShouldBe(mediaFolder.Id);
        media.PropertyTypes.Select(property => property.Alias).ShouldContain("caption");
        member.ShouldNotBeNull();
        member!.Icon.ShouldBe("icon-user");
        member.ParentId.ShouldBe(memberFolder.Id);
        member.PropertyTypes.Single(property => property.Alias == "displayName").Mandatory.ShouldBeTrue();
    }

    [Fact]
    public async Task SynchronizeAsync_TestCompositions_AreCreatedAndRemainIdempotent_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        await SynchronizeTestDefinitionsAsync(scope.ServiceProvider);
        var store = scope.ServiceProvider.GetRequiredService<ICodeFirstDefinitionStore>();
        var synchronizer = new CodeFirstSynchronizer(
            new IntegrationTestDefinitionRegistry(),
            store,
            new SynchronizationReportStore(),
            TimeProvider.System,
            scope.ServiceProvider.GetRequiredService<ILogger<CodeFirstSynchronizer>>());

        // Act
        var firstReport =
            await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
        var secondReport =
            await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
        var contentTypeService = scope.ServiceProvider.GetRequiredService<IContentTypeService>();
        var nestedComposition = contentTypeService.Get("integrationNestedComposition");
        var metadataComposition = contentTypeService.Get("integrationMetadataComposition");
        var composedPage = contentTypeService.Get("integrationComposedPage");
        var siblingPage = contentTypeService.Get("integrationSiblingPage");

        // Assert
        firstReport.FailedCount.ShouldBe(0);
        secondReport.FailedCount.ShouldBe(0);
        secondReport.Items
            .Where(item => item.Alias.StartsWith("integration", StringComparison.Ordinal))
            .ShouldAllBe(item => item.Status == SynchronizationStatus.Unchanged);
        nestedComposition.ShouldNotBeNull();
        metadataComposition.ShouldNotBeNull();
        composedPage.ShouldNotBeNull();
        siblingPage.ShouldNotBeNull();
        nestedComposition!.CompositionAliases().ShouldContain("integrationCompositionBase");
        composedPage!.CompositionAliases().ShouldContain("integrationNestedComposition");
        composedPage.CompositionAliases().ShouldContain("integrationMetadataComposition");
        siblingPage!.CompositionAliases().ShouldContain("integrationCompositionBase");
        composedPage.PropertyGroups.Select(group => group.Alias)
            .ShouldContain("page");
        siblingPage.PropertyGroups.Select(group => group.Alias)
            .ShouldContain("page");
    }

    [Fact]
    public async Task SynchronizeAsync_UpdatesOwnedTypeAndPropertyMetadata_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        await SynchronizeTestDefinitionsAsync(scope.ServiceProvider);
        var synchronizer = new CodeFirstSynchronizer(
            IntegrationTestDefinitionRegistry.CreateUpdated(),
            scope.ServiceProvider.GetRequiredService<ICodeFirstDefinitionStore>(),
            new SynchronizationReportStore(),
            TimeProvider.System,
            scope.ServiceProvider.GetRequiredService<ILogger<CodeFirstSynchronizer>>());

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
        var contentTypeService = scope.ServiceProvider.GetRequiredService<IContentTypeService>();
        var contentTypeContainerService =
            scope.ServiceProvider.GetRequiredService<IContentTypeContainerService>();
        var richDocument = contentTypeService.Get("integrationRichDocument");
        var updatedFolder = (await contentTypeContainerService.GetAllAsync()).Single(container =>
            container.Name == "Updated"
            && container.ParentId != -1);

        // Assert
        report.FailedCount.ShouldBe(0);
        report.Items.Single(item => item.Alias == "integrationRichDocument")
            .Status
            .ShouldBe(SynchronizationStatus.Updated);
        richDocument.ShouldNotBeNull();
        richDocument!.Name.ShouldBe("Updated Rich Document");
        richDocument.Description.ShouldBe("Updated rich document description");
        richDocument.Icon.ShouldBe("icon-folder");
        richDocument.AllowedAsRoot.ShouldBeFalse();
        richDocument.ParentId.ShouldBe(updatedFolder.Id);
        richDocument.PropertyTypes.Single(property => property.Alias == "headline")
            .ShouldSatisfyAllConditions(
                property => property.Name.ShouldBe("Updated headline"),
                property => property.Description.ShouldBe("Updated headline description"),
                property => property.Mandatory.ShouldBeFalse(),
                property => property.ValidationRegExp.ShouldBe(@"^.{5,}$"),
                property => property.ValidationRegExpMessage.ShouldBe(
                    "Headline must contain at least five characters."));
    }

    [Fact]
    public async Task SynchronizeAsync_PreservesUnownedContentTypeWithConflictingAlias_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var contentTypeService = scope.ServiceProvider.GetRequiredService<IContentTypeService>();
        var shortStringHelper = scope.ServiceProvider.GetRequiredService<IShortStringHelper>();
        var manualContentType = new ContentType(shortStringHelper, -1) {
            Alias = "integrationManualCollision",
            Name = "Manual Content Type",
            Description = "Owned by an editor.",
        };
        await contentTypeService.CreateAsync(manualContentType, Constants.Security.SuperUserKey);

        var registry = new TestRegistry([
            new(
                CodeFirstDefinitionKind.DocumentType,
                "integrationManualCollision",
                "Code First Replacement",
                null,
                [],
                Settings: new(Description: "Should not overwrite the manual type.")),
        ]);
        var synchronizer = new CodeFirstSynchronizer(
            registry,
            scope.ServiceProvider.GetRequiredService<ICodeFirstDefinitionStore>(),
            new SynchronizationReportStore(),
            TimeProvider.System,
            scope.ServiceProvider.GetRequiredService<ILogger<CodeFirstSynchronizer>>());

        // Act
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
        var persistedContentType = contentTypeService.Get("integrationManualCollision");

        // Assert
        report.Items.Single().Status.ShouldBe(SynchronizationStatus.Skipped);
        report.Items.Single().Message.ShouldNotBeNull().ShouldContain("not owned");
        persistedContentType.ShouldNotBeNull();
        persistedContentType!.Name.ShouldBe("Manual Content Type");
        persistedContentType.Description.ShouldBe("Owned by an editor.");
    }

    [Fact]
    public async Task SynchronizeAsync_PreservesManualPropertiesAndGroupsOnOwnedType_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        await SynchronizeTestDefinitionsAsync(scope.ServiceProvider);
        var contentTypeService = scope.ServiceProvider.GetRequiredService<IContentTypeService>();
        var dataTypeService = scope.ServiceProvider.GetRequiredService<IDataTypeService>();
        var shortStringHelper = scope.ServiceProvider.GetRequiredService<IShortStringHelper>();
        var textDataType = (await dataTypeService.GetAllAsync())
            .Single(dataType => dataType.Name == "Integration Text");
        var richDocument = contentTypeService.Get("integrationRichDocument");

        richDocument.ShouldNotBeNull();
        richDocument!.AddPropertyGroup("manual", "Manual");
        richDocument.AddPropertyType(
            new PropertyType(shortStringHelper, textDataType, "manualProperty") {
                Name = "Manual property",
            },
            "manual",
            "Manual");
        await contentTypeService.UpdateAsync(richDocument, Constants.Security.SuperUserKey);

        // Act
        await SynchronizeTestDefinitionsAsync(scope.ServiceProvider);
        var synchronizedDocument = contentTypeService.Get("integrationRichDocument");

        // Assert
        synchronizedDocument.ShouldNotBeNull();
        synchronizedDocument!.PropertyGroups.Select(group => group.Alias)
            .ShouldContain("manual");
        synchronizedDocument.PropertyTypes.Single(property => property.Alias == "manualProperty")
            .Name
            .ShouldBe("Manual property");

        var cleanup = scope.ServiceProvider.GetRequiredService<ICodeFirstCleanupService>();
        var cleanupPreview = await cleanup.PreviewAsync();
        cleanupPreview.Items.ShouldNotContain(item => item.Alias == "manualProperty");
    }

    private static async Task SynchronizeTestDefinitionsAsync(IServiceProvider services) {
        var synchronizer = new CodeFirstSynchronizer(
            new IntegrationTestDefinitionRegistry(),
            services.GetRequiredService<ICodeFirstDefinitionStore>(),
            services.GetRequiredService<ISynchronizationReportStore>(),
            TimeProvider.System,
            services.GetRequiredService<ILogger<CodeFirstSynchronizer>>());
        var report = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
        report.FailedCount.ShouldBe(0);
    }

    private sealed class TestRegistry(IReadOnlyList<CodeFirstDefinition> definitions)
        : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = definitions;
    }
}
