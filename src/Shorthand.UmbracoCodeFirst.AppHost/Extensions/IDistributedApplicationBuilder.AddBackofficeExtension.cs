using Aspire.Hosting.Dotnet;

namespace Umbraco.Aspire.AppHost.Extensions;

internal static partial class IDistributedApplicationBuilderExtensions {
    public static IDistributedApplicationBuilder AddBackofficeExtensions(
            this IDistributedApplicationBuilder builder,
            IResourceBuilder<DotnetProjectResource> umbracoProject) {
        if(builder.ExecutionContext.IsRunMode) {
            var backofficeExtension = builder
                .AddJavaScriptApp("umbraco-codefirst-backoffice-extension", "../Shorthand.UmbracoCodeFirst.Backoffice/Client")
                .WithNpm(installArgs: ["--force"])
                .WithRunScript("watch");

            var sampleBackofficeExtension = builder
                .AddJavaScriptApp("umbraco-codefirst-sample-backoffice-extension", "../Shorthand.UmbracoCodeFirst.Umbraco.Backoffice/Client")
                .WithNpm(installArgs: ["--force"])
                .WithRunScript("watch");

            umbracoProject.WithReference(backofficeExtension)
                .WaitFor(backofficeExtension)
                .WithReference(sampleBackofficeExtension)
                .WaitFor(sampleBackofficeExtension);
        }

        return builder;
    }
}
