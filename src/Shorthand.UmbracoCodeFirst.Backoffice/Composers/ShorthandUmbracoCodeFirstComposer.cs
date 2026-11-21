using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shorthand.UmbracoCodeFirst.Backoffice.Cleanup;
using Shorthand.UmbracoCodeFirst.Backoffice.Contracts;
using Shorthand.UmbracoCodeFirst.Backoffice.GeneratedSources;
using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Composers;

public sealed class ShorthandUmbracoCodeFirstComposer : IComposer {
    public void Compose(IUmbracoBuilder builder) {
        builder.Services.AddSingleton<ICodeFirstDefinitionRegistry>(_ => CodeFirstDefinitionRegistryDiscovery.Discover());
        builder.Services.AddSingleton<ICodeFirstTemplatePaths>(services => {
            var fileSystems = services.GetRequiredService<FileSystems>();
            var directory = services.GetRequiredService<IConfiguration>()["Shorthand:UmbracoCodeFirst:TemplateViewsDirectory"];
            IFileSystem? customViews = null;
            if(!string.IsNullOrWhiteSpace(directory)) {
                var contentRoot = services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
                var fullPath = Path.GetFullPath(directory, contentRoot);
                var relativePath = Path.GetRelativePath(contentRoot, fullPath);
                if(Path.IsPathRooted(directory) || relativePath == "." || relativePath == ".."
                    || relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)) {
                    throw new InvalidOperationException("TemplateViewsDirectory must be a directory inside the application content root.");
                }

                if(!string.Equals(relativePath, "Views", OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) {
                    if(!Directory.Exists(fullPath)) {
                        throw new InvalidOperationException($"TemplateViewsDirectory '{directory}' does not exist.");
                    }

                    customViews = new PhysicalFileSystem(
                        services.GetRequiredService<IIOHelper>(),
                        services.GetRequiredService<Umbraco.Cms.Core.Hosting.IHostingEnvironment>(),
                        services.GetRequiredService<ILogger<PhysicalFileSystem>>(),
                        fullPath,
                        "/" + relativePath.Replace('\\', '/'));
                }
            }

            return new CodeFirstTemplatePaths(
                services.GetRequiredService<ICodeFirstDefinitionRegistry>(),
                services.GetRequiredService<Umbraco.Cms.Core.Services.IKeyValueService>(),
                fileSystems, customViews);
        });
        Decorate<IViewHelper>(builder.Services, (inner, services) =>
            new CodeFirstTemplateViewHelper(inner, services.GetRequiredService<ICodeFirstTemplatePaths>()));
        Decorate<ITemplateRepository>(builder.Services, (inner, services) =>
            new CodeFirstTemplateRepository(inner, services.GetRequiredService<ICodeFirstTemplatePaths>(),
                services.GetRequiredService<ICoreScopeProvider>()));
        builder.Services.AddSingleton<IConfigureOptions<RazorViewEngineOptions>, ConfigureCodeFirstTemplateViews>();
        builder.Services.AddSingleton<ICodeFirstGeneratedSourceService, CodeFirstGeneratedSourceService>();
        builder.Services.AddSingleton<ISynchronizationReportStore, SynchronizationReportStore>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ICodeFirstOwnershipStore, CodeFirstOwnershipStore>();
        builder.Services.AddSingleton<ICodeFirstTemplateDefinitionStore, CodeFirstTemplateDefinitionStore>();
        builder.Services.AddSingleton<ICodeFirstDefinitionStore, UmbracoCodeFirstDefinitionStore>();
        builder.Services.AddSingleton<CodeFirstSynchronizer>();
        builder.Services.AddSingleton<ICodeFirstSynchronizer, SynchronizationCoordinator>();
        builder.Services.AddSingleton<ICodeFirstCleanupService, CodeFirstCleanupService>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, StartupSynchronizationHandler>();
    }

    private static void Decorate<T>(IServiceCollection services, Func<T, IServiceProvider, T> decorate) where T : class {
        var original = services.Last(descriptor => descriptor.ServiceType == typeof(T));
        services.Remove(original);
        services.Add(ServiceDescriptor.Describe(typeof(T), provider => {
            var inner = original.ImplementationInstance
                ?? original.ImplementationFactory?.Invoke(provider)
                ?? ActivatorUtilities.CreateInstance(provider, original.ImplementationType!);
            return decorate((T)inner, provider);
        }, original.Lifetime));
    }
}
