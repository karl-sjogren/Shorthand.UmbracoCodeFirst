using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shorthand.UmbracoCodeFirst.Contracts;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;

internal sealed class StartupSynchronizationHandler(
    ICodeFirstSynchronizer synchronizer,
    IConfiguration configuration,
    IOptions<ModelsBuilderSettings> modelsBuilderSettings,
    ILogger<StartupSynchronizationHandler> logger) : INotificationAsyncHandler<UmbracoApplicationStartingNotification> {
    public async Task HandleAsync(UmbracoApplicationStartingNotification notification, CancellationToken cancellationToken) {
        var modelsMode = modelsBuilderSettings.Value.ModelsMode;
        if(!string.Equals(modelsMode, UmbracoConstants.ModelsBuilder.ModelsModes.Nothing, StringComparison.Ordinal)) {
            logger.LogWarning(
                "Umbraco Models Builder mode is {ModelsMode}. Set Umbraco:CMS:ModelsBuilder:ModelsMode to Nothing to avoid conflicts with Code First models.",
                modelsMode);
        }

        if(notification.RuntimeLevel == RuntimeLevel.Run
            && !configuration.GetValue<bool>("Shorthand:UmbracoCodeFirst:DisableStartupSynchronization")) {
            await synchronizer.SynchronizeAsync(cancellationToken);
        }
    }
}
