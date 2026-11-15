using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.DocumentTypes;
using Shorthand.UmbracoCodeFirst.Umbraco.Models.MediaTypes;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Xunit.Sdk;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

[Trait("Category", "Integration")]
[Collection("Umbraco integration")]
public class CollectionIntegrationTests(UmbracoIntegrationFixture fixture) : IClassFixture<UmbracoIntegrationFixture> {
    [Theory]
    [InlineData(CodeFirstDefinitionKind.DocumentType)]
    [InlineData(CodeFirstDefinitionKind.MediaType)]
    public async Task Store_ManagesCollectionWithoutReplacingManualAssociation_Async(CodeFirstDefinitionKind kind) {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
        var dataTypes = services.GetRequiredService<IDataTypeService>();
        var contentTypes = services.GetRequiredService<IContentTypeService>();
        var mediaTypes = services.GetRequiredService<IMediaTypeService>();
        var alias = kind == CodeFirstDefinitionKind.DocumentType ? "integrationCollectionDocument" : "integrationCollectionMedia";
        var definition = new CodeFirstDefinition(
            kind, alias, alias, null, [], new CodeFirstTypeSettings(
                Collection: CodeFirstCollectionConfiguration.FromType(typeof(TestCollection), kind)));
        var token = TestContext.Current.CancellationToken;

        // Act
        var created = await store.ApplyAsync(definition, token);
        var restartedStore = ActivatorUtilities.CreateInstance<UmbracoCodeFirstDefinitionStore>(services);
        var repeated = await restartedStore.ApplyAsync(definition, token);
        var parent = GetParent();
        var dataType = (await dataTypes.GetAllAsync()).Single(item => item.Key == parent.ListView);
        var siblingAlias = alias + "Sibling";
        var sibling = definition with { Alias = siblingAlias, Name = siblingAlias };
        await restartedStore.ApplyAsync(sibling, token);
        var siblingParent = kind == CodeFirstDefinitionKind.DocumentType
            ? (IContentTypeComposition)contentTypes.Get(siblingAlias)!
            : mediaTypes.Get(siblingAlias)!;
        var updatedDefinition = definition with {
            Settings = definition.Settings! with {
                Collection = CodeFirstCollectionConfiguration.FromType(typeof(UpdatedCollection), kind),
            },
        };
        var updated = await store.ApplyAsync(updatedDefinition, token);
        var repeatedUpdate = await store.ApplyAsync(updatedDefinition, token);
        var savedDataType = (await dataTypes.GetAllAsync()).Single(item => item.Key == dataType.Key);
        var user = await services.GetRequiredService<IUserService>().GetAsync(Constants.Security.SuperUserKey)
            ?? throw new InvalidOperationException("The integration user was not found.");
        if(kind == CodeFirstDefinitionKind.DocumentType) {
            var contentService = services.GetRequiredService<IContentService>();
            var content = contentService.Create("Collection parent", -1, alias);
            contentService.Save(content).Success.ShouldBeTrue();
            contentService.Save(contentService.Create("Beta", content.Id, alias)).Success.ShouldBeTrue();
            contentService.Save(contentService.Create("Alpha", content.Id, alias)).Success.ShouldBeTrue();
            var result = await services.GetRequiredService<IContentListViewService>().GetListViewItemsByKeyAsync(
                user, content.Key, null, "name", null, Direction.Ascending, null, 0, 1);
            result.Success.ShouldBeTrue();
            result.Result.ShouldNotBeNull().Items.Items.Single().Name.ShouldBe("Alpha");
        } else {
            var mediaService = services.GetRequiredService<IMediaService>();
            var media = mediaService.CreateMedia("Collection parent", -1, alias);
            mediaService.Save(media).Success.ShouldBeTrue();
            mediaService.Save(mediaService.CreateMedia("Beta", media.Id, alias)).Success.ShouldBeTrue();
            mediaService.Save(mediaService.CreateMedia("Alpha", media.Id, alias)).Success.ShouldBeTrue();
            var result = await services.GetRequiredService<IMediaListViewService>().GetListViewItemsByKeyAsync(
                user, media.Key, null, "name", Direction.Ascending, null, 0, 1);
            result.Success.ShouldBeTrue();
            result.Result.ShouldNotBeNull().Items.Items.Single().Name.ShouldBe("Alpha");
        }

        var invalidDefinition = definition with {
            Settings = definition.Settings! with {
                Collection = new CodeFirstCollectionSettings("{}", false, false, 0, "Invalid layouts"),
            },
        };
        await Should.ThrowAsync<InvalidOperationException>(async () => await store.ApplyAsync(invalidDefinition, token));
        GetParent().ListView.ShouldBe(dataType.Key);
        var detached = await store.ApplyAsync(updatedDefinition with {
            Settings = updatedDefinition.Settings! with { Collection = null },
        }, token);

        // Assert
        created.Status.ShouldBe(SynchronizationStatus.Created);
        repeated.Status.ShouldBe(SynchronizationStatus.Unchanged);
        siblingParent.ListView.ShouldNotBe(dataType.Key);
        siblingParent.ListView.ShouldNotBeNull();
        updated.Status.ShouldBe(SynchronizationStatus.Updated);
        repeatedUpdate.Status.ShouldBe(SynchronizationStatus.Unchanged);
        detached.Status.ShouldBe(SynchronizationStatus.Updated);
        GetParent().ListView.ShouldBeNull();
        (await dataTypes.GetAllAsync()).Any(item => item.Key == dataType.Key).ShouldBeTrue();
        JsonSerializer.SerializeToElement(savedDataType.ConfigurationData["pageSize"]).GetInt32().ShouldBe(30);
        dataTypes.ValidateConfigurationData(savedDataType).ShouldBeEmpty();
        var columns = JsonSerializer.SerializeToElement(savedDataType.ConfigurationData["includeProperties"]);
        columns[1].GetProperty("nameTemplate").GetString().ShouldBe("{=value}");
        var layouts = JsonSerializer.SerializeToElement(savedDataType.ConfigurationData["layouts"]);
        var entity = kind == CodeFirstDefinitionKind.DocumentType ? "Document" : "Media";
        layouts[0].GetProperty("collectionView").GetString().ShouldBe($"Umb.CollectionView.{entity}.Grid");
        var configuration = savedDataType.ConfigurationObject.ShouldBeOfType<ListViewConfiguration>();
        configuration.IncludeProperties.Select(column => column.Alias).ShouldContain("summary");
        configuration.IncludeProperties.Single(column => column.Alias == "updateDate").IsSystem.ShouldBeTrue();
        configuration.IncludeProperties.Single(column => column.Alias == "summary").IsSystem.ShouldBeFalse();

        // Arrange
        var manual = (await dataTypes.GetAllAsync()).First(item =>
            item.EditorAlias == "Umbraco.ListView" && item.Key != dataType.Key);
        parent = GetParent();
        parent.ListView = manual.Key;
        if(kind == CodeFirstDefinitionKind.DocumentType) {
            (await contentTypes.UpdateAsync((IContentType)parent, Constants.Security.SuperUserKey)).Success.ShouldBeTrue();
        } else {
            (await mediaTypes.UpdateAsync((IMediaType)parent, Constants.Security.SuperUserKey)).Success.ShouldBeTrue();
        }

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await store.ApplyAsync(definition, token));
        GetParent().ListView.ShouldBe(manual.Key);
        await store.ApplyAsync(definition with { Settings = definition.Settings! with { Collection = null } }, token);
        GetParent().ListView.ShouldBe(manual.Key);

