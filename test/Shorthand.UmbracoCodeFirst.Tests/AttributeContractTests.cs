using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Tests;

public class AttributeContractTests {
    [Fact]
    public void UCFDocumentTypeAttribute_ExplicitSettings_PreservesValues() {
        // Arrange
        var attribute = new UCFDocumentTypeAttribute("productPage", "Product Page") {
            Icon = "icon-barcode",
            AllowAsRoot = true,
            DefaultTemplateAlias = "productTemplate",
            AllowedTemplateAliases = ["productTemplate", "default"],
            AllowedChildAliases = ["variant"],
            Variations = "Culture",
            IsListView = true,
        };

        // Act
        var alias = attribute.Alias;

        // Assert
        alias.ShouldBe("productPage");
        attribute.Name.ShouldBe("Product Page");
        attribute.AllowedTemplateAliases.ShouldBe(["productTemplate", "default"]);
        attribute.AllowedChildAliases.ShouldBe(["variant"]);
    }

    [Fact]
    public void UCFElementTypeAttribute_ExplicitSettings_PreservesValues() {
        var attribute = new UCFElementTypeAttribute("productElement", "Product Element") {
            Icon = "icon-barcode",
            AllowInLibrary = true,
            Variations = "Culture",
        };

        attribute.Alias.ShouldBe("productElement");
        attribute.Name.ShouldBe("Product Element");
        attribute.AllowInLibrary.ShouldBeTrue();
        attribute.Variations.ShouldBe("Culture");
    }

    [Theory]
    [InlineData("Documents/Products")]
    [InlineData("/")]
    [InlineData("")]
    public void UCFFolderPathAttribute_PreservesConfiguredPath(string path) {
        var attribute = new UCFFolderPathAttribute(path);

        attribute.Path.ShouldBe(path);
    }

    [Fact]
    public void UCFDataTypeAttributeAndUCFPropertyAttribute_WithoutExplicitAliases_LeaveAliasesNull() {
        // Arrange
        var dataType = new UCFDataTypeAttribute(
            "Code First Text",
            "Umbraco.TextBox",
            "Umb.PropertyEditorUi.TextBox");
        var property = new UCFPropertyAttribute("Description", "codeFirstText");

        // Act
        var dataTypeAlias = dataType.Alias;
        var propertyAlias = property.Alias;

        // Assert
        dataTypeAlias.ShouldBeNull();
        dataType.Name.ShouldBe("Code First Text");
        dataType.EditorAlias.ShouldBe("Umbraco.TextBox");
        dataType.EditorUiAlias.ShouldBe("Umb.PropertyEditorUi.TextBox");
        propertyAlias.ShouldBeNull();
        property.Name.ShouldBe("Description");
        property.DataTypeAlias.ShouldBe("codeFirstText");
    }

    [Fact]
    public void UCFDataTypeAttribute_ExplicitAlias_PreservesAlias() {
        var attribute = new UCFDataTypeAttribute(
            "Code First Text",
            "Umbraco.TextBox",
            "Umb.PropertyEditorUi.TextBox") {
            Alias = "customText",
        };

        attribute.Alias.ShouldBe("customText");
    }

    [Fact]
    public void UCFDataTypeAttribute_EditorType_ReadsAliasesFromEditorType() {
        var attribute = new UCFDataTypeAttribute("Code First Text", typeof(TestEditor));

        attribute.EditorAlias.ShouldBe("Umbraco.TextBox");
        attribute.EditorUiAlias.ShouldBe("Umb.PropertyEditorUi.TextBox");
        attribute.Editor.ShouldBe(typeof(TestEditor));
    }

    [Fact]
    public void UCFTabAttribute_NameAndSortOrder_LeavesAliasNull() {
        var attribute = new UCFTabAttribute("Product details", 2);

        attribute.Alias.ShouldBeNull();
        attribute.Name.ShouldBe("Product details");
        attribute.SortOrder.ShouldBe(2);
    }

    private static class TestEditor {
        public const string Alias = "Umbraco.TextBox";
        public const string UiAlias = "Umb.PropertyEditorUi.TextBox";
    }
}
