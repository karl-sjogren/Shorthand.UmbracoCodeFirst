using Microsoft.Extensions.DependencyInjection;
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
        var dataTypeService = scope.ServiceProvider.GetRequiredService<IDataTypeService>();
        var contentTypeService = scope.ServiceProvider.GetRequiredService<IContentTypeService>();
        IReadOnlyList<IDataType> dataTypes = (await dataTypeService.GetAllAsync()).ToList();
        var productPage = contentTypeService.Get("productPage");

        // Assert
        dataTypes.Select(dataType => dataType.EditorAlias).ShouldContain("Umbraco.TextBox");
        dataTypes.Select(dataType => dataType.EditorAlias).ShouldContain("Umbraco.Decimal");
        productPage.ShouldNotBeNull();
        productPage!.PropertyTypes.Select(property => property.Alias).ShouldContain("description");
        productPage.PropertyGroups.Select(group => group.Alias).ShouldContain("pricing");
        productPage.CompositionAliases().ShouldContain("article");
    }
}
