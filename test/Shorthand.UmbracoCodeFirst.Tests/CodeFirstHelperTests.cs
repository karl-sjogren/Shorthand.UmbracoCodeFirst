using System.Linq.Expressions;
using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Tests;

public class CodeFirstHelperTests {
    [Fact]
    public void Alias_UsesGeneratedAliasForDeclaredAndInheritedProperties() {
        // Arrange
        Expression<Func<DerivedModel, object>> boxed = model => model.Count;

        // Act
        var title = CodeFirstHelper<DerivedModel>.Alias(model => model.Title);
        var count = CodeFirstHelper<DerivedModel>.Alias(boxed);

        // Assert
        title.ShouldBe("customTitle");
        count.ShouldBe("itemCount");
    }

    [Fact]
    public void Alias_RejectsExpressionsThatAreNotDirectCodeFirstProperties() {
        // Arrange
        Expression<Func<Model, string>> nested = model => model.Child.Title;
        Expression<Func<Model, string>> method = model => model.Title.ToUpperInvariant();
        Expression<Func<Model, int>> field = model => model.Field;
        Expression<Func<Model, string>> unmarked = model => model.Unmarked;
        Expression<Func<Model, long>> converted = model => (long)model.Count;
        Expression<Func<Model, string>> staticProperty = _ => Model.StaticValue;

        // Act & Assert
        Should.Throw<ArgumentException>(() => CodeFirstHelper<Model>.Alias(nested));
        Should.Throw<ArgumentException>(() => CodeFirstHelper<Model>.Alias(method));
        Should.Throw<ArgumentException>(() => CodeFirstHelper<Model>.Alias(field));
        Should.Throw<ArgumentException>(() => CodeFirstHelper<Model>.Alias(unmarked));
        Should.Throw<ArgumentException>(() => CodeFirstHelper<Model>.Alias(converted));
        Should.Throw<ArgumentException>(() => CodeFirstHelper<Model>.Alias(staticProperty));
        Should.Throw<ArgumentNullException>(() => CodeFirstHelper<Model>.Alias<string>(null!));
    }

    [Fact]
    public void Alias_RejectsMissingGeneratedAlias() {
        // Arrange
        Expression<Func<MissingAliases, string>> selector = model => model.Title;

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => CodeFirstHelper<MissingAliases>.Alias(selector))
            .Message.ShouldContain("no generated code-first property alias");
    }

    private class Model {
        [UCFProperty("customTitle", "Display heading", "text")]
        public string Title { get; set; } = string.Empty;

        [UCFProperty("Item count", "number")]
        public int Count { get; set; }

        public Model Child { get; set; } = null!;
        public int Field = 1;
        public string Unmarked { get; set; } = string.Empty;
        public static string StaticValue => string.Empty;

        public static class PropertyAliases {
            public const string Title = "customTitle";
            public const string Count = "itemCount";
        }
    }

    private sealed class DerivedModel : Model;

    private sealed class MissingAliases {
        [UCFProperty("Title", "text")]
        public string Title { get; set; } = string.Empty;
    }
}
