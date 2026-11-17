using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.SourceGenerators.Tests;

public class RealAttributeConfigurationTests {
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, false, true)]
    public void Generator_ValidatesConfigurationUsingRealAttributes(bool unsupported, bool namedArgument, bool typed) {
        // Arrange
        var settings = unsupported
            ? "public const Mode Choice = Mode.One;"
            : "public const string Choice = \"one\";";
        var attribute = namedArgument
            ? "[UCFDataType(\"Choice\", typeof(UmbracoEditors.TextBox), ConfigurationType = typeof(Settings))]"
            : "[UCFDataType(\"Choice\", typeof(UmbracoEditors.TextBox), typeof(Settings))]";
        var baseType = typed ? " : UmbracoEditors.TextBox.Configuration" : string.Empty;
        var source = $$"""
            using Shorthand.UmbracoCodeFirst.Attributes;
            using Shorthand.UmbracoCodeFirst.PropertyEditors;
            public enum Mode { One }
            public class Settings{{baseType}} { {{settings}} }
            {{attribute}}
            public partial class ChoiceDataType;
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(UCFDataTypeAttribute).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "RealAttributeConfigurationTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new CodeFirstRegistryGenerator().AsSourceGenerator());

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var output, out var diagnostics, TestContext.Current.CancellationToken);

        // Assert
        output.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        if(unsupported) {
            var diagnostic = diagnostics.ShouldHaveSingleItem();
            diagnostic.Id.ShouldBe("UCF112");
            diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
            diagnostic.GetMessage().ShouldContain("Choice");
            diagnostic.Location.SourceTree.ShouldNotBeNull().GetText(TestContext.Current.CancellationToken)
                .ToString(diagnostic.Location.SourceSpan).ShouldContain("Choice");
        } else {
            diagnostics.ShouldBeEmpty();
            var registry = driver.GetRunResult().Results.Single().GeneratedSources
                .Single(item => item.HintName == "CodeFirstRegistry.g.cs").SourceText.ToString();
            registry.ShouldContain("\\\"choice\\\":\\\"one\\\"");
        }
    }
}
