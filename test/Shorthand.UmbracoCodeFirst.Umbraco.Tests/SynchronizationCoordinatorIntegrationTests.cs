using Microsoft.Extensions.DependencyInjection;
using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.DistributedLocking;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

[Trait("Category", "Integration")]
[Collection("Umbraco integration")]
public class SynchronizationCoordinatorIntegrationTests(UmbracoIntegrationFixture fixture)
    : IClassFixture<UmbracoIntegrationFixture> {
    [Fact]
    public async Task Package_WithoutExplicitStartupCall_SynchronizesDuringBoot_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            return;
        }

        // Act
        await using var application = await Program.CreateApplicationAsync(
            [],
            builder => builder.WebHost
                .UseSetting("ConnectionStrings:umbracoDbDSN", fixture.ConnectionString)
                .UseSetting("ConnectionStrings:umbracoDbDSN_ProviderName", "Microsoft.Data.SqlClient")
                .UseSetting("Umbraco:CMS:Unattended:InstallUnattended", "true")
                .UseSetting("Umbraco:CMS:Unattended:UpgradeUnattended", "true"));
        var report = application.Services.GetRequiredService<ISynchronizationReportStore>().Current;
        await using var otherApplication = await Program.CreateApplicationAsync(
            [],
            builder => builder.WebHost
                .UseSetting("ConnectionStrings:umbracoDbDSN", fixture.ConnectionString)
                .UseSetting("ConnectionStrings:umbracoDbDSN_ProviderName", "Microsoft.Data.SqlClient")
                .UseSetting("Umbraco:CMS:Unattended:InstallUnattended", "true")
                .UseSetting("Umbraco:CMS:Unattended:UpgradeUnattended", "true"));
        var otherReport = otherApplication.Services.GetRequiredService<ISynchronizationReportStore>().Current;

        // Assert
        report.CompletedAt.ShouldNotBe(DateTimeOffset.MinValue);
        report.Items.ShouldNotBeEmpty();
        report.Items.Where(item => item.Status == SynchronizationStatus.Failed)
            .Select(item => $"{item.Alias}: {item.Message}").ShouldBeEmpty();
        otherReport.CompletedAt.ShouldNotBe(DateTimeOffset.MinValue);
        otherReport.Items.Count.ShouldBe(report.Items.Count);
        otherReport.FailedCount.ShouldBe(0);
    }

    [Fact]
    public async Task Coordinators_SerializeMatchingDefinitions_AndRejectDifferentDefinitions_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            return;
        }

        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRegistry = new TestRegistry("shared");
        var secondRegistry = new TestRegistry("shared");
        var differentRegistry = new TestRegistry("different");
        var firstStore = new TestStore(async token => {
            entered.SetResult();
            await release.Task.WaitAsync(token);
        });
        var secondStore = new TestStore(_ => {
            secondEntered.TrySetResult();
            return Task.CompletedTask;
        });
        var differentStore = new TestStore(_ => Task.CompletedTask);
        using var first = CreateCoordinator(firstRegistry, firstStore);
        using var second = CreateCoordinator(secondRegistry, secondStore);
        using var different = CreateCoordinator(differentRegistry, differentStore);

        // Act
        var firstRun = first.SynchronizeAsync(TestContext.Current.CancellationToken);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);
        var conflict = await different.SynchronizeAsync(TestContext.Current.CancellationToken);
        var secondRun = second.SynchronizeAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        var enteredBeforeRelease = secondEntered.Task.IsCompleted;
        release.SetResult();
        var firstReport = await firstRun.WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);
        var secondReport = await secondRun.WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);

        // Assert
        conflict.FailedCount.ShouldBe(1);
        conflict.Items.Single().Message.ShouldNotBeNull().ShouldContain("different definitions");
        differentStore.Calls.ShouldBe(0);
        enteredBeforeRelease.ShouldBeFalse();
        firstReport.FailedCount.ShouldBe(0);
        secondReport.FailedCount.ShouldBe(0);
        secondStore.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task Coordinator_InsideAmbientScope_FailsWithoutWriting_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            return;
        }

        var scopes = fixture.Services.GetRequiredService<ICoreScopeProvider>();
        var store = new TestStore(_ => Task.CompletedTask);
        using var coordinator = CreateCoordinator(new TestRegistry("ambient"), store);
        using var scope = scopes.CreateCoreScope();

        // Act
        var report = await coordinator.SynchronizeAsync(TestContext.Current.CancellationToken);

        // Assert
        report.FailedCount.ShouldBe(1);
        report.Error.ShouldNotBeNull().ShouldContain("existing Umbraco scope");
        store.Calls.ShouldBe(0);
        scope.Complete();
    }

    [Fact]
    public async Task Coordinator_StaleClaim_IsNotTakenOverUntilExplicitRecovery_Async() {
        // Arrange
        if(!fixture.IsEnabled) {
            return;
        }

        var values = fixture.Services.GetRequiredService<IKeyValueService>();
        var scopes = fixture.Services.GetRequiredService<ICoreScopeProvider>();
        var store = new TestStore(_ => Task.CompletedTask);
        using var coordinator = CreateCoordinator(new TestRegistry("recovery"), store);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        SetState("""{"Version":1,"Members":{"stopped":"old"},"Owner":"stopped"}""");

        try {
            // Act & Assert
            var conflict = await coordinator.SynchronizeAsync(TestContext.Current.CancellationToken);
            conflict.FailedCount.ShouldBe(1);
            store.Calls.ShouldBe(0);

            var fingerprint = DefinitionFingerprint.Create(new TestRegistry("recovery").Definitions);
            SetState($$"""{"Version":1,"Members":{"stopped":"{{fingerprint}}"},"Owner":"stopped"}""");
            await Should.ThrowAsync<OperationCanceledException>(() => coordinator.SynchronizeAsync(cancellation.Token));
            values.GetValue(SynchronizationCoordinator.CoordinationKey).ShouldNotBeNull().ShouldContain("\"stopped\"");
            store.Calls.ShouldBe(0);

            SetState(string.Empty);
            var recovered = await coordinator.SynchronizeAsync(TestContext.Current.CancellationToken);
            recovered.FailedCount.ShouldBe(0);
            store.Calls.ShouldBe(1);
        } finally {
            SetState(string.Empty);
        }

        void SetState(string state) {
            using var scope = scopes.CreateCoreScope();
            scope.EagerWriteLock(global::Umbraco.Cms.Core.Constants.Locks.KeyValues);
            values.SetValue(SynchronizationCoordinator.CoordinationKey, state);
            scope.Complete();
        }
    }

    private SynchronizationCoordinator CreateCoordinator(TestRegistry registry, TestStore store) {
        var reports = new SynchronizationReportStore();
        var synchronizer = new CodeFirstSynchronizer(
            registry, store, reports, TimeProvider.System, NullLogger<CodeFirstSynchronizer>.Instance);
        return new SynchronizationCoordinator(
            synchronizer,
            registry,
            reports,
            fixture.Services.GetRequiredService<ICoreScopeProvider>(),
            fixture.Services.GetRequiredService<IKeyValueService>(),
            fixture.Services.GetRequiredService<IDistributedLockingMechanismFactory>(),
            TimeProvider.System,
            NullLogger<SynchronizationCoordinator>.Instance);
    }

    private sealed class TestRegistry(string alias) : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = [
            new(CodeFirstDefinitionKind.DataType, alias, alias, "Umbraco.TextBox", []),
        ];
    }

    private sealed class TestStore(Func<CancellationToken, Task> apply) : ICodeFirstDefinitionStore {
        public int Calls { get; private set; }

        public async Task<SynchronizationResult> ApplyAsync(CodeFirstDefinition definition, CancellationToken cancellationToken) {
            Calls++;
            await apply(cancellationToken);
            return new(SynchronizationStatus.Unchanged);
        }
    }
}
