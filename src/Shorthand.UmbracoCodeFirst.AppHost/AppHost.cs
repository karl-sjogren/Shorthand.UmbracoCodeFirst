using Umbraco.Aspire.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var umbraco = builder.AddUmbracoProject("umbraco-codefirst");

builder
    .AddSqlServer(umbraco, "umbraco-codefirst-sql", "umbraco-codefirst-sqldb")
    .AddAzureStorage(umbraco, "umbraco-codefirst-storage", "umbraco-codefirst-media")
    .AddRedisCache(umbraco, "umbraco-codefirst-redis")
    .AddBackofficeExtensions(umbraco);

builder.Build().Run();
