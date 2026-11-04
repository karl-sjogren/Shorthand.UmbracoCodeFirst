using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Tests;

public class AttributeContractTests {
    [Fact]
    public void UCFContentTypeAttribute_ExplicitSettings_PreservesValues() {
        // Arrange
        var attribute = new UCFContentTypeAttribute("productPage", "Product Page") {
            Icon = "icon-barcode",
            IsElement = true,
            AllowAsRoot = true,
            AllowInLibrary = true,
            DefaultTemplateAlias = "productTemplate",
            AllowedTemplateAliases = ["productTemplate", "default"],
            AllowedChildAliases = ["variant"],
            Variations = "Culture",
            IsListView = true,
        };

        // Act
        string? alias = attribute.Alias;

        // Assert
        alias.ShouldBe("productPage");
        attribute.Name.ShouldBe("Product Page");
        attribute.AllowedTemplateAliases.ShouldBe(["productTemplate", "default"]);
        attribute.AllowedChildAliases.ShouldBe(["variant"]);
    }

    [Fact]
    public void UCFDataTypeAttributeAndUCFPropertyAttribute_WithoutExplicitAliases_LeaveAliasesNull() {
        // Arrange
        var dataType = new UCFDataTypeAttribute("Code First Text", "Umbraco.TextBox");
        var property = new UCFPropertyAttribute("Description", "codeFirstText");

        // Act
        string? dataTypeAlias = dataType.Alias;
        string? propertyAlias = property.Alias;

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
