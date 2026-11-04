using Shorthand.UmbracoCodeFirst.Backoffice.Controllers;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Tests;

public class ShorthandUmbracoCodeFirstBackofficeApiControllerTests {
    [Fact]
    public void Ping_ReturnsPong() {
        new ShorthandUmbracoCodeFirstBackofficeApiController()
            .Ping()
            .ShouldBe("Pong");
    }

    [Fact]
    public async Task StatusAsync_MapsOwnedDataTypeAndDocumentTypeUrls_Async() {
        // Arrange
        var dataTypeKey = Guid.NewGuid();
        var documentTypeKey = Guid.NewGuid();
        var dataType = A.Fake<IDataType>();
        A.CallTo(() => dataType.Key).Returns(dataTypeKey);
        A.CallTo(() => dataType.ConfigurationData)
            .Returns(new Dictionary<string, object> {
                ["shorthand.umbraco-codefirst"] = "text",
            });
        var dataTypeService = A.Fake<IDataTypeService>();
        A.CallTo(() => dataTypeService.GetAllAsync())
            .Returns(Task.FromResult<IEnumerable<IDataType>>([dataType]));

        var contentType = A.Fake<IContentType>();
        A.CallTo(() => contentType.Key).Returns(documentTypeKey);
        var contentTypeService = A.Fake<IContentTypeService>();
        A.CallTo(() => contentTypeService.Get("article")).Returns(contentType);

        var registry = new TestRegistry([
            new(CodeFirstDefinitionKind.DataType, "text", "Text", "Umbraco.TextBox", []),
            new(CodeFirstDefinitionKind.DocumentType, "article", "Article", null, []),
        ]);
        var reportStore = new SynchronizationReportStore();

        // Act
        var response = await new ShorthandUmbracoCodeFirstBackofficeApiController()
            .StatusAsync(registry, reportStore, dataTypeService, contentTypeService);

        // Assert
        response.DataTypes.Single().BackofficeUrl.ShouldBe(
            $"/umbraco/section/settings/workspace/data-type/edit/{dataTypeKey:D}");
        response.DocumentTypes.Single().BackofficeUrl.ShouldBe(
            $"/umbraco/section/settings/workspace/document-type/edit/{documentTypeKey:D}");
    }

    private sealed class TestRegistry(IReadOnlyList<CodeFirstDefinition> definitions)
        : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = definitions;
    }
}
