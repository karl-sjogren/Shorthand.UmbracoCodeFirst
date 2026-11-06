namespace Umbraco.Aspire.AppHost.Extensions;

internal static partial class IDistributedApplicationBuilderExtensions {
    public static IDistributedApplicationBuilder AddSqlServer(
            this IDistributedApplicationBuilder builder,
            IResourceBuilder<ProjectResource> umbracoProject,
            string serverName,
            string databaseName) {
        var sqlServer = builder.AddSqlServer(serverName)
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent)
            .WithHostPort(55921);

        sqlServer.WithDbGate(builder => {
            sqlServer.WithChildRelationship(builder);
            builder.WithHostPort(55922);
            builder.WithLifetime(ContainerLifetime.Persistent);

            builder.OnResourceEndpointsAllocated((resource, _, _) => {
                var dashboardEndpoint = resource.GetEndpoint("http");
                if(dashboardEndpoint is not null) {
                    sqlServer.WithUrl(dashboardEndpoint.Url, "DbGate Dashboard");
                }

                return Task.CompletedTask;
            });
        }, serverName + "-dbgate");

        var sqlDatabase = sqlServer
            .AddDatabase(databaseName);

        umbracoProject
            .WithReference(sqlDatabase)
            .WaitFor(sqlDatabase)
            .WithEnvironment("ConnectionStrings__umbracoDbDSN", sqlDatabase);

        return builder;
    }
}
