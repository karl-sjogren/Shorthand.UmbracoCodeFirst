using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Shorthand.UmbracoCodeFirst.Contracts;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
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
        var handler = new StartupSynchronizationHandler(
            synchronizer, configuration,
            Options.Create(new ModelsBuilderSettings { ModelsMode = "Nothing" }),
            new FakeLogger<StartupSynchronizationHandler>());
        var token = TestContext.Current.CancellationToken;

        // Act
        await handler.HandleAsync(new UmbracoApplicationStartingNotification(level, false), token);

        // Assert
        A.CallTo(() => synchronizer.SynchronizeAsync(token)).MustHaveHappened(calls, Times.Exactly);
    }

    [Theory]
    [InlineData("Nothing", false)]
    [InlineData("nothing", true)]
    [InlineData("InMemoryAuto", true)]
    [InlineData("SourceCodeManual", true)]
    [InlineData("", true)]
    public async Task HandleAsync_ModelsBuilderMode_WarnsWhenIncompatible_Async(string mode, bool warn) {
        // Arrange
        var logger = new FakeLogger<StartupSynchronizationHandler>();
        var handler = new StartupSynchronizationHandler(
            A.Fake<ICodeFirstSynchronizer>(),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["Shorthand:UmbracoCodeFirst:DisableStartupSynchronization"] = "true",
            }).Build(),
            Options.Create(new ModelsBuilderSettings { ModelsMode = mode }),
            logger);

        // Act
        await handler.HandleAsync(new UmbracoApplicationStartingNotification(RuntimeLevel.Run, false), TestContext.Current.CancellationToken);

        // Assert
        var warnings = logger.Collector.GetSnapshot().Where(entry => entry.Level == LogLevel.Warning).ToArray();
        warnings.Length.ShouldBe(warn ? 1 : 0);
        if(warn) {
            warnings[0].Message.ShouldContain("Umbraco:CMS:ModelsBuilder:ModelsMode");
            warnings[0].Message.ShouldContain("Nothing");
            warnings[0].StructuredState!.Single(item => item.Key == "ModelsMode").Value.ShouldBe(mode);
        }
    }

    [Fact]
    public async Task HandleAsync_DefaultModelsBuilderMode_Warns_Async() {
        // Arrange
        var logger = new FakeLogger<StartupSynchronizationHandler>();
        var settings = new ModelsBuilderSettings();
        var handler = new StartupSynchronizationHandler(
            A.Fake<ICodeFirstSynchronizer>(),
            new ConfigurationBuilder().Build(),
            Options.Create(settings),
            logger);

        // Act
        await handler.HandleAsync(new UmbracoApplicationStartingNotification(RuntimeLevel.Install, false), TestContext.Current.CancellationToken);

        // Assert
        settings.ModelsMode.ShouldBe("InMemoryAuto");
        logger.Collector.GetSnapshot().Single(entry => entry.Level == LogLevel.Warning)
            .Message.ShouldContain("InMemoryAuto");
    }
}
