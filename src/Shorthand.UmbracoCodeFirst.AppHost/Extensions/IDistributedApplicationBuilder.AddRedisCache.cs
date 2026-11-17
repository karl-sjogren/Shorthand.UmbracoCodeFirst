namespace Umbraco.Aspire.AppHost.Extensions;

internal static partial class IDistributedApplicationBuilderExtensions {
    public static IDistributedApplicationBuilder AddRedisCache(
            this IDistributedApplicationBuilder builder,
            IResourceBuilder<ProjectResource> umbracoProject,
            string redisCacheName) {
        var redisCache = builder.AddAzureManagedRedis(redisCacheName);

        if(builder.ExecutionContext.IsRunMode) {
            redisCache
                .RunAsContainer(redis => {
                    redis.WithDataVolume();
                    redis.WithRedisInsight(containerName: $"{redisCacheName}-insight");
                });
        }

        umbracoProject.WithReference(redisCache)
            .WaitFor(redisCache);

        return builder;
    }
}
