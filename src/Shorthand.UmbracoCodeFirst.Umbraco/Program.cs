using Shorthand.UmbracoCodeFirst.Contracts;

var app = await CreateApplicationAsync(args);
await app.RunAsync();

internal partial class Program {
    public static async Task<WebApplication> CreateApplicationAsync(
        string[] args,
        Action<WebApplicationBuilder>? configureBuilder = null) {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
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

        builder.AddServiceDefaults();

        var app = builder.Build();

        await app.BootUmbracoAsync();

        if(!app.Configuration.GetValue<bool>("Shorthand:UmbracoCodeFirst:DisableStartupSynchronization")) {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider
                .GetRequiredService<ICodeFirstSynchronizer>()
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
