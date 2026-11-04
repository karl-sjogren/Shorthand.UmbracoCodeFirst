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
        var contentTypeService = scope.ServiceProvider.GetRequiredService<IContentTypeService>();
        IReadOnlyList<IDataType> dataTypes = (await dataTypeService.GetAllAsync()).ToList();
        var composedPage = contentTypeService.Get("integrationComposedPage");

        // Assert
        dataTypes.Select(dataType => dataType.Name).ShouldContain("Integration Text");
        composedPage.ShouldNotBeNull();
        composedPage!.PropertyTypes.Select(property => property.Alias).ShouldContain("pageTitle");
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
