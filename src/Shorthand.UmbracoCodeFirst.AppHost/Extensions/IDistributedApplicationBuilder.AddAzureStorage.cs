namespace Umbraco.Aspire.AppHost.Extensions;

internal static partial class IDistributedApplicationBuilderExtensions {
    public static IDistributedApplicationBuilder AddAzureStorage(
            this IDistributedApplicationBuilder builder,
            IResourceBuilder<ProjectResource> umbracoProject,
            string accountName,
            string containerName) {
        var storage = builder
            .AddAzureStorage(accountName)
            .RunAsEmulator(azurite => {
                azurite.WithDataVolume();
                azurite.WithLifetime(ContainerLifetime.Persistent);
            });

        var blobContainer = storage.AddBlobContainer(containerName, blobContainerName: containerName);

        umbracoProject
            .WithReference(blobContainer)
            .WaitFor(blobContainer);

        return builder;
    }
}
