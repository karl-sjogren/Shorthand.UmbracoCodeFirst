namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class AnalyzerInfrastructureTests : AnalyzerTestBase {
    [Fact]
    public void AnalyzerDescriptors_HaveDocumentationLinks_ReturnFeatureBranchUris() {
        const string documentationBaseUrl =
            "https://github.com/karl-sjogren/Shorthand.UmbracoCodeFirst/blob/" +
            "feature/initial-implementation/docs/analyzers";
        var descriptors = AllAnalyzers
            .SelectMany(static analyzer => analyzer.SupportedDiagnostics)
            .ToDictionary(
                descriptor => descriptor.Id,
                descriptor => descriptor.HelpLinkUri);

        descriptors.ShouldBe(new Dictionary<string, string> {
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
            ["UCF110"] = $"{documentationBaseUrl}/UCF110.md",
        });
    }

    [Fact]
    public void AnalyzerAssembly_LoadProjectAssembly_LoadsSuccessfully() {
        typeof(AnalyzerInfrastructureTests).Assembly.GetName().Name.ShouldBe(
            "Shorthand.UmbracoCodeFirst.Analyzers.Tests");
    }
}
