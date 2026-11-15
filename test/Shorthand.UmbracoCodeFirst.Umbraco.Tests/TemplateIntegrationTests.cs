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
using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Services;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

[Trait("Category", "Integration")]
[Collection("Umbraco integration")]
public class TemplateIntegrationTests(UmbracoIntegrationFixture fixture) : IClassFixture<UmbracoIntegrationFixture> {
    [Fact]
    public async Task Templates_RegisterBeforeDocuments_PreserveViewsAndSkipUnowned_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            return;
        }

        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var views = services.GetRequiredService<FileSystems>().MvcViewsFileSystem.ShouldNotBeNull();
        var templateService = services.GetRequiredService<ITemplateService>();
        var alias = "ucfTemplate" + Guid.NewGuid().ToString("N");
        var manualAlias = "ucfManual" + Guid.NewGuid().ToString("N");
        var path = alias + ".cshtml";
        var manualPath = manualAlias + ".cshtml";
        const string content = "@attribute [Shorthand.UmbracoCodeFirst.Attributes.UCFTemplate(\"Test\")]\n<h1>Template content</h1>";
        views.FileExists(path).ShouldBeFalse();
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
                    new CodeFirstTypeSettings(DefaultTemplateAlias: alias, AllowedTemplateAliases: [alias])),
                new(CodeFirstDefinitionKind.Template, alias, "Test", null, []),
                new(CodeFirstDefinitionKind.Template, manualAlias, "Changed", null, []),
            ]);
            var store = services.GetRequiredService<ICodeFirstDefinitionStore>();
            var synchronizer = new CodeFirstSynchronizer(
                registry, store, new SynchronizationReportStore(), TimeProvider.System,
                NullLogger<CodeFirstSynchronizer>.Instance);

            // Act
            var first = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
            var second = await synchronizer.SynchronizeAsync(TestContext.Current.CancellationToken);
            var template = (await templateService.GetAsync(alias)).ShouldNotBeNull();
            var document = services.GetRequiredService<IContentTypeService>().Get(alias).ShouldNotBeNull();
            var updated = await store.ApplyAsync(
                new CodeFirstDefinition(CodeFirstDefinitionKind.Template, alias, "Renamed", null, []),
                TestContext.Current.CancellationToken);

            // Assert
            first.FailedCount.ShouldBe(0);
            first.Items.Single(item => item.Kind == CodeFirstDefinitionKind.Template && item.Alias == alias)
                .Status.ShouldBe(SynchronizationStatus.Created);
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
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
            var tempData = new TempDataDictionary(httpContext, A.Fake<ITempDataProvider>());
            using var output = new StringWriter();
            await view.View.RenderAsync(new ViewContext(
                actionContext, view.View, viewData, tempData, output, new HtmlHelperOptions()));
            output.ToString().ShouldContain("<h1>Template content</h1>");
            using var stream = views.OpenFile(path);
            using var reader = new StreamReader(stream);
            (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldBe(content);
            using var unchangedStream = views.OpenFile(manualPath);
            using var unchangedBytes = new MemoryStream();
            await unchangedStream.CopyToAsync(unchangedBytes, TestContext.Current.CancellationToken);
            unchangedBytes.ToArray().ShouldBe(manualBytes);
        } finally {
            if(createdPath) {
                views.DeleteFile(path);
            }

            if(createdManualPath) {
                views.DeleteFile(manualPath);
            }
        }
    }

    [Fact]
    public async Task Templates_MissingView_FailsWithoutCreatingTemplate_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            return;
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
