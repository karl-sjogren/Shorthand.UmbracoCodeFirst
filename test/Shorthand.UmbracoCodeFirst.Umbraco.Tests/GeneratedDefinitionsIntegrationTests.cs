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
using Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

[Trait("Category", "Integration")]
[Collection("Umbraco integration")]
public class GeneratedDefinitionsIntegrationTests(UmbracoIntegrationFixture fixture)
    : IClassFixture<UmbracoIntegrationFixture> {
    [Fact]
    public async Task AppRegistry_AllowedChildren_ArePersistedAndRestored_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            return;
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var registry = new GeneratedCodeFirstDefinitionRegistry();
        var startDefinition = registry.Definitions.Single(definition => definition.Alias == StartPage.ModelTypeAlias);
        var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
        var synchronizer = new CodeFirstSynchronizer(
            registry, store, new SynchronizationReportStore(), TimeProvider.System,
            services.GetRequiredService<ILogger<CodeFirstSynchronizer>>());
        var contentTypes = services.GetRequiredService<IContentTypeService>();
        var token = TestContext.Current.CancellationToken;

        // Act
        var initialReport = await synchronizer.SynchronizeAsync(token);
        var firstChildren = contentTypes.Get(StartPage.ModelTypeAlias)?.AllowedContentTypes?.ToArray();
        var cleared = await store.ApplyAsync(startDefinition with {
            Settings = startDefinition.Settings! with { AllowedChildAliases = [] },
        }, token);
        var restartedStore = ActivatorUtilities.CreateInstance<UmbracoCodeFirstDefinitionStore>(services);
        var restartedSynchronizer = new CodeFirstSynchronizer(
            registry, restartedStore, new SynchronizationReportStore(), TimeProvider.System,
            services.GetRequiredService<ILogger<CodeFirstSynchronizer>>());
        var restoredReport = await restartedSynchronizer.SynchronizeAsync(token);
        var restoredChildren = contentTypes.Get(StartPage.ModelTypeAlias)?.AllowedContentTypes?.ToArray();
        var repeatedReport = await restartedSynchronizer.SynchronizeAsync(token);
        var child = contentTypes.Get(BlogPost.ModelTypeAlias);

        // Assert
        startDefinition.Settings!.AllowedChildAliases.ShouldBe([BlogPost.ModelTypeAlias]);
        initialReport.FailedCount.ShouldBe(0);
        firstChildren.ShouldNotBeNull().Select(item => item.Alias).ShouldBe([BlogPost.ModelTypeAlias]);
        cleared.Status.ShouldBe(SynchronizationStatus.Updated);
        restoredReport.FailedCount.ShouldBe(0);
        restoredReport.Items.Single(item => item.Alias == StartPage.ModelTypeAlias).Status.ShouldBe(SynchronizationStatus.Updated);
        child.ShouldNotBeNull();
        restoredChildren.ShouldNotBeNull().Single().ShouldSatisfyAllConditions(
            item => item.Alias.ShouldBe(BlogPost.ModelTypeAlias),
            item => item.Key.ShouldBe(child.Key));
        repeatedReport.Items.Single(item => item.Alias == StartPage.ModelTypeAlias).Status.ShouldBe(SynchronizationStatus.Unchanged);
    }

    [Fact]
    public async Task AppRegistry_CodeBlockDependencies_SynchronizeAndPersist_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            return;
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
        var blockList = (await dataTypes.GetAllAsync()).Single(item => item.Name == "Blog Post Content");
        JsonSerializer.SerializeToElement(blockList.ConfigurationData["blocks"]).EnumerateArray()
            .ShouldContain(block => block.GetProperty("contentElementTypeKey").GetGuid() == element.Key);
    }

    [Fact]
    public async Task Store_DropdownConfiguration_CreatesAndUpdatesDataType_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            return;
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

    [Fact]
    public async Task UmbracoHost_WithSqlServerContainer_PersistsGeneratedDefinitions_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            return;
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
            return;
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
            return;
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
            return;
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
            return;
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
