using Microsoft.Extensions.DependencyInjection;
using Shorthand.UmbracoCodeFirst.Backoffice.Cleanup;
using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Composers;

public sealed class ShorthandUmbracoCodeFirstComposer : IComposer {
    public void Compose(IUmbracoBuilder builder) {
        builder.Services.AddSingleton<ICodeFirstDefinitionRegistry>(
            _ => CodeFirstDefinitionRegistryDiscovery.Discover());
        builder.Services.AddSingleton<ISynchronizationReportStore, SynchronizationReportStore>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<CodeFirstOwnershipStore>();
        builder.Services.AddSingleton<ICodeFirstDefinitionStore, UmbracoCodeFirstDefinitionStore>();
        builder.Services.AddSingleton<CodeFirstSynchronizer>();
        builder.Services.AddSingleton<CodeFirstCleanupService>();
    }
}
