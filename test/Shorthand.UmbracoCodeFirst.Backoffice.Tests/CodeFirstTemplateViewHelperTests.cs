using System.Text;
using Microsoft.Extensions.Options;
using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Shorthand.UmbracoCodeFirst.Metadata;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Tests;

public class CodeFirstTemplateViewHelperTests {
    [Fact]
    public void CreateView_NestedTemplate_ReadsOriginalWithoutWritingAliasFile() {
        // Arrange
        var template = A.Fake<ITemplate>();
        var inner = A.Fake<IViewHelper>();
        var paths = A.Fake<ICodeFirstTemplatePaths>();
        var views = A.Fake<IFileSystem>();
        A.CallTo(() => paths.GetPathForSave(template)).Returns("Stuff/Page.cshtml");
        A.CallTo(() => paths.Views).Returns(views);
        A.CallTo(() => views.OpenFile("Stuff/Page.cshtml"))
            .ReturnsLazily(() => new MemoryStream(Encoding.UTF8.GetBytes("Nested view")));
        var helper = new CodeFirstTemplateViewHelper(inner, paths);

        // Act
        var result = helper.CreateView(template, true);

        // Assert
        result.ShouldBe("Nested view");
        A.CallTo(() => inner.CreateView(A<ITemplate>._, A<bool>._)).MustNotHaveHappened();
        A.CallTo(() => views.AddFile(A<string>._, A<Stream>._, A<bool>._)).MustNotHaveHappened();
    }

    [Fact]
    public void UpdateViewFile_UnownedTemplate_DelegatesToUmbraco() {
        // Arrange
        var template = A.Fake<ITemplate>();
        var inner = A.Fake<IViewHelper>();
        var paths = A.Fake<ICodeFirstTemplatePaths>();
        A.CallTo(() => paths.GetPathForSave(template)).Returns(null);
        A.CallTo(() => paths.GetVerifiedPath("manual")).Returns(null);
        A.CallTo(() => inner.UpdateViewFile(template, "manual")).Returns("Manual");
        var helper = new CodeFirstTemplateViewHelper(inner, paths);

        // Act
        var result = helper.UpdateViewFile(template, "manual");

        // Assert
        result.ShouldBe("Manual");
        A.CallTo(() => inner.UpdateViewFile(template, "manual")).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void UpdateViewFile_NestedTemplateWithUnchangedContent_DoesNotRewriteSource() {
        // Arrange
        var template = A.Fake<ITemplate>();
        var inner = A.Fake<IViewHelper>();
        var paths = A.Fake<ICodeFirstTemplatePaths>();
        var views = A.Fake<IFileSystem>();
        A.CallTo(() => template.Content).Returns("Nested view");
        A.CallTo(() => paths.GetPathForSave(template)).Returns("Stuff/Page.cshtml");
        A.CallTo(() => paths.Views).Returns(views);
        A.CallTo(() => views.OpenFile("Stuff/Page.cshtml"))
            .ReturnsLazily(() => new MemoryStream(Encoding.UTF8.GetBytes("Nested view")));
        var helper = new CodeFirstTemplateViewHelper(inner, paths);

        // Act
        var result = helper.UpdateViewFile(template);

        // Assert
        result.ShouldBe("Nested view");
        A.CallTo(() => views.AddFile(A<string>._, A<Stream>._, A<bool>._)).MustNotHaveHappened();
        A.CallTo(() => inner.UpdateViewFile(A<ITemplate>._, A<string?>._)).MustNotHaveHappened();
    }

    [Fact]
    public void TemplatePaths_OnlyMapsTemplateWithMatchingOwnershipKey() {
        // Arrange
        var registry = A.Fake<ICodeFirstDefinitionRegistry>();
        A.CallTo(() => registry.Definitions).Returns([
            new(CodeFirstDefinitionKind.Template, "Stuff_Page", "Page", null, [], TemplateViewPath: "Stuff/Page.cshtml"),
        ]);
        var keyValues = A.Fake<IKeyValueService>();
        var ownedKey = Guid.NewGuid();
        A.CallTo(() => keyValues.GetValue(CodeFirstTemplatePaths.OwnershipKey("Stuff_Page"))).Returns(ownedKey.ToString("D"));
        var fileSystems = new FileSystems(NullLoggerFactory.Instance, A.Fake<IIOHelper>(),
            Options.Create(new GlobalSettings()), A.Fake<IHostingEnvironment>());
        var paths = new CodeFirstTemplatePaths(registry, keyValues, fileSystems);
        var owned = A.Fake<ITemplate>();
        A.CallTo(() => owned.Alias).Returns("Stuff_Page");
        A.CallTo(() => owned.Key).Returns(ownedKey);
        var unowned = A.Fake<ITemplate>();
        A.CallTo(() => unowned.Alias).Returns("Stuff_Page");
        A.CallTo(() => unowned.Key).Returns(Guid.NewGuid());

        // Act
        var ownedPath = paths.GetOwnedPath(owned);
        var unownedPath = paths.GetOwnedPath(unowned);

        // Assert
        ownedPath.ShouldBe("Stuff/Page.cshtml");
        unownedPath.ShouldBeNull();
        paths.GetVerifiedPath("Stuff_Page").ShouldBeNull();
    }

    [Theory]
    [InlineData("Page", "Page.cshtml")]
    [InlineData("Blog_Page", "Blog/Page.cshtml")]
    [InlineData("Implicit", null)]
    public void TemplatePaths_CustomViews_MapsOwnedTemplates(string alias, string? path) {
        // Arrange
        var registry = A.Fake<ICodeFirstDefinitionRegistry>();
        A.CallTo(() => registry.Definitions).Returns([
            new(CodeFirstDefinitionKind.Template, alias, "Page", null, [], TemplateViewPath: path),
        ]);
        var keyValues = A.Fake<IKeyValueService>();
        var key = Guid.NewGuid();
        A.CallTo(() => keyValues.GetValue(CodeFirstTemplatePaths.OwnershipKey(alias))).Returns(key.ToString("D"));
        var fileSystems = new FileSystems(NullLoggerFactory.Instance, A.Fake<IIOHelper>(),
            Options.Create(new GlobalSettings()), A.Fake<IHostingEnvironment>());
        var customViews = A.Fake<IFileSystem>();
        var paths = new CodeFirstTemplatePaths(registry, keyValues, fileSystems, customViews);
        var owned = A.Fake<ITemplate>();
        A.CallTo(() => owned.Alias).Returns(alias);
        A.CallTo(() => owned.Key).Returns(key);

        // Act
        var ownedPath = paths.GetOwnedPath(owned);

        // Assert
        ownedPath.ShouldBe(path ?? alias + ".cshtml");
        paths.Views.ShouldBeSameAs(customViews);
        paths.UsesCustomViews.ShouldBeTrue();
    }
}
