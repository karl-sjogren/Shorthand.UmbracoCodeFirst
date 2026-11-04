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
        var dataType = new UCFDataTypeAttribute("Code First Text", "Umbraco.TextBox");
        var property = new UCFPropertyAttribute("Description", "codeFirstText");

        // Act
        var dataTypeAlias = dataType.Alias;
        var propertyAlias = property.Alias;

        // Assert
        dataTypeAlias.ShouldBeNull();
        dataType.Name.ShouldBe("Code First Text");
        propertyAlias.ShouldBeNull();
        property.Name.ShouldBe("Description");
        property.DataTypeAlias.ShouldBe("codeFirstText");
    }

    [Fact]
    public void UCFTabAttribute_NameAndSortOrder_LeavesAliasNull() {
        var attribute = new UCFTabAttribute("Product details", 2);

        attribute.Alias.ShouldBeNull();
        attribute.Name.ShouldBe("Product details");
        attribute.SortOrder.ShouldBe(2);
    }
}
