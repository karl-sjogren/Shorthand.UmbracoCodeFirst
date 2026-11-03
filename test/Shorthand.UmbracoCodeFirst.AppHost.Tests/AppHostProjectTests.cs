using System.Reflection;

namespace Shorthand.UmbracoCodeFirst.AppHost.Tests;

public class AppHostProjectTests {
    [Fact]
    public void AppHostAssembly_LoadProjectAssembly_LoadsSuccessfully() {
        // Arrange
        const string expectedName = "Shorthand.UmbracoCodeFirst.AppHost";

        // Act
        Assembly assembly = typeof(Projects.Shorthand_UmbracoCodeFirst_Umbraco).Assembly;

        // Assert
        assembly.GetName().Name.ShouldBe(expectedName);
    }
}
