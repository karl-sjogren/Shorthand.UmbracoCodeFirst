using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Services;
using Xunit.Sdk;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

[Trait("Category", "Integration")]
[Collection("Umbraco integration")]
public class TemplateViewsDirectoryIntegrationTests {
    [Fact]
    public async Task Templates_InCustomDirectory_SynchronizeAndResolveWithoutViewsCopies_Async() {
        // Arrange
        if(!string.Equals(Environment.GetEnvironmentVariable("RUN_UMBRACO_INTEGRATION_TESTS"), "true", StringComparison.OrdinalIgnoreCase)) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        var directory = "ucfFeatures" + Guid.NewGuid().ToString("N");
        var directoryPath = Path.Combine(AppContext.BaseDirectory, directory);
        var nestedDirectory = Path.Combine(directoryPath, "Blog");
        var nestedFile = Path.Combine(nestedDirectory, "Page.cshtml");
        var rootFile = Path.Combine(directoryPath, "Home.cshtml");
        Directory.CreateDirectory(nestedDirectory);
        const string content = "<h1>Custom template</h1>";
        await File.WriteAllTextAsync(nestedFile, content, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(rootFile, content, TestContext.Current.CancellationToken);
        var fixture = new UmbracoIntegrationFixture(directory);
        try {
            await fixture.InitializeAsync();
            using var scope = fixture.Services.CreateScope();
            var services = scope.ServiceProvider;
            var store = services.GetRequiredService<ICodeFirstTemplateDefinitionStore>();
            var templates = services.GetRequiredService<ITemplateService>();
            var defaultViews = services.GetRequiredService<FileSystems>().MvcViewsFileSystem.ShouldNotBeNull();
            var nested = new CodeFirstDefinition(CodeFirstDefinitionKind.Template, "Blog_Page", "Blog", null, [], TemplateViewPath: "Blog/Page.cshtml");
            var root = new CodeFirstDefinition(CodeFirstDefinitionKind.Template, "Home", "Home", null, [], TemplateViewPath: "Home.cshtml");
            var synchronizer = new CodeFirstSynchronizer(
                new TestRegistry([nested, root]), services.GetRequiredService<ICodeFirstDefinitionStore>(),
                new SynchronizationReportStore(), TimeProvider.System, NullLogger<CodeFirstSynchronizer>.Instance);

            // Act
            var first = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
            var second = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);

            // Assert
            first.FailedCount.ShouldBe(0);
            first.Items.Select(item => item.Status).ShouldAllBe(status => status == SynchronizationStatus.Created);
            second.FailedCount.ShouldBe(0);
            second.Items.Select(item => item.Status).ShouldAllBe(status => status == SynchronizationStatus.Unchanged);
            (await templates.GetAsync("Blog_Page")).ShouldNotBeNull().Content.ShouldBe(content);
            (await templates.GetAsync("Home")).ShouldNotBeNull().Content.ShouldBe(content);
            defaultViews.FileExists("Blog_Page.cshtml").ShouldBeFalse();
            defaultViews.FileExists("Home.cshtml").ShouldBeFalse();
            using(var stream = await templates.GetFileContentStreamAsync("Blog_Page.cshtml"))
            using(var reader = new StreamReader(stream, Encoding.UTF8)) {
                (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldBe(content);
            }
            using(var stream = await templates.GetFileContentStreamAsync("Blog/Page.cshtml"))
            using(var reader = new StreamReader(stream, Encoding.UTF8)) {
                (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldBe(content);
            }
            var context = new ActionContext(new DefaultHttpContext { RequestServices = services }, new RouteData(), new ActionDescriptor());
            var viewEngine = services.GetRequiredService<IRazorViewEngine>();
            var view = viewEngine.FindView(context, "Blog_Page", true);
            view.Success.ShouldBeTrue();
            viewEngine.FindView(context, "Home", true).Success.ShouldBeTrue();
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
            var tempData = new TempDataDictionary(context.HttpContext, A.Fake<ITempDataProvider>());
            using var output = new StringWriter();
            await view.View.RenderAsync(new ViewContext(context, view.View, viewData, tempData, output, new HtmlHelperOptions()));
            output.ToString().ShouldContain(content);
            (await File.ReadAllTextAsync(nestedFile, TestContext.Current.CancellationToken)).ShouldBe(content);
            (await File.ReadAllTextAsync(rootFile, TestContext.Current.CancellationToken)).ShouldBe(content);
            var changedContent = content + "\n<p>Changed</p>";
            await File.WriteAllTextAsync(nestedFile, changedContent, TestContext.Current.CancellationToken);
            (await store.ApplyAsync(nested, TestContext.Current.CancellationToken)).ShouldBe(SynchronizationStatus.Updated);
            (await templates.GetAsync("Blog_Page")).ShouldNotBeNull().Content.ShouldBe(changedContent);
            (await store.ApplyAsync(root with { Name = "Renamed" }, TestContext.Current.CancellationToken))
                .ShouldBe(SynchronizationStatus.Updated);
            (await templates.GetAsync("Home")).ShouldNotBeNull().Name.ShouldBe("Renamed");
            defaultViews.FileExists("Blog_Page.cshtml").ShouldBeFalse();
            defaultViews.FileExists("Home.cshtml").ShouldBeFalse();
        } finally {
            await fixture.DisposeAsync();
            File.Delete(nestedFile);
            File.Delete(rootFile);
            Directory.Delete(nestedDirectory);
            Directory.Delete(directoryPath);
        }
    }

    private sealed class TestRegistry(IReadOnlyList<CodeFirstDefinition> definitions) : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions => definitions;
    }
}
