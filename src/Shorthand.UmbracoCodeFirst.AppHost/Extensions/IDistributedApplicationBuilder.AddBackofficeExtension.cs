namespace Umbraco.Aspire.AppHost.Extensions;

internal static partial class IDistributedApplicationBuilderExtensions {
    public static IDistributedApplicationBuilder AddBackofficeExtensions(
            this IDistributedApplicationBuilder builder,
            IResourceBuilder<ProjectResource> umbracoProject) {
        if(builder.ExecutionContext.IsRunMode) {
            var backofficeExtension = builder.AddJavaScriptApp("umbraco-codefirst-backoffice-extension", "../Shorthand.UmbracoCodeFirst.Backoffice/Client", runScriptName: "watch");

            umbracoProject.WithReference(backofficeExtension)
                .WaitFor(backofficeExtension);
        }

        return builder;
    }
}
