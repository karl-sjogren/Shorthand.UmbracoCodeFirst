using Shorthand.UmbracoCodeFirst.Backoffice.Cleanup;
using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Shorthand.UmbracoCodeFirst.Metadata;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Tests;

public class CodeFirstCleanupServiceTests {
    [Fact]
    public async Task PreviewAsync_WhenNoOwnedItemsAreObsolete_ReturnsEmptyCleanupList_Async() {
        var dataTypeService = A.Fake<IDataTypeService>();
        var contentTypeService = A.Fake<IContentTypeService>();
        A.CallTo(() => dataTypeService.GetAllAsync())
            .Returns(Task.FromResult<IEnumerable<IDataType>>([]));
        A.CallTo(() => contentTypeService.GetAll()).Returns([]);
        var service = CreateService(dataTypeService, contentTypeService, []);

        var result = await service.PreviewAsync();

        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task PreviewAsync_WhenOwnedContentTypeHasObsoleteMembers_ReturnsPropertiesAndGroups_Async() {
        var dataTypeService = A.Fake<IDataTypeService>();
        var contentTypeService = A.Fake<IContentTypeService>();
        A.CallTo(() => dataTypeService.GetAllAsync())
            .Returns(Task.FromResult<IEnumerable<IDataType>>([]));
        var contentType = CreateContentType(
            "productPage",
            "Product Page",
            "shorthand.umbraco-codefirst:productPage");
        AddProperty(contentType, "current", "Current", "currentGroup");
        AddProperty(contentType, "obsolete", "Obsolete", "obsoleteGroup");
        contentType.AddPropertyGroup("obsoleteEmpty", "Obsolete empty");
        A.CallTo(() => contentTypeService.GetAll())
            .Returns([AsContentType(contentType)]);
        var service = CreateService(
            dataTypeService,
            contentTypeService,
            [new(
                CodeFirstDefinitionKind.DocumentType,
                "productPage",
                "Product Page",
                null,
                [new("current", "Current", "text", GroupAlias: "currentGroup")])]);

        var result = await service.PreviewAsync();

        result.Items.ShouldBe([
            new("Property", "obsolete", "Obsolete", "productPage"),
            new("Group", "obsoleteGroup", "obsoleteGroup", "productPage"),
            new("Group", "obsoleteEmpty", "Obsolete empty", "productPage"),
        ]);
    }

    [Fact]
    public async Task PreviewAsync_IgnoresUnownedContentTypesAndReportsObsoleteDataTypes_Async() {
        var dataTypeService = A.Fake<IDataTypeService>();
        var contentTypeService = A.Fake<IContentTypeService>();
        var dataType = A.Fake<IDataType>();
        A.CallTo(() => dataType.ConfigurationData)
            .Returns(new Dictionary<string, object> {
                ["shorthand.umbraco-codefirst"] = "obsoleteText",
            });
        A.CallTo(() => dataType.Name).Returns("Obsolete text");
        A.CallTo(() => dataTypeService.GetAllAsync())
            .Returns(Task.FromResult<IEnumerable<IDataType>>([dataType]));
        var manualContentType = CreateContentType("manual", "Manual", null);
        A.CallTo(() => contentTypeService.GetAll())
            .Returns([AsContentType(manualContentType)]);
        var service = CreateService(dataTypeService, contentTypeService, []);

        var result = await service.PreviewAsync();

        result.Items.ShouldBe([
            new("DataType", "obsoleteText", "Obsolete text"),
        ]);
    }

    [Fact]
    public async Task PreviewAsync_ReportsOwnedContentTypeMissingFromRegistry_Async() {
        var dataTypeService = A.Fake<IDataTypeService>();
        var contentTypeService = A.Fake<IContentTypeService>();
        A.CallTo(() => dataTypeService.GetAllAsync())
            .Returns(Task.FromResult<IEnumerable<IDataType>>([]));
        var contentType = CreateContentType(
            "obsoletePage",
            "Obsolete Page",
            "shorthand.umbraco-codefirst:obsoletePage");
        A.CallTo(() => contentTypeService.GetAll())
            .Returns([AsContentType(contentType)]);
        var service = CreateService(dataTypeService, contentTypeService, []);

        var result = await service.PreviewAsync();

        result.Items.ShouldBe([
            new("ContentType", "obsoletePage", "Obsolete Page"),
        ]);
    }

    [Fact]
    public async Task ApplyAsync_WhenOwnedContentTypeIsObsolete_DeletesIt_Async() {
        var dataTypeService = A.Fake<IDataTypeService>();
        var contentTypeService = A.Fake<IContentTypeService>();
        A.CallTo(() => dataTypeService.GetAllAsync())
            .Returns(Task.FromResult<IEnumerable<IDataType>>([]));
        var contentType = CreateContentType(
            "obsoletePage",
            "Obsolete Page",
            "shorthand.umbraco-codefirst:obsoletePage");
        var persistedContentType = AsContentType(contentType);
        A.CallTo(() => contentTypeService.GetAll()).Returns([persistedContentType]);
        A.CallTo(() => contentTypeService.Get("obsoletePage")).Returns(persistedContentType);
        A.CallTo(() => contentTypeService.DeleteAsync(
                persistedContentType.Key,
                A<Guid>._))
            .Returns(Umbraco.Cms.Core.Services.OperationStatus.ContentTypeOperationStatus.Success);
        var service = CreateService(dataTypeService, contentTypeService, []);

        var result = await service.ApplyAsync();

        result.Removed.ShouldBe([
            new("ContentType", "obsoletePage", "Obsolete Page"),
        ]);
        result.Failed.ShouldBeEmpty();
        A.CallTo(() => contentTypeService.DeleteAsync(
                persistedContentType.Key,
                A<Guid>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task PreviewAsync_PreservesCompositionPropertiesAndGroups_Async() {
        var dataTypeService = A.Fake<IDataTypeService>();
        var contentTypeService = A.Fake<IContentTypeService>();
        A.CallTo(() => dataTypeService.GetAllAsync())
            .Returns(Task.FromResult<IEnumerable<IDataType>>([]));
        var contentType = CreateContentType(
            "productPage",
            "Product Page",
            "shorthand.umbraco-codefirst:productPage");
        AddProperty(contentType, "composed", "Composed", "shared");
        AddProperty(contentType, "obsolete", "Obsolete", "obsolete");
        var persistedContentType = AsContentType(
            contentType,
            contentType.PropertyTypes.Where(property => property.Alias == "composed"),
            contentType.PropertyGroups.Where(group => group.Alias == "shared"));
        A.CallTo(() => contentTypeService.GetAll()).Returns([persistedContentType]);
        var service = CreateService(
            dataTypeService,
            contentTypeService,
            [new(
                CodeFirstDefinitionKind.DocumentType,
                "productPage",
                "Product Page",
                null,
                [new("composed", "Composed", "text")])]);

        var result = await service.PreviewAsync();

        result.Items.ShouldBe([
            new("Property", "obsolete", "Obsolete", "productPage"),
            new("Group", "obsolete", "obsolete", "productPage"),
        ]);
    }

    [Fact]
    public async Task ApplyAsync_WhenObsoletePropertyAndGroupExist_RemovesBoth_Async() {
        var dataTypeService = A.Fake<IDataTypeService>();
        var contentTypeService = A.Fake<IContentTypeService>();
        A.CallTo(() => dataTypeService.GetAllAsync())
            .Returns(Task.FromResult<IEnumerable<IDataType>>([]));
        var contentType = CreateContentType(
            "productPage",
            "Product Page",
            "shorthand.umbraco-codefirst:productPage");
        AddProperty(contentType, "obsolete", "Obsolete", "obsoletePropertyGroup");
        contentType.AddPropertyGroup("obsoleteGroup", "Obsolete group");
        var persistedContentType = AsContentType(contentType);
        A.CallTo(() => contentTypeService.GetAll()).Returns([persistedContentType]);
        A.CallTo(() => contentTypeService.Get("productPage")).Returns(persistedContentType);
        var service = CreateService(
            dataTypeService,
            contentTypeService,
            [new(CodeFirstDefinitionKind.DocumentType, "productPage", "Product Page", null, [])]);

        var result = await service.ApplyAsync();

        result.Failed.ShouldBeEmpty();
        result.Removed.ShouldBe([
            new("Property", "obsolete", "Obsolete", "productPage"),
            new("Group", "obsoleteGroup", "Obsolete group", "productPage"),
        ]);
        result.RemovedCount.ShouldBe(2);
        A.CallTo(() => persistedContentType.RemovePropertyType("obsolete"))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => persistedContentType.RemovePropertyGroup("obsoleteGroup"))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => contentTypeService.UpdateAsync(persistedContentType, A<Guid>._))
            .MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public async Task ApplyAsync_WhenUpdateFails_ReportsEachFailure_Async() {
        var dataTypeService = A.Fake<IDataTypeService>();
        var contentTypeService = A.Fake<IContentTypeService>();
        A.CallTo(() => dataTypeService.GetAllAsync())
            .Returns(Task.FromResult<IEnumerable<IDataType>>([]));
        var contentType = CreateContentType(
            "productPage",
            "Product Page",
            "shorthand.umbraco-codefirst:productPage");
        AddProperty(contentType, "obsolete", "Obsolete", "obsoleteProperty");
        contentType.AddPropertyGroup("obsoleteGroup", "Obsolete group");
        var persistedContentType = AsContentType(contentType);
        A.CallTo(() => contentTypeService.GetAll()).Returns([persistedContentType]);
        A.CallTo(() => contentTypeService.Get("productPage")).Returns(persistedContentType);
        A.CallTo(() => contentTypeService.UpdateAsync(persistedContentType, A<Guid>._))
            .Throws(new InvalidOperationException("update failed"));
        var service = CreateService(
            dataTypeService,
            contentTypeService,
            [new(CodeFirstDefinitionKind.DocumentType, "productPage", "Product Page", null, [])]);

        var result = await service.ApplyAsync();

        result.RemovedCount.ShouldBe(0);
        result.Failed.Count.ShouldBe(2);
        result.Failed.Select(failure => failure.Message)
            .ShouldAllBe(message => message == "update failed");
    }

    private static CodeFirstCleanupService CreateService(
        IDataTypeService dataTypeService,
        IContentTypeService contentTypeService,
        IReadOnlyList<CodeFirstDefinition> definitions) {
        var keyValueService = A.Fake<IKeyValueService>();
        A.CallTo(() => keyValueService.GetValue(A<string>._)).Returns(null);
        return new(
            new TestRegistry(definitions),
            dataTypeService,
            contentTypeService,
            new CodeFirstOwnershipStore(keyValueService));
    }

    private static ContentType CreateContentType(
        string alias,
        string name,
        string? description) {
        var contentType = new ContentType(CreateShortStringHelper(), -1) {
            Alias = alias,
            Name = name,
            Description = description,
        };
        return contentType;
    }

    private static void AddProperty(
        ContentType contentType,
        string alias,
        string name,
        string groupAlias) {
        if(contentType.PropertyGroups.All(group => group.Alias != groupAlias)) {
            contentType.AddPropertyGroup(groupAlias, groupAlias);
        }

        var propertyType = new PropertyType(
            CreateShortStringHelper(),
            A.Fake<IDataType>(),
            alias) {
            Name = name,
        };
        contentType.AddPropertyType(propertyType, groupAlias, groupAlias);
    }

    private static IContentType AsContentType(
        ContentType contentType,
        IEnumerable<IPropertyType>? compositionPropertyTypes = null,
        IEnumerable<PropertyGroup>? compositionPropertyGroups = null) {
        var persistedContentType = A.Fake<IContentType>();
        A.CallTo(() => persistedContentType.Alias).Returns(contentType.Alias);
        A.CallTo(() => persistedContentType.Name).Returns(contentType.Name);
        A.CallTo(() => persistedContentType.Description).Returns(contentType.Description);
        A.CallTo(() => persistedContentType.PropertyTypes).Returns(contentType.PropertyTypes);
        A.CallTo(() => persistedContentType.CompositionPropertyTypes)
            .Returns(compositionPropertyTypes ?? []);
        A.CallTo(() => persistedContentType.PropertyGroups).Returns(contentType.PropertyGroups);
        A.CallTo(() => persistedContentType.CompositionPropertyGroups)
            .Returns(compositionPropertyGroups ?? []);
        return persistedContentType;
    }

    private static IShortStringHelper CreateShortStringHelper() {
        var helper = A.Fake<IShortStringHelper>();
        A.CallTo(() => helper.CleanStringForSafeAlias(A<string>._))
            .ReturnsLazily((string value) => value);
        A.CallTo(() => helper.CleanString(A<string>._, A<CleanStringType>._))
            .ReturnsLazily((string value, CleanStringType _) => value);
        return helper;
    }

    private sealed class TestRegistry(IReadOnlyList<CodeFirstDefinition> definitions)
        : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = definitions;
    }
}
