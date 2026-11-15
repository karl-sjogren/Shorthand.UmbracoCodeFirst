using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.SourceGenerators;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class GeneratedConsumerAnalyzerTests {
    [Theory]
    [InlineData("ConfigurationJson = \"{\\\"multiple\\\":false}\"", "string?", true)]
    [InlineData("ConfigurationJson = \"{\\\"multiple\\\":false}\"", "Umbraco.Cms.Core.Models.MediaWithCrops?", false)]
    [InlineData("ConfigurationJson = \"{\\\"multiple\\\":false}\"", "System.Collections.Generic.IEnumerable<Umbraco.Cms.Core.Models.MediaWithCrops>", true)]
    [InlineData("typeof(MediaConfiguration)", "Umbraco.Cms.Core.Models.MediaWithCrops?", true)]
    [InlineData("typeof(MediaConfiguration)", "System.Collections.Generic.IEnumerable<Umbraco.Cms.Core.Models.MediaWithCrops>", false)]
    [InlineData("ConfigurationJson = \"{}\"", "Umbraco.Cms.Core.Models.MediaWithCrops?", false)]
    [InlineData("ConfigurationJson = \"{}\"", "System.Collections.Generic.IEnumerable<Umbraco.Cms.Core.Models.MediaWithCrops>", false)]
    public async Task GeneratorAndAnalyzers_UseGeneratedAliasAndPartialPropertyAsync(
        string configuration, string propertyType, bool incompatible) {
        // Arrange
        var source = $$"""
            #nullable enable
            using Shorthand.UmbracoCodeFirst.Attributes;
            using Shorthand.UmbracoCodeFirst.PropertyEditors;
            public class MediaConfiguration : UmbracoEditors.MediaPicker.Configuration {
                public MediaConfiguration() { Multiple = true; }
            }
            [UCFDataType("Picker", typeof(UmbracoEditors.MediaPicker), {{configuration}})]
            public partial class Picker;
            [UCFDocument("Page", DefaultTemplateAlias = "page", AllowedTemplateAliases = new[] { "page" })]
            public partial class Page {
                [UCFProperty("Image", Picker.Alias)]
                public partial {{propertyType}} Image { get; }
            }
            namespace Umbraco.Cms.Core {
                public enum PublishedItemType { Element, Content }
            }
            namespace Umbraco.Cms.Core.Models {
                public class MediaWithCrops { }
            }
            namespace Umbraco.Cms.Core.Models.PublishedContent {
                public interface IPublishedContent { }
                public interface IPublishedValueFallback { }
                public interface IPublishedContentType { }
                public interface IPublishedPropertyType { }
                public class PublishedContentModel(IPublishedContent content, IPublishedValueFallback fallback) {
                    public T? Value<T>(IPublishedValueFallback fallback, string alias) => default;
                }
            }
            namespace Umbraco.Cms.Core.PublishedCache {
                public interface IPublishedContentTypeCache { }
            }
            namespace Umbraco.Cms.Infrastructure.ModelsBuilder {
                using System;
                using System.Linq.Expressions;
                using Umbraco.Cms.Core;
                using Umbraco.Cms.Core.Models.PublishedContent;
                using Umbraco.Cms.Core.PublishedCache;
                public class PublishedModelAttribute(string alias) : Attribute;
                public class ImplementPropertyTypeAttribute(string alias) : Attribute;
                public static class PublishedModelUtility {
                    public static IPublishedContentType GetModelContentType(
                        IPublishedContentTypeCache cache, PublishedItemType type, string alias) => null!;
                    public static IPublishedPropertyType GetModelPropertyType<T, TValue>(
                        IPublishedContentType type, Expression<Func<T, TValue>> selector) => null!;
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(UCFDataTypeAttribute).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "GeneratedConsumerAnalyzerTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new CodeFirstRegistryGenerator().AsSourceGenerator());

        // Act
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var output, out var generatorDiagnostics, TestContext.Current.CancellationToken);
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
            new IncompatibleEditorTypeAnalyzer(), new EditorAliasAnalyzer(),
            new DefaultTemplateNotAllowedAnalyzer(), new InvalidAliasAnalyzer());
        var diagnostics = await output.WithAnalyzers(analyzers)
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        // Assert
        generatorDiagnostics.ShouldBeEmpty();
        output.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        driver.GetRunResult().GeneratedTrees.ShouldContain(tree =>
            tree.FilePath.EndsWith(".PublishedModel.g.cs", StringComparison.Ordinal));
        if(incompatible) {
            var diagnostic = diagnostics.ShouldHaveSingleItem();
            diagnostic.Id.ShouldBe("UCF109");
            diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
            diagnostic.GetMessage().ShouldContain("Image");
            diagnostic.GetMessage().ShouldContain("Umbraco.MediaPicker3");
            var text = await diagnostic.Location.SourceTree.ShouldNotBeNull().GetTextAsync(TestContext.Current.CancellationToken);
            text.ToString(diagnostic.Location.SourceSpan).ShouldBe("Image");
        } else {
            diagnostics.ShouldBeEmpty();
        }
    }
}
