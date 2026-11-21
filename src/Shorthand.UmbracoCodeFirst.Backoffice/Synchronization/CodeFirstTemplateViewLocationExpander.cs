using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;

internal sealed class CodeFirstTemplateViewLocationExpander(
    ICodeFirstTemplatePaths paths,
    ITemplateRepository templates,
    ICoreScopeProvider scopeProvider) : IViewLocationExpander {
    public void PopulateValues(ViewLocationExpanderContext context) {
        string? path = null;
        if(paths.ContainsAlias(context.ViewName)) {
            using var scope = scopeProvider.CreateCoreScope(autoComplete: true);
            var template = templates.Get(context.ViewName);
            path = template is null ? null : paths.GetOwnedPath(template);
        }

        if(path is not null) {
            context.Values["Shorthand.UmbracoCodeFirst.TemplatePath"] = paths.Views.GetUrl(path);
        }
    }

    public IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations) =>
        context.Values.TryGetValue("Shorthand.UmbracoCodeFirst.TemplatePath", out var path) && path is not null
            ? new[] { path.Replace('\\', '/') }.Concat(viewLocations)
            : viewLocations;
}

internal sealed class ConfigureCodeFirstTemplateViews(
    ICodeFirstTemplatePaths paths,
    ITemplateRepository templates,
    ICoreScopeProvider scopeProvider) : IConfigureOptions<RazorViewEngineOptions> {
    public void Configure(RazorViewEngineOptions options) =>
        options.ViewLocationExpanders.Add(new CodeFirstTemplateViewLocationExpander(paths, templates, scopeProvider));
}
