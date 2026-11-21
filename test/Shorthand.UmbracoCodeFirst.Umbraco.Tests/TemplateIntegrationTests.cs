using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;
using Xunit.Sdk;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

[Trait("Category", "Integration")]
[Collection("Umbraco integration")]
public class TemplateIntegrationTests(UmbracoIntegrationFixture fixture) : IClassFixture<UmbracoIntegrationFixture> {
    [Fact]
    public async Task Templates_RegisterBeforeDocuments_PreserveViewsAndSkipUnowned_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var views = services.GetRequiredService<FileSystems>().MvcViewsFileSystem.ShouldNotBeNull();
        var templateService = services.GetRequiredService<ITemplateService>();
        var leaf = "ucfTemplate" + Guid.NewGuid().ToString("N");
        var alias = "Features_Blog_" + leaf;
        var manualAlias = "ucfManual" + Guid.NewGuid().ToString("N");
        var path = "Features/Blog/" + leaf + ".cshtml";
        var rootPath = alias + ".cshtml";
        var manualPath = manualAlias + ".cshtml";
        const string content = "@attribute [Shorthand.UmbracoCodeFirst.Attributes.UCFTemplate(\"Test\")]\n<h1>Template content</h1>";
        views.FileExists(path).ShouldBeFalse();
        views.FileExists(rootPath).ShouldBeFalse();
        views.FileExists(manualPath).ShouldBeFalse();
        var createdPath = false;
        var createdManualPath = false;
        try {
            using(var data = new MemoryStream(Encoding.UTF8.GetBytes(content))) {
                views.AddFile(path, data, false);
                createdPath = true;
            }

            using(var data = new MemoryStream(Encoding.UTF8.GetBytes(content))) {
                views.AddFile(manualPath, data, false);
                createdManualPath = true;
            }

            var manual = await templateService.CreateAsync("Manual", manualAlias, content, Constants.Security.SuperUserKey);
            manual.Success.ShouldBeTrue();
            byte[] manualBytes;
            using(var manualStream = views.OpenFile(manualPath)) {
                using var buffer = new MemoryStream();
                await manualStream.CopyToAsync(buffer, TestContext.Current.CancellationToken);
                manualBytes = buffer.ToArray();
            }

            var registry = new TestRegistry([
                new(CodeFirstDefinitionKind.DocumentType, alias, "Template document", null, [],
                    new CodeFirstTypeSettings(AllowAsRoot: true, DefaultTemplateAlias: alias, AllowedTemplateAliases: [alias])),
                new(CodeFirstDefinitionKind.Template, alias, "Test", null, [], TemplateViewPath: path),
                new(CodeFirstDefinitionKind.Template, manualAlias, "Changed", null, [], TemplateViewPath: "Features/Blog/Manual.cshtml"),
            ]);
            var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
            var synchronizer = new CodeFirstSynchronizer(
                registry, store, new SynchronizationReportStore(), TimeProvider.System,
                NullLogger<CodeFirstSynchronizer>.Instance);

            // Act
            var first = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
            var second = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
            var updated = await store.ApplyAsync(
                new CodeFirstDefinition(CodeFirstDefinitionKind.Template, alias, "Renamed", null, [], TemplateViewPath: path),
                TestContext.Current.CancellationToken);

            // Assert
            first.FailedCount.ShouldBe(0);
            var templateItem = first.Items.Single(item => item.Kind == CodeFirstDefinitionKind.Template && item.Alias == alias);
            templateItem.Status.ShouldBe(SynchronizationStatus.Created);
            var template = (await templateService.GetAsync(alias)).ShouldNotBeNull();
            template.Content.ShouldBe(content);
            template.VirtualPath.ShouldEndWith(path);
            views.FileExists(rootPath).ShouldBeFalse();
            var document = services.GetRequiredService<IContentTypeService>().Get(alias).ShouldNotBeNull();
            first.Items.Single(item => item.Alias == manualAlias).Status.ShouldBe(SynchronizationStatus.Skipped);
            second.FailedCount.ShouldBe(0);
            second.Items.Single(item => item.Kind == CodeFirstDefinitionKind.Template && item.Alias == alias)
                .Status.ShouldBe(SynchronizationStatus.Unchanged);
            document.DefaultTemplate.ShouldNotBeNull().Key.ShouldBe(template.Key);
            document.AllowedTemplates.ShouldNotBeNull().Select(item => item.Key).ShouldContain(template.Key);
            updated.Status.ShouldBe(SynchronizationStatus.Updated);
            (await templateService.GetAsync(alias)).ShouldNotBeNull().Name.ShouldBe("Renamed");
            (await templateService.GetAsync(manualAlias)).ShouldNotBeNull().Name.ShouldBe("Manual");
            var view = services.GetRequiredService<IRazorViewEngine>().GetView(null, $"~/Views/{path}", true);
            view.Success.ShouldBeTrue();
            var httpContext = new DefaultHttpContext { RequestServices = services };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var aliasView = services.GetRequiredService<IRazorViewEngine>().FindView(actionContext, alias, true);
            aliasView.Success.ShouldBeTrue();
            var freshPaths = new CodeFirstTemplatePaths(registry, services.GetRequiredService<IKeyValueService>(),
                services.GetRequiredService<FileSystems>());
            var expander = new CodeFirstTemplateViewLocationExpander(
                freshPaths, services.GetRequiredService<ITemplateRepository>(), services.GetRequiredService<ICoreScopeProvider>());
            var expanderContext = new ViewLocationExpanderContext(actionContext, alias, "Render", null, null, true) {
                Values = new Dictionary<string, string?>(),
            };
            expander.PopulateValues(expanderContext);
            expander.ExpandViewLocations(expanderContext, []).Single().ShouldBe("/Views/" + path);
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
            var tempData = new TempDataDictionary(httpContext, A.Fake<ITempDataProvider>());
            using var output = new StringWriter();
            await aliasView.View.RenderAsync(new ViewContext(
                actionContext, aliasView.View, viewData, tempData, output, new HtmlHelperOptions()));
            output.ToString().ShouldContain("<h1>Template content</h1>");
            var contentService = services.GetRequiredService<IContentService>();
            var page = contentService.Create("Template test", -1, alias);
            contentService.SaveAndPublish(page, []).Success.ShouldBeTrue();
            var addresses = services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().ShouldNotBeNull().Addresses;
            using var client = new HttpClient();
            var html = await client.GetStringAsync(addresses.First(address => address.StartsWith("http://", StringComparison.Ordinal)),
                TestContext.Current.CancellationToken);
            html.ShouldContain("<h1>Template content</h1>");
            using(var stream = views.OpenFile(path))
            using(var reader = new StreamReader(stream)) {
                (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldBe(content);
            }
            using var unchangedStream = views.OpenFile(manualPath);
            using var unchangedBytes = new MemoryStream();
            await unchangedStream.CopyToAsync(unchangedBytes, TestContext.Current.CancellationToken);
            unchangedBytes.ToArray().ShouldBe(manualBytes);

            var changedContent = content + "\n<p>Changed</p>";
            using(var data = new MemoryStream(Encoding.UTF8.GetBytes(changedContent))) {
                views.AddFile(path, data, true);
            }

            var changed = await store.ApplyAsync(
                new CodeFirstDefinition(CodeFirstDefinitionKind.Template, alias, "Renamed", null, [], TemplateViewPath: path),
                TestContext.Current.CancellationToken);
            changed.Status.ShouldBe(SynchronizationStatus.Updated);
            (await templateService.GetAsync(alias)).ShouldNotBeNull().Content.ShouldBe(changedContent);
            using var contentStream = await templateService.GetFileContentStreamAsync(rootPath);
            using var contentReader = new StreamReader(contentStream);
            (await contentReader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldBe(changedContent);
            views.FileExists(rootPath).ShouldBeFalse();
            var unchanged = await store.ApplyAsync(
                new CodeFirstDefinition(CodeFirstDefinitionKind.Template, alias, "Renamed", null, [], TemplateViewPath: path),
                TestContext.Current.CancellationToken);
            unchanged.Status.ShouldBe(SynchronizationStatus.Unchanged);
        } finally {
            if(createdPath) {
                views.DeleteFile(path);
                if(views.FileExists(rootPath)) {
                    views.DeleteFile(rootPath);
                }
            }

            if(createdManualPath) {
                views.DeleteFile(manualPath);
            }
        }
    }

    [Fact]
    public async Task Templates_NestedViewWithExistingRootFile_FailsWithoutOverwritingEither_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var views = services.GetRequiredService<FileSystems>().MvcViewsFileSystem.ShouldNotBeNull();
        var alias = "ucfConflict" + Guid.NewGuid().ToString("N");
        var nestedPath = "Features/" + alias + ".cshtml";
        var rootPath = alias + ".cshtml";
        const string nestedContent = "Nested content";
        const string rootContent = "Manual root content";
        using(var nested = new MemoryStream(Encoding.UTF8.GetBytes(nestedContent))) {
            views.AddFile(nestedPath, nested, false);
        }

        using(var root = new MemoryStream(Encoding.UTF8.GetBytes(rootContent))) {
            views.AddFile(rootPath, root, false);
        }

        try {
            var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
            var templates = services.GetRequiredService<ITemplateService>();

            // Act
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await store.ApplyAsync(new(CodeFirstDefinitionKind.Template, alias, "Conflict", null, [], TemplateViewPath: nestedPath),
                    TestContext.Current.CancellationToken));

            // Assert
            exception.Message.ShouldContain("conflicts");
            (await templates.GetAsync(alias)).ShouldBeNull();
            using var nestedReader = new StreamReader(views.OpenFile(nestedPath));
            using var rootReader = new StreamReader(views.OpenFile(rootPath));
            (await nestedReader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldBe(nestedContent);
            (await rootReader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldBe(rootContent);
        } finally {
            views.DeleteFile(nestedPath);
            views.DeleteFile(rootPath);
        }
    }

    [Fact]
    public async Task Templates_NestedView_PreservesLayoutMetadataOnCreateAndUpdate_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var views = services.GetRequiredService<FileSystems>().MvcViewsFileSystem.ShouldNotBeNull();
        var templates = services.GetRequiredService<ITemplateService>();
        var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
        var suffix = Guid.NewGuid().ToString("N");
        var layoutAlias = "ucfLayout" + suffix;
        var leaf = "ucfChild" + suffix;
        var alias = "Features_" + leaf;
        var path = "Features/" + leaf + ".cshtml";
        var rootPath = alias + ".cshtml";
        var layoutPath = layoutAlias + ".cshtml";
        var withLayout = $"@{{ Layout = \"{layoutAlias}.cshtml\"; }}\n<h1>Nested</h1>";
        const string withoutLayout = "@{ Layout = null; }\n<h1>Nested</h1>";
        var layout = await templates.CreateAsync("Layout", layoutAlias, "@RenderBody()", Constants.Security.SuperUserKey);
        layout.Success.ShouldBeTrue();
        using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(withLayout))) {
            views.AddFile(path, stream, false);
        }

        try {
            var definition = new CodeFirstDefinition(CodeFirstDefinitionKind.Template, alias, "Child", null, [], TemplateViewPath: path);

            // Act
            var created = await store.ApplyAsync(definition, TestContext.Current.CancellationToken);
            var initial = (await templates.GetAsync(alias)).ShouldNotBeNull();
            var initialLayout = initial.LayoutTemplateAlias;
            using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(withoutLayout))) {
                views.AddFile(path, stream, true);
            }

            var updated = await store.ApplyAsync(definition, TestContext.Current.CancellationToken);

            // Assert
            created.Status.ShouldBe(SynchronizationStatus.Created);
            initialLayout.ShouldBe(layoutAlias);
            updated.Status.ShouldBe(SynchronizationStatus.Updated);
            (await templates.GetAsync(alias)).ShouldNotBeNull().LayoutTemplateAlias.ShouldBeNull();
            views.FileExists(rootPath).ShouldBeFalse();
        } finally {
            views.DeleteFile(path);
            views.DeleteFile(layoutPath);
            if(views.FileExists(rootPath)) {
                views.DeleteFile(rootPath);
            }
        }
    }

    [Fact]
    public async Task Templates_MissingView_FailsWithoutCreatingTemplate_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            throw SkipException.ForSkip("Umbraco integration tests are disabled.");
        }

        using var scope = fixture.Services.CreateScope();
        var alias = "ucfMissing" + Guid.NewGuid().ToString("N");
        var store = scope.ServiceProvider.GetRequiredService<ICodeFirstDefinitionStore>();
        var templates = scope.ServiceProvider.GetRequiredService<ITemplateService>();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await store.ApplyAsync(new(CodeFirstDefinitionKind.Template, alias, "Missing", null, []),
                TestContext.Current.CancellationToken));
        exception.Message.ShouldContain("does not exist");
        (await templates.GetAsync(alias)).ShouldBeNull();
    }

    private sealed class TestRegistry(IReadOnlyList<CodeFirstDefinition> definitions) : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = definitions;
    }
}
