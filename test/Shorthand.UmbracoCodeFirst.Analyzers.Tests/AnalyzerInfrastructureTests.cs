namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class AnalyzerInfrastructureTests : AnalyzerTestBase {
    [Fact]
    public void AnalyzerDescriptors_HaveDocumentationLinks_ReturnFeatureBranchUris() {
        // Arrange
        const string documentationBaseUrl =
            "https://github.com/karl-sjogren/Shorthand.UmbracoCodeFirst/blob/" +
            "feature/initial-implementation/docs/analyzers";
        var descriptors = AllAnalyzers
            .SelectMany(static analyzer => analyzer.SupportedDiagnostics)
            .ToArray();

        // Act
        var links = descriptors.ToDictionary(
            descriptor => descriptor.Id,
            descriptor => descriptor.HelpLinkUri);

        // Assert
        links.ShouldBe(new Dictionary<string, string> {
            ["UCF100"] = $"{documentationBaseUrl}/UCF100.md",
            ["UCF101"] = $"{documentationBaseUrl}/UCF101.md",
            ["UCF102"] = $"{documentationBaseUrl}/UCF102.md",
            ["UCF103"] = $"{documentationBaseUrl}/UCF103.md",
            ["UCF104"] = $"{documentationBaseUrl}/UCF104.md",
            ["UCF105"] = $"{documentationBaseUrl}/UCF105.md",
            ["UCF106"] = $"{documentationBaseUrl}/UCF106.md",
            ["UCF107"] = $"{documentationBaseUrl}/UCF107.md",
            ["UCF108"] = $"{documentationBaseUrl}/UCF108.md",
            ["UCF109"] = $"{documentationBaseUrl}/UCF109.md",
        });
    }

    [Fact]
    public void AnalyzerAssembly_LoadProjectAssembly_LoadsSuccessfully() {
        // Arrange
        const string expectedName = "Shorthand.UmbracoCodeFirst.Analyzers.Tests";

        // Act
        var assembly = typeof(AnalyzerInfrastructureTests).Assembly;

        // Assert
        assembly.GetName().Name.ShouldBe(expectedName);
    }
}
