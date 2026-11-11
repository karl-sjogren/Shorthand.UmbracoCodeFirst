using Microsoft.Extensions.Configuration;
using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Shorthand.UmbracoCodeFirst.Contracts;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Notifications;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Tests;

public class StartupSynchronizationHandlerTests {
    [Theory]
    [InlineData(RuntimeLevel.Run, false, 1)]
    [InlineData(RuntimeLevel.Run, true, 0)]
    [InlineData(RuntimeLevel.Install, false, 0)]
    [InlineData(RuntimeLevel.Upgrade, false, 0)]
    public async Task HandleAsync_RunsOnlyWhenEnabledAndReady_Async(RuntimeLevel level, bool disabled, int calls) {
        // Arrange
        var synchronizer = A.Fake<ICodeFirstSynchronizer>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Shorthand:UmbracoCodeFirst:DisableStartupSynchronization"] = disabled.ToString(),
        }).Build();
        var handler = new StartupSynchronizationHandler(synchronizer, configuration);
        var token = TestContext.Current.CancellationToken;

        // Act
        await handler.HandleAsync(new UmbracoApplicationStartingNotification(level, false), token);

        // Assert
        A.CallTo(() => synchronizer.SynchronizeAsync(token)).MustHaveHappened(calls, Times.Exactly);
    }
}
