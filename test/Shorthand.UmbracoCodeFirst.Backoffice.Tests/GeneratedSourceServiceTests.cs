using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Shorthand.UmbracoCodeFirst.Backoffice.Controllers;
using Shorthand.UmbracoCodeFirst.Backoffice.Contracts;
using Shorthand.UmbracoCodeFirst.Backoffice.GeneratedSources;
using Shorthand.UmbracoCodeFirst.Backoffice.Models;
using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;
using Umbraco.Cms.Web.Common.Authorization;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Tests;

public class GeneratedSourceServiceTests {
    [Fact]
    public void DevelopmentCatalog_AssignsDistinctIdsAndKeepsThemInstanceScoped() {
        // Arrange
        var environment = A.Fake<IHostEnvironment>();
        A.CallTo(() => environment.EnvironmentName).Returns(Environments.Development);
        var registry = new TestRegistry();
        var service = new CodeFirstGeneratedSourceService(registry, environment, new TestCatalog());
        var nextService = new CodeFirstGeneratedSourceService(registry, environment, new TestCatalog());

        // Act
        var first = service.GetId(0).ShouldNotBeNull();
        var second = service.GetId(1).ShouldNotBeNull();

        // Assert
        first.ShouldNotBe(second);
        service.GetSources(first).ShouldNotBeNull().Single().Content.ShouldBe("first");
        service.GetSources(second).ShouldNotBeNull().Single().Content.ShouldBe("second");
        service.GetSources("invalid").ShouldBeNull();
        nextService.GetSources(first).ShouldBeNull();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void NonDevelopmentEnvironment_DoesNotExposeGeneratedSources(string environmentName) {
        // Arrange
        var environment = A.Fake<IHostEnvironment>();
        A.CallTo(() => environment.EnvironmentName).Returns(environmentName);
        var service = new CodeFirstGeneratedSourceService(new TestRegistry(), environment, new TestCatalog());

        // Act
        var id = service.GetId(0);

        // Assert
        id.ShouldBeNull();
        service.GetSources("invalid").ShouldBeNull();
    }

    [Fact]
    public void CustomRegistryWithoutCatalog_DoesNotExposeSources() {
        // Arrange
        var environment = A.Fake<IHostEnvironment>();
        A.CallTo(() => environment.EnvironmentName).Returns(Environments.Development);

        // Act
        var service = new CodeFirstGeneratedSourceService(new TestRegistry(), environment);

        // Assert
        service.GetId(0).ShouldBeNull();
        service.GetSources("unknown").ShouldBeNull();
    }

    [Fact]
    public void Endpoint_ReturnsSourcesOnlyForKnownId() {
        // Arrange
        var service = A.Fake<ICodeFirstGeneratedSourceService>();
        IReadOnlyList<CodeFirstGeneratedSource> sources = [new("file.cs", "source")];
        A.CallTo(() => service.GetSources("valid")).Returns(sources);
        A.CallTo(() => service.GetSources("invalid")).Returns(null);
        var controller = new ShorthandUmbracoCodeFirstBackofficeApiController();

        // Act
        var valid = controller.GeneratedSource("valid", service);
        var missing = controller.GeneratedSource("invalid", service);

        // Assert
        valid.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(sources);
        missing.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public void StatusMapping_PreservesSourceIdentityWhenSortingDuplicateAliases() {
        // Arrange
        var environment = A.Fake<IHostEnvironment>();
        A.CallTo(() => environment.EnvironmentName).Returns(Environments.Development);
        var registry = new TestRegistry();
        var service = new CodeFirstGeneratedSourceService(registry, environment, new TestCatalog());

        // Act
        var response = CodeFirstStatusMapping.Map(
            registry, A.Fake<ISynchronizationReportStore>(), generatedSources: service);

        // Assert
        response.DataTypes.Select(item => item.GeneratedSourceId)
            .ShouldBe([service.GetId(0), service.GetId(1)]);
    }

    [Fact]
    public void GeneratedSourceEndpoint_RequiresSettingsAuthorization() {
        // Arrange
        var method = typeof(ShorthandUmbracoCodeFirstBackofficeApiController)
            .GetMethod(nameof(ShorthandUmbracoCodeFirstBackofficeApiController.GeneratedSource)).ShouldNotBeNull();

        // Act
        var policies = method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>();

        // Assert
        policies.ShouldContain(attribute => attribute.Policy == AuthorizationPolicies.SectionAccessSettings);
        method.GetCustomAttributes(typeof(ResponseCacheAttribute), true).Cast<ResponseCacheAttribute>()
            .Single().NoStore.ShouldBeTrue();
    }

    private sealed class TestRegistry : ICodeFirstDefinitionRegistry {
        public IReadOnlyList<CodeFirstDefinition> Definitions { get; } = [
            new(CodeFirstDefinitionKind.DataType, "same", "First", "editor", []),
            new(CodeFirstDefinitionKind.DataType, "same", "Second", "editor", []),
        ];
    }

    private sealed class TestCatalog : ICodeFirstGeneratedSourceCatalog {
        public IReadOnlyList<CodeFirstGeneratedSource> GetSources(int definitionIndex) => definitionIndex switch {
            0 => [new("first.g.cs", "first")],
            1 => [new("second.g.cs", "second")],
            _ => [],
        };
    }
}
