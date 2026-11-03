using System.Reflection;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class AnalyzerProjectTests {
    [Fact]
    public void AnalyzerAssembly_LoadProjectAssembly_LoadsSuccessfully() {
        // Arrange
        const string expectedName = "Shorthand.UmbracoCodeFirst.Analyzers.Tests";

        // Act
        Assembly assembly = typeof(AnalyzerProjectTests).Assembly;

        // Assert
        assembly.GetName().Name.ShouldBe(expectedName);
    }
}
