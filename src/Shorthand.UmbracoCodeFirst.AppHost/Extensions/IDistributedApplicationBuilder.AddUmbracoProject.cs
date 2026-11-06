namespace Umbraco.Aspire.AppHost.Extensions;

internal static partial class IDistributedApplicationBuilderExtensions {
    public static IResourceBuilder<ProjectResource> AddUmbracoProject<TProject>(this IDistributedApplicationBuilder builder, string projectName) where TProject : IProjectMetadata, new() {
        var project = builder.AddProject<TProject>(projectName)
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
