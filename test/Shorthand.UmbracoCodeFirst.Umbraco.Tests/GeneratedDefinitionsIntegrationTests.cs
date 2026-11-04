using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Shorthand.UmbracoCodeFirst.Umbraco.Tests.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

[Trait("Category", "Integration")]
public class GeneratedDefinitionsIntegrationTests(UmbracoIntegrationFixture fixture)
    : IClassFixture<UmbracoIntegrationFixture> {
    [Fact]
    public async Task UmbracoHost_WithSqlServerContainer_PersistsGeneratedDefinitions_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            return;
        }

        // Act
        using var scope = fixture.Services.CreateScope();
        await SynchronizeTestDefinitionsAsync(scope.ServiceProvider);
        var dataTypeService = scope.ServiceProvider.GetRequiredService<IDataTypeService>();
        var dataTypeContainerService =
            scope.ServiceProvider.GetRequiredService<IDataTypeContainerService>();
        var contentTypeService = scope.ServiceProvider.GetRequiredService<IContentTypeService>();
        var contentTypeContainerService =
            scope.ServiceProvider.GetRequiredService<IContentTypeContainerService>();
        IReadOnlyList<IDataType> dataTypes = (await dataTypeService.GetAllAsync()).ToList();
        var composedPage = contentTypeService.Get("integrationComposedPage");
        var dataTypeContainers = await dataTypeContainerService.GetAllAsync();
        var contentTypeContainers = await contentTypeContainerService.GetAllAsync();
        var dataTypeRootFolder = dataTypeContainers.Single(container =>
            container.Name == "Integration"
            && container.ParentId == -1);
        var dataTypeFolder = dataTypeContainers.Single(container =>
            container.Name == "Editors"
            && container.ParentId == dataTypeRootFolder.Id);
        var contentTypeRootFolder = contentTypeContainers.Single(container =>
            container.Name == "Integration"
            && container.ParentId == -1);
        var contentTypeFolder = contentTypeContainers.Single(container =>
            container.Name == "Pages"
            && container.ParentId == contentTypeRootFolder.Id);

        // Assert
        dataTypes.Select(dataType => dataType.Name).ShouldContain("Integration Text");
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
}
