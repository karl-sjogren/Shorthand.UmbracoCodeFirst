
using Shorthand.UmbracoCodeFirst.Synchronization;

WebApplication app = await Program.CreateApplicationAsync(args);
await app.RunAsync();

public partial class Program {
    public static async Task<WebApplication> CreateApplicationAsync(
        string[] args,
        Action<WebApplicationBuilder>? configureBuilder = null) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            Args = args,
            ApplicationName = typeof(Program).Assembly.GetName().Name,
        });
        configureBuilder?.Invoke(builder);

#if DEBUG
        builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
#endif

        builder.CreateUmbracoBuilder()
            .AddBackOffice()
            .AddWebsite()
            .AddComposers()
            .Build();

        WebApplication app = builder.Build();

        await app.BootUmbracoAsync();

        using(IServiceScope scope = app.Services.CreateScope()) {
            await scope.ServiceProvider
                .GetRequiredService<CodeFirstSynchronizer>()
                .SynchronizeAsync();
        }

        app.UseUmbraco()
            .WithMiddleware(u => {
                u.UseBackOffice();
                u.UseWebsite();
            })
            .WithEndpoints(u => {
                u.UseBackOfficeEndpoints();
                u.UseWebsiteEndpoints();
            });

        return app;
    }
}