        IContentTypeComposition GetParent() =>
            kind == CodeFirstDefinitionKind.DocumentType
                ? contentTypes.Get(alias)!
                : mediaTypes.Get(alias)!;
    }

    [Theory]
    [InlineData(CodeFirstDefinitionKind.DocumentType, "seoCompositeTitle")]
    [InlineData(CodeFirstDefinitionKind.MediaType, "caption")]
    public async Task Store_PersistsTypedCollectionAliasesAsync(CodeFirstDefinitionKind kind, string expectedAlias) {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
        var dataTypes = services.GetRequiredService<IDataTypeService>();
        var alias = kind == CodeFirstDefinitionKind.DocumentType ? "typedCollectionDocument" : "typedCollectionMedia";
        var configurationType = kind == CodeFirstDefinitionKind.DocumentType
            ? typeof(DocumentTypedCollection)
            : typeof(MediaTypedCollection);
        var configuration = CodeFirstCollectionConfiguration.FromType(configurationType, kind);
        configuration.Error.ShouldBeNull();
        var definition = new CodeFirstDefinition(
            kind, alias, alias, null, [], new CodeFirstTypeSettings(Collection: configuration));

        // Act
        await store.ApplyAsync(definition, TestContext.Current.CancellationToken);
        var parent = kind == CodeFirstDefinitionKind.DocumentType
            ? (IContentTypeComposition)services.GetRequiredService<IContentTypeService>().Get(alias)!
            : services.GetRequiredService<IMediaTypeService>().Get(alias)!;
        var dataType = (await dataTypes.GetAllAsync()).Single(item => item.Key == parent.ListView);
        var columns = JsonSerializer.SerializeToElement(dataType.ConfigurationData["includeProperties"]);
        var orderBy = JsonSerializer.SerializeToElement(dataType.ConfigurationData["orderBy"]);

        // Assert
        columns[0].GetProperty("alias").GetString().ShouldBe(expectedAlias);
        columns[0].GetProperty("isSystem").GetInt32().ShouldBe(0);
        columns[1].GetProperty("alias").GetString().ShouldBe("updateDate");
        columns[1].GetProperty("isSystem").GetInt32().ShouldBe(1);
        orderBy.GetString().ShouldBe(expectedAlias);
    }

    private sealed class DocumentTypedCollection : CodeFirstCollectionConfiguration<SeoComposite> {
        public DocumentTypedCollection() {
            OrderBy = Alias(model => model.SeoCompositeTitle);
            Columns = [
                new(SeoComposite.PropertyAliases.SeoCompositeTitle, "SEO title"),
                new(CodeFirstCollectionSystemFields.UpdateDate, "Updated"),
            ];
        }
    }

    private sealed class MediaTypedCollection : CodeFirstCollectionConfiguration<ImageAsset> {
        public MediaTypedCollection() {
            OrderBy = ImageAsset.PropertyAliases.Caption;
            Columns = [
                new(Alias(model => model.Caption), "Caption"),
                new(CodeFirstCollectionSystemFields.UpdateDate, "Updated"),
            ];
        }
    }

    private sealed class TestCollection : CodeFirstCollectionConfiguration {
        public TestCollection() {
            Columns = [new("updateDate", "Updated"), new("summary", "Summary", NameTemplate: "{=value}")];
            OrderBy = "updateDate";
            Grid = false;
        }
    }

    private sealed class UpdatedCollection : CodeFirstCollectionConfiguration {
        public UpdatedCollection() {
            Columns = [new("updateDate", "Updated"), new("summary", "Summary", NameTemplate: "{=value}")];
            OrderBy = "updateDate";
            PageSize = 30;
            List = false;
        }
    }
}
