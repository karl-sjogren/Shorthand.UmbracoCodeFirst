namespace Shorthand.UmbracoCodeFirst.AppHost.Tests;

public class AppHostProjectTests {
    [Fact]
    public void AppHostAssembly_LoadProjectAssembly_LoadsSuccessfully() {
        // Arrange
        const string expectedName = "Shorthand.UmbracoCodeFirst.AppHost";

        // Act
        var assembly = typeof(Projects.Shorthand_UmbracoCodeFirst_Umbraco).Assembly;

        // Assert
        assembly.GetName().Name.ShouldBe(expectedName);
    }
}
