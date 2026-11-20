using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.SourceGenerators.Tests;

public class CompositePublishedModelTests {
    private const string _umbracoStubs = """
        namespace Umbraco.Cms.Core {
            public enum PublishedItemType { Content, Element }
        }
        namespace Umbraco.Cms.Core.Models.PublishedContent {
            public interface IPublishedElement { object? Read(string alias); }
            public interface IPublishedContent : IPublishedElement { }
            public interface IPublishedValueFallback { }
            public interface IPublishedContentType { }
            public interface IPublishedPropertyType { }
            public class PublishedElementModel(IPublishedElement content, IPublishedValueFallback fallback) : IPublishedElement {
                public object? Read(string alias) => content.Read(alias);
                public T? Value<T>(IPublishedValueFallback fallback, string alias) => (T?)Read(alias);
            }
            public class PublishedContentModel(IPublishedContent content, IPublishedValueFallback fallback)
                : PublishedElementModel(content, fallback), IPublishedContent { }
        }
        namespace Umbraco.Extensions {
            using Umbraco.Cms.Core.Models.PublishedContent;
            public static class PublishedExtensions {
                public static T? Value<T>(this IPublishedElement content, IPublishedValueFallback fallback, string alias)
                    => (T?)content.Read(alias);
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

    [Fact]
    public void Generator_ComposesDocumentsAndElementsThroughDiamondGraph() {
        // Arrange
        var source = """
            #nullable enable
            using Shorthand.UmbracoCodeFirst.Attributes;
            using Umbraco.Cms.Core.Models.PublishedContent;
            namespace Shared {
                [UCFDocument("base", "Base")]
                public partial class Base {
                    [UCFProperty("seoTitle", "SEO Title", "text")]
                    public partial string? Title { get; }
                }
                [UCFDocument("left", "Left")]
                [UCFComposite(typeof(Base))]
                public partial class Left {
                    [UCFProperty("Count", "text")]
                    public partial int Count { get; }
                }
                [UCFDocument("right", "Right")]
                [UCFComposite(typeof(Base))]
                public partial class Right;
                [UCFElementType("sharedElement", "Shared element")]
                public partial class SharedElement {
                    [UCFProperty("Caption", "text")]
                    public partial string? Caption { get; }
                }
            }
            namespace Pages {
                [UCFDocument("page", "Page")]
                [UCFComposite(typeof(Shared.Left))]
                [UCFComposite(typeof(Shared.Right))]
                public partial class Page {
                    [UCFProperty("Heading", "text")]
                    public partial string? Heading { get; }
                }
                [UCFElementType("block", "Block")]
                [UCFComposite(typeof(Shared.SharedElement))]
                public partial class Block;
                public sealed class Input : IPublishedContent {
                    public object? Read(string alias) => alias switch {
                        "seoTitle" => "SEO",
                        "count" => 42,
                        "heading" => "Heading",
                        "caption" => "Caption",
                        _ => null
                    };
                }
                public sealed class Fallback : IPublishedValueFallback;
                public static class Probe {
                    public static bool Run() {
                        var input = new Input();
                        var fallback = new Fallback();
                        var page = new Page(input, fallback);
                        var block = new Block(input, fallback);
                        Shared.IBase contract = page;
                        Shared.ISharedElement element = block;
                        return contract.Title == "SEO" && page.Title == "SEO" && page.Count == 42
                            && page.Heading == "Heading" && Shared.Base.GetTitle(contract, fallback) == "SEO"
                            && Shared.Left.GetCount(page, fallback) == 42
                            && element.Caption == "Caption" && block.Caption == "Caption"
                            && Page.PropertyAliases.Title == "seoTitle"
                            && Page.PropertyAliases.Count == "count"
                            && Block.PropertyAliases.Caption == "caption";
                    }
                }
            }
            """ + _umbracoStubs;
        var compilation = Compilation(source);

        // Act
        var (driver, output, diagnostics) = Generate(compilation);
        using var stream = new MemoryStream();
        var emitted = output.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        emitted.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        emitted.Success.ShouldBeTrue();
        var assembly = Assembly.Load(stream.ToArray());
        assembly.GetType("Pages.Probe")!.GetMethod("Run")!.Invoke(null, null).ShouldBe(true);
        var sources = driver.GetRunResult().Results.Single().GeneratedSources;
        var page = sources.Single(item => item.HintName.Contains("Pages_Page", StringComparison.Ordinal)).SourceText.ToString();
        var left = sources.Single(item => item.HintName.Contains("Shared_Left", StringComparison.Ordinal)).SourceText.ToString();
        var registry = sources.Single(item => item.HintName == "CodeFirstRegistry.g.cs").SourceText.ToString();
        page.Split("public global::System.String? Title", StringSplitOptions.None).Length.ShouldBe(2);
        left.ShouldContain("interface ILeft : IPublishedContent, global::Shared.IBase");
        registry.ShouldContain("new global::System.String[] { \"left\", \"right\" }");
    }

    [Theory]
    [InlineData("[UCFProperty(\"Another\", \"text\")] public partial string? Title { get; }", "Title")]
    [InlineData("public int Title => 0;", "Title")]
    public void Generator_ReportsComposedMemberConflicts(string member, string expectedName) {
        // Arrange
        var source = $$"""
            #nullable enable
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocument("Base")]
            public partial class Base {
                [UCFProperty("Title", "text")] public partial string? Title { get; }
            }
            [UCFDocument("Page")]
            [UCFComposite(typeof(Base))]
            public partial class Page { {{member}} }
            """ + _umbracoStubs;

        // Act
        var (_, output, diagnostics) = Generate(Compilation(source));

        // Assert
        diagnostics.ShouldContain(item => item.Id == "UCF125" && item.GetMessage().Contains(expectedName, StringComparison.Ordinal));
        output.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    }

    [Fact]
    public void Generator_ReportsGetterAndInterfaceConflicts() {
        // Arrange
        var source = """
            #nullable enable
            using Shorthand.UmbracoCodeFirst.Attributes;
            public interface IBase { }
            [UCFDocument("Base")]
            public partial class Base {
                [UCFProperty("Title", "text")] public partial string? Title { get; }
                public static void GetTitle() { }
            }
            [UCFDocument("Page")]
            [UCFComposite(typeof(Base))]
            public partial class Page;
            """ + _umbracoStubs;

        // Act
        var (_, output, diagnostics) = Generate(Compilation(source));

        // Assert
        diagnostics.ShouldContain(item => item.Id == "UCF125" && item.GetMessage().Contains("IBase", StringComparison.Ordinal));
        diagnostics.ShouldContain(item => item.Id == "UCF125" && item.GetMessage().Contains("GetTitle", StringComparison.Ordinal));
        output.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    }

    [Fact]
    public void Generator_ReportsConflictsBetweenDifferentComposites() {
        // Arrange
        var source = """
            #nullable enable
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocument("First")]
            public partial class First {
                [UCFProperty("firstTitle", "First title", "text")] public partial string? Title { get; }
            }
            [UCFDocument("Second")]
            public partial class Second {
                [UCFProperty("secondTitle", "Second title", "text")] public partial string? Title { get; }
            }
            [UCFDocument("Page")]
            [UCFComposite(typeof(First))]
            [UCFComposite(typeof(Second))]
            public partial class Page;
            """ + _umbracoStubs;

        // Act
        var (_, output, diagnostics) = Generate(Compilation(source));

        // Assert
        diagnostics.ShouldContain(item => item.Id == "UCF125"
            && item.GetMessage().Contains("Title", StringComparison.Ordinal));
        output.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    }

    [Fact]
    public void Generator_EscapesIdentifiersInCompositeModels() {
        // Arrange
        var source = """
            #nullable enable
            using Shorthand.UmbracoCodeFirst.Attributes;
            namespace @namespace {
                [UCFDocument("Composite")]
                public partial class @class {
                    [UCFProperty("Event", "text")] public partial string? @event { get; }
                }
                [UCFDocument("Page")]
                [UCFComposite(typeof(@class))]
                public partial class @return;
            }
            """ + _umbracoStubs;

        // Act
        var (driver, output, diagnostics) = Generate(Compilation(source));

        // Assert
        diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        output.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var model = driver.GetRunResult().GeneratedTrees.Single(tree =>
            tree.FilePath.Contains("return.PublishedModel", StringComparison.Ordinal)).ToString();
        model.ShouldContain("public global::System.String? @event");
        model.ShouldContain("global::@namespace.@class.Getevent");
    }

    [Fact]
    public void Generator_ReportsConflictsWithPublishedModelBaseMembers() {
        // Arrange
        var source = """
            #nullable enable
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocument("Base")]
            public partial class Base {
                [UCFProperty("Read", "text")] public partial string? Read { get; }
            }
            [UCFDocument("Page")]
            [UCFComposite(typeof(Base))]
            public partial class Page;
            """ + _umbracoStubs;

        // Act
        var (_, _, diagnostics) = Generate(Compilation(source));

        // Assert
        diagnostics.ShouldContain(item => item.Id == "UCF125"
            && item.GetMessage().Contains("Read", StringComparison.Ordinal));
    }

    [Fact]
    public void Generator_CyclicCompositionsDoNotCreateInterfaceCycles() {
        // Arrange
        var source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocument("First")]
            [UCFComposite(typeof(Second))]
            public partial class First;
            [UCFDocument("Second")]
            [UCFComposite(typeof(First))]
            public partial class Second;
            """ + _umbracoStubs;

        // Act
        var (driver, output, diagnostics) = Generate(Compilation(source));

        // Assert
        diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        output.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        driver.GetRunResult().GeneratedTrees
            .Where(tree => tree.FilePath.EndsWith(".PublishedModel.g.cs", StringComparison.Ordinal))
            .ShouldAllBe(tree => !tree.ToString().Contains("partial interface", StringComparison.Ordinal));
    }

    [Fact]
    public void Generator_ReferencedCompositeKeepsRegistryAliasWithoutGeneratedContract() {
        // Arrange
        var baseSource = """
            #nullable enable
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocument("base", "Base")]
            public partial class Base {
                [UCFProperty("Title", "text")] public partial string? Title { get; }
            }
            """ + _umbracoStubs;
        var (_, baseOutput, baseDiagnostics) = Generate(Compilation(baseSource));
        using var assembly = new MemoryStream();
        var emitted = baseOutput.Emit(assembly, cancellationToken: TestContext.Current.CancellationToken);
        baseDiagnostics.Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        emitted.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var consumer = Compilation("""
            using Shorthand.UmbracoCodeFirst.Attributes;
            [UCFDocument("page", "Page")]
            [UCFComposite(typeof(Base))]
            public partial class Page;
            """).WithAssemblyName("Consumer").AddReferences(MetadataReference.CreateFromImage(assembly.ToArray()));

        // Act
        var (driver, output, diagnostics) = Generate(consumer);

        // Assert
        diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        output.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var sources = driver.GetRunResult().Results.Single().GeneratedSources;
        sources.Single(item => item.HintName == "CodeFirstRegistry.g.cs").SourceText.ToString()
            .ShouldContain("new global::System.String[] { \"base\" }");
        sources.Single(item => item.HintName.EndsWith(".PublishedModel.g.cs", StringComparison.Ordinal)).SourceText.ToString()
            .ShouldNotContain("IBase");
    }

    private static CSharpCompilation Compilation(string source) =>
        CSharpCompilation.Create(
            "CompositeTests",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Append(typeof(UCFDocumentAttribute).Assembly.Location)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    private static (GeneratorDriver Driver, Compilation Output, ImmutableArray<Diagnostic> Diagnostics) Generate(
        Compilation compilation) {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new CodeFirstRegistryGenerator().AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var output, out var diagnostics, TestContext.Current.CancellationToken);
        return (driver, output, diagnostics);
    }
}
