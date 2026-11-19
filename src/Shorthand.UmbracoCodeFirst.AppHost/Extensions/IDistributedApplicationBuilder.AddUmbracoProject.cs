using Aspire.Hosting.Dotnet;

namespace Umbraco.Aspire.AppHost.Extensions;

internal static partial class IDistributedApplicationBuilderExtensions {
    public static IResourceBuilder<DotnetProjectResource> AddUmbracoProject(this IDistributedApplicationBuilder builder, string projectName) {
        var project = builder.AddDotnetProject(projectName, "../Shorthand.UmbracoCodeFirst.Umbraco/Shorthand.UmbracoCodeFirst.Umbraco.csproj")
            .WithExternalHttpEndpoints()
            .WithEnvironment("Umbraco__CMS__Unattended__InstallUnattended", bool.TrueString)
            .WithEnvironment("Umbraco__CMS__Unattended__UnattendedUserName", "codefirst")
            .WithEnvironment("Umbraco__CMS__Unattended__UnattendedUserEmail", "codefirst@example.com")
            .WithEnvironment("Umbraco__CMS__Unattended__UnattendedUserPassword", "codefirst123!");

        if(builder.ExecutionContext.IsRunMode) {
            project
                .WithUrlForEndpoint("http", u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly)
                .WithUrlForEndpoint("https", _ => new() { Url = "/umbraco", DisplayText = "Umbraco Backoffice" });
        }

        return project;
    }
}
