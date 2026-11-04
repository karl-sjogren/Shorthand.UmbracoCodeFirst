using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.RepeatedLayoutAnalyzer>;
using VerifyFixCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpCodeFixVerifier<Shorthand.UmbracoCodeFirst.Analyzers.RepeatedLayoutAnalyzer, Shorthand.UmbracoCodeFirst.Analyzers.RepeatedLayoutCodeFixProvider>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class RepeatedLayoutAnalyzerTests : AnalyzerTestBase {
    [Fact]
    public async Task RepeatedGroupAlias_ReportsDiagnosticAsync() {
        await VerifyCS.VerifyAnalyzerAsync(
            Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("product", "Product")]
                public partial class Product {
                    [UCFProperty("name", "Name", "text")]
                    [UCFGroup("details", "Details")]
                    public string Name { get; } = null!;
                    [UCFProperty("sku", "SKU", "text")]
                    [[|UCFGroup("details", "Details")|]]
                    public string Sku { get; } = null!;
                }
                """));
    }

    [Fact]
    public async Task RepeatedGroupAlias_CodeFixCreatesDerivedAttributeAsync() {
        await VerifyFixCS.VerifyCodeFixAsync(
            Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("product", "Product")]
                public partial class Product {
                    [UCFGroup("details", "Details")]
                    public string Name { get; } = null!;
                    [[|UCFGroup("details", "Details")|]]
                    public string Sku { get; } = null!;
                }
                """),
            SourceWithAppended("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("product", "Product")]
                public partial class Product {
                    [DetailsGroupAttribute]
                    public string Name { get; } = null!;
                    [DetailsGroupAttribute]
                    public string Sku { get; } = null!;
                }
                """,
            """
                [global::System.AttributeUsage(global::System.AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
                public sealed class DetailsGroupAttribute : UCFGroupAttribute {
                    public DetailsGroupAttribute() : base("details", "Details") {
                    }
                }
                """));
    }

    [Fact]
    public async Task RepeatedTabAliasWithSortOrder_CodeFixCreatesDerivedAttributeAsync() {
        await VerifyFixCS.VerifyCodeFixAsync(
            Source("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("product", "Product")]
                public partial class Product {
                    [UCFTab("Details", 1)]
                    public string Name { get; } = null!;
                    [[|UCFTab("Details", 1)|]]
                    public string Sku { get; } = null!;
                }
                """),
            SourceWithAppended("""
                using Shorthand.UmbracoCodeFirst.Attributes;
                [UCFDocumentType("product", "Product")]
                public partial class Product {
                    [DetailsTabAttribute]
                    public string Name { get; } = null!;
                    [DetailsTabAttribute]
                    public string Sku { get; } = null!;
                }
                """,
            """
                [global::System.AttributeUsage(global::System.AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
                public sealed class DetailsTabAttribute : UCFTabAttribute {
                    public DetailsTabAttribute() : base("Details", 1) {
                    }
                }
                """));
    }
}
