using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Shorthand.UmbracoCodeFirst.ServiceDefaults.Tests;

public class ServiceDefaultsTests {
    [Fact]
    public async Task AddServiceDefaultsAsync_DevelopmentEnvironment_RegistersHealthyChecks_Async() {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddServiceDefaults();
        WebApplication app = builder.Build();

        // Act
        app.MapDefaultEndpoints();

        // Assert
        (await app.Services
            .GetRequiredService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>()
            .CheckHealthAsync(TestContext.Current.CancellationToken))
            .Status
            .ShouldBe(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy);
    }
}
