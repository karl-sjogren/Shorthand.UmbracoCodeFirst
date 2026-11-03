using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Builder;
using Testcontainers.MsSql;

namespace Shorthand.UmbracoCodeFirst.Umbraco.Tests;

public sealed class UmbracoIntegrationFixture : IAsyncLifetime {
    private const string _sqlServerImage = "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";
    private readonly IFileSystem _fileSystem = new FileSystem();
    private readonly CancellationTokenSource _startupTimeout = new(TimeSpan.FromMinutes(5));
    private MsSqlContainer? _sqlServer;
    private WebApplication? _application;
    private Func<AssemblyLoadContext, AssemblyName, Assembly?>? _assemblyResolver;

    public bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("RUN_UMBRACO_INTEGRATION_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    public IServiceProvider Services =>
        _application?.Services
        ?? throw new InvalidOperationException("The Umbraco integration fixture is not initialized.");

    public async ValueTask InitializeAsync() {
        if(!IsEnabled) {
            return;
        }

        string assemblyDirectory = _fileSystem.Path.GetDirectoryName(typeof(UmbracoIntegrationFixture).Assembly.Location)!;
        _assemblyResolver = (_, assemblyName) => {
            string assemblyPath = _fileSystem.Path.Combine(assemblyDirectory, $"{assemblyName.Name}.dll");
            return _fileSystem.File.Exists(assemblyPath)
                ? AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath)
                : null;
        };
        AssemblyLoadContext.Default.Resolving += _assemblyResolver;

        _sqlServer = new MsSqlBuilder(_sqlServerImage)
            .WithDatabase("UmbracoIntegration")
            .WithPassword("CodeFirst12345!")
            .Build();
        await _sqlServer.StartAsync(_startupTimeout.Token);

        _application = await Program.CreateApplicationAsync(
            [],
            builder => {
                builder.WebHost
                    .UseSetting("ConnectionStrings:umbracoDbDSN", _sqlServer.GetConnectionString())
                    .UseSetting("ConnectionStrings:umbracoDbDSN_ProviderName", "Microsoft.Data.SqlClient")
                    .UseSetting("Umbraco:CMS:Unattended:InstallUnattended", "true")
                    .UseSetting("Umbraco:CMS:Unattended:UpgradeUnattended", "true")
                    .UseSetting("Umbraco:CMS:Unattended:UnattendedUserName", "codefirst")
                    .UseSetting("Umbraco:CMS:Unattended:UnattendedUserEmail", "codefirst@example.com")
                    .UseSetting("Umbraco:CMS:Unattended:UnattendedUserPassword", "codefirst123!");
            });
        await _application.StartAsync(_startupTimeout.Token);
    }

    public async ValueTask DisposeAsync() {
        if(_application is not null) {
            await _application.StopAsync();
            await _application.DisposeAsync();
        }

        if(_sqlServer is not null) {
            await _sqlServer.DisposeAsync();
        }

        if(_assemblyResolver is not null) {
            AssemblyLoadContext.Default.Resolving -= _assemblyResolver;
        }

        _startupTimeout.Dispose();
    }
}
