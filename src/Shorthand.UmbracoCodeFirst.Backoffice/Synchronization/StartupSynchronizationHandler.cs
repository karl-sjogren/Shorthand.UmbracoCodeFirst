using Microsoft.Extensions.Configuration;
using Shorthand.UmbracoCodeFirst.Contracts;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;

internal sealed class StartupSynchronizationHandler(
    ICodeFirstSynchronizer synchronizer,
    IConfiguration configuration) : INotificationAsyncHandler<UmbracoApplicationStartingNotification> {
    public async Task HandleAsync(UmbracoApplicationStartingNotification notification, CancellationToken cancellationToken) {
        if(notification.RuntimeLevel == RuntimeLevel.Run
            && !configuration.GetValue<bool>("Shorthand:UmbracoCodeFirst:DisableStartupSynchronization")) {
            await synchronizer.SynchronizeAsync(cancellationToken);
        }
    }
}
