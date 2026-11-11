using System.Text.Json;
using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Umbraco.Cms.Core.Services;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Tests;

public class CodeFirstOwnershipStoreTests {
    [Fact]
    public void Set_PersistsDocumentTypeOwnershipOutsideTheDocumentTypeDescription() {
        // Arrange
        var keyValueService = A.Fake<IKeyValueService>();
        var store = new CodeFirstOwnershipStore(keyValueService);

        // Act
        store.Set("productPage", Guid.Parse("11111111-1111-1111-1111-111111111111"), ["article"]);

        // Assert
        A.CallTo(() => keyValueService.SetValue(
                "shorthand.umbraco-codefirst:content-type:productPage",
                A<string>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => keyValueService.SetValue(
                "shorthand.umbraco-codefirst:content-type:productPage",
                JsonSerializer.Serialize(new CodeFirstDocumentTypeOwnership(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "productPage",
                    new[] { "article" }))))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void Get_IgnoresOwnershipForAnotherDocumentTypeKey() {
        // Arrange
        var keyValueService = A.Fake<IKeyValueService>();
        A.CallTo(() => keyValueService.GetValue(
                "shorthand.umbraco-codefirst:content-type:productPage"))
            .Returns(JsonSerializer.Serialize(new CodeFirstDocumentTypeOwnership(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "productPage",
                ["article"])));
        var store = new CodeFirstOwnershipStore(keyValueService);

        // Act
        var result = store.Get(
            "productPage",
            Guid.Parse("22222222-2222-2222-2222-222222222222"));

        // Assert
        result.ShouldBeNull();
    }
}
