using System.Reflection;
using System.Runtime.Loader;
using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.PropertyEditors;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Testcontainers.MsSql;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

namespace Shorthand.UmbracoCodeFirst.PackageConsumer.Tests;

public class PackageConsumerStartupTests : IDisposable {
    private readonly Func<AssemblyLoadContext, AssemblyName, Assembly?> _assemblyResolver = (_, assemblyName) => {
        var path = Path.Combine(AppContext.BaseDirectory, $"{assemblyName.Name}.dll");
        return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
    };

    public PackageConsumerStartupTests() => AssemblyLoadContext.Default.Resolving += _assemblyResolver;

    public void Dispose() => AssemblyLoadContext.Default.Resolving -= _assemblyResolver;

    [Fact]
    public async Task PackagedConsumer_UnattendedInstallRestartAndUpgrade_SynchronizesGeneratedDefinition_Async() {
        // Arrange
        var token = TestContext.Current.CancellationToken;
        await using var sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
            .WithDatabase("CodeFirstPackageConsumer")
            .WithPassword($"CodeFirst-{Guid.NewGuid():N}!")
            .Build();
        await sql.StartAsync(token);

        // Act
        SynchronizationReport firstReport;
        await using(var first = await CreateApplicationAsync(sql.GetConnectionString())) {
            await first.StartAsync(token);
            firstReport = first.Services.GetRequiredService<ISynchronizationReportStore>().Current;
            await first.StopAsync(token);
        }

        SynchronizationReport restartReport;
        IReadOnlyList<CodeFirstDefinition> definitions;
        string stateKey;
        string finalState;
        await using(var restarted = await CreateApplicationAsync(sql.GetConnectionString())) {
            definitions = restarted.Services.GetRequiredService<ICodeFirstDefinitionRegistry>().Definitions;
            restartReport = restarted.Services.GetRequiredService<ISynchronizationReportStore>().Current;
            var plan = new UmbracoPlan(restarted.Services.GetRequiredService<IUmbracoVersion>());
            stateKey = new Upgrader(plan).StateValueKey;
            finalState = plan.FinalState;

            var scopes = restarted.Services.GetRequiredService<ICoreScopeProvider>();
            using var scope = scopes.CreateCoreScope();
            // This state precedes Umbraco 18.1's idempotent final migration.
            restarted.Services.GetRequiredService<IKeyValueService>().SetValue(
                stateKey, "{6FE4656E-8B8D-452F-AE2A-438A615B61BC}");
            scope.Complete();
        }

        await using var upgraded = await CreateApplicationAsync(sql.GetConnectionString());
        var upgradeLevel = upgraded.Services.GetRequiredService<IRuntimeState>().Level;
        var reportBeforeUpgrade = upgraded.Services.GetRequiredService<ISynchronizationReportStore>().Current;
        await upgraded.StartAsync(token);
        var upgradeStore = upgraded.Services.GetRequiredService<ISynchronizationReportStore>();
        using var upgradeTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        upgradeTimeout.CancelAfter(TimeSpan.FromMinutes(2));
        while(upgradeStore.Current.CompletedAt == DateTimeOffset.MinValue) {
            await Task.Delay(TimeSpan.FromMilliseconds(100), upgradeTimeout.Token);
        }

        var upgradeReport = upgraded.Services.GetRequiredService<ISynchronizationReportStore>().Current;
        var upgradedState = upgraded.Services.GetRequiredService<IKeyValueService>().GetValue(stateKey);
        await upgraded.Services.GetRequiredService<IRuntime>().RestartAsync();
        var runtimeRestartReport = upgradeStore.Current;

        // Assert
        definitions.ShouldContain(definition => definition.Name == "Package consumer text");
        firstReport.CompletedAt.ShouldNotBe(DateTimeOffset.MinValue);
        firstReport.Error.ShouldBeNull();
        firstReport.FailedCount.ShouldBe(0);
        firstReport.Items.ShouldContain(item =>
            item.Name == "Package consumer text" && item.Status == SynchronizationStatus.Created);
        restartReport.CompletedAt.ShouldNotBe(DateTimeOffset.MinValue);
        restartReport.Error.ShouldBeNull();
        restartReport.Items.ShouldContain(item =>
            item.Name == "Package consumer text"
            && (item.Status == SynchronizationStatus.Updated || item.Status == SynchronizationStatus.Unchanged));
        restartReport.FailedCount.ShouldBe(0);
        upgradeLevel.ShouldBe(RuntimeLevel.Upgrading);
        reportBeforeUpgrade.CompletedAt.ShouldBe(DateTimeOffset.MinValue);
        upgradedState.ShouldBe(finalState);
        upgradeReport.CompletedAt.ShouldNotBe(DateTimeOffset.MinValue);
        upgradeReport.Error.ShouldBeNull();
        upgradeReport.Items.ShouldContain(item =>
            item.Name == "Package consumer text"
            && (item.Status == SynchronizationStatus.Updated || item.Status == SynchronizationStatus.Unchanged));
        upgradeReport.FailedCount.ShouldBe(0);
        runtimeRestartReport.ShouldNotBeSameAs(upgradeReport);
        runtimeRestartReport.FailedCount.ShouldBe(0);
        runtimeRestartReport.Items.ShouldContain(item =>
            item.Name == "Package consumer text"
            && (item.Status == SynchronizationStatus.Updated || item.Status == SynchronizationStatus.Unchanged));
    }

    private static async Task<WebApplication> CreateApplicationAsync(string connectionString) {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            ApplicationName = typeof(PackageConsumerStartupTests).Assembly.GetName().Name,
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.WebHost
            .UseSetting("urls", "http://127.0.0.1:0")
            .UseSetting("ConnectionStrings:umbracoDbDSN", connectionString)
            .UseSetting("ConnectionStrings:umbracoDbDSN_ProviderName", "Microsoft.Data.SqlClient")
            .UseSetting("Umbraco:CMS:Global:Id", "b6c8c0a1-0f65-48a5-b894-8e57ee7bc1db")
            .UseSetting("Umbraco:CMS:Unattended:InstallUnattended", "true")
            .UseSetting("Umbraco:CMS:Unattended:UpgradeUnattended", "true")
            .UseSetting("Umbraco:CMS:Unattended:UnattendedUserName", "Package consumer")
            .UseSetting("Umbraco:CMS:Unattended:UnattendedUserEmail", "package-consumer@example.invalid")
            .UseSetting("Umbraco:CMS:Unattended:UnattendedUserPassword", $"CodeFirst-{Guid.NewGuid():N}!")
            .UseSetting("Umbraco:CMS:ModelsBuilder:ModelsMode", "Nothing");

        builder.CreateUmbracoBuilder().AddBackOffice().AddWebsite().AddComposers().Build();
        var app = builder.Build();
        await app.BootUmbracoAsync();
        app.UseUmbraco()
            .WithMiddleware(umbraco => {
                umbraco.UseBackOffice();
                umbraco.UseWebsite();
            })
            .WithEndpoints(umbraco => {
                umbraco.UseBackOfficeEndpoints();
                umbraco.UseWebsiteEndpoints();
            });
        return app;
    }
}

[UCFDataType("Package consumer text", typeof(UmbracoEditors.TextBox), typeof(PackageConsumerTextConfiguration))]
public partial class PackageConsumerTextDataType;

public sealed class PackageConsumerTextConfiguration : UmbracoEditors.TextBox.Configuration;
