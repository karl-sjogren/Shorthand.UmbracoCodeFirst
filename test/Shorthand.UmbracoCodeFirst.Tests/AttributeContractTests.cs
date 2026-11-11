using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.PropertyEditors;

namespace Shorthand.UmbracoCodeFirst.Tests;

public class AttributeContractTests {
    [Fact]
    public void UCFDocumentTypeAttribute_ExplicitSettings_PreservesValues() {
        // Arrange
        var attribute = new UCFDocumentTypeAttribute("productPage", "Product Page") {
            Icon = UmbracoIcons.Barcode,
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
        attribute.Icon.ShouldBe(UmbracoIcons.Barcode);
        attribute.AllowedTemplateAliases.ShouldBe(["productTemplate", "default"]);
        attribute.AllowedChildAliases.ShouldBe(["variant"]);
    }

    [Fact]
    public void UCFElementTypeAttribute_ExplicitSettings_PreservesValues() {
        // Arrange
        // Act
        var attribute = new UCFElementTypeAttribute("productElement", "Product Element") {
            Icon = "icon-custom",
            AllowInLibrary = true,
            Variations = "Culture",
        };

        // Assert
        attribute.Alias.ShouldBe("productElement");
        attribute.Name.ShouldBe("Product Element");
        attribute.AllowInLibrary.ShouldBeTrue();
        attribute.Icon.ShouldBe("icon-custom");
        attribute.Variations.ShouldBe("Culture");
    }

    [Theory]
    [InlineData("Documents/Products")]
    [InlineData("/")]
    [InlineData("")]
    public void UCFFolderPathAttribute_PreservesConfiguredPath(string path) {
        // Arrange
        // Act
        var attribute = new UCFFolderPathAttribute(path);

        // Assert
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
        // Arrange
        // Act
        var attribute = new UCFDataTypeAttribute(
            "Code First Text",
            "Umbraco.TextBox",
            "Umb.PropertyEditorUi.TextBox") {
            Alias = "customText",
        };

        // Assert
        attribute.Alias.ShouldBe("customText");
    }

    [Fact]
    public void UCFDataTypeAttribute_EditorType_ReadsAliasesFromEditorType() {
        // Arrange
        // Act
        var attribute = new UCFDataTypeAttribute("Code First Text", typeof(TestEditor));

        // Assert
        attribute.EditorAlias.ShouldBe("Umbraco.TextBox");
        attribute.EditorUiAlias.ShouldBe("Umb.PropertyEditorUi.TextBox");
        attribute.Editor.ShouldBe(typeof(TestEditor));
    }

    [Fact]
    public void UCFDataTypeAttribute_EditorAndConfigurationTypes_PreserveConfigurationType() {
        // Arrange
        // Act
        var attribute = new UCFDataTypeAttribute(
            "Code First Text",
            typeof(TestEditor),
            typeof(TestConfiguration));

        // Assert
        attribute.ConfigurationType.ShouldBe(typeof(TestConfiguration));
    }

    [Fact]
    public void CodeFirstEditorConfiguration_FromJson_CanonicalizesObjectProperties() {
        // Arrange
        // Act
        var configuration = CodeFirstEditorConfiguration.FromJson(
            "Umbraco.TextBox",
            """{"z":true,"a":{"y":2,"x":1}}""");

        // Assert
        configuration.Json.ShouldBe("""{"a":{"x":1,"y":2},"z":true}""");
        configuration.SchemaVersion.ShouldBe(1);
    }

    [Fact]
    public void CodeFirstEditorConfiguration_FromType_SerializesTypedDefinition() {
        // Arrange
        // Act
        var configuration = CodeFirstEditorConfiguration.FromType(
            typeof(TestTextConfiguration),
            "Umbraco.TextBox");

        // Assert
        configuration.Json.ShouldBe("""{"maxChars":255}""");
        configuration.EditorId.ShouldBe("Umbraco.TextBox");
    }

    [Fact]
    public void CodeFirstEditorConfiguration_FromType_KeepsTypedBlockMetadataOutOfJson() {
        // Arrange
        // Act
        var configuration = CodeFirstEditorConfiguration.FromType(typeof(TestBlockConfiguration), "Umbraco.BlockList");

        // Assert
        configuration.Json.ShouldBe("""{"useLiveEditing":true}""");
        configuration.BlockTypes.ShouldNotBeNull().Count.ShouldBe(1);
        CodeFirstBlockReferences.GetAlias(configuration.BlockTypes[0].Content).ShouldBe("testElement");
        configuration.TypedSettings.ShouldNotBeNull()["validationLimit"].GetRawText().ShouldBe("""{"min":1,"max":3}""");
    }

    [Fact]
    public void CodeFirstEditorConfiguration_FromType_SerializesTypedRichTextSettings() {
        // Arrange
        // Act
        var configuration = CodeFirstEditorConfiguration.FromType(typeof(TestRichTextConfiguration), "Umbraco.RichText");

        // Assert
        configuration.Json.ShouldBe("{}");
        var settings = configuration.TypedSettings.ShouldNotBeNull();
        settings["toolbar"].GetRawText().ShouldBe("""[[["bold","italic"]]]""");
        settings["statusbar"].GetRawText().ShouldBe("""[["words"]]""");
        settings["dimensions"].GetRawText().ShouldBe("""{"height":400}""");
        settings["stylesheets"].GetRawText().ShouldBe("""["/editor.css"]""");
        configuration.BlockTypes.ShouldNotBeNull().ShouldBeEmpty();
    }

    [Fact]
    public void RichTextConfiguration_DefaultCollectionsAreMutableAndIndependent() {
        // Arrange
        var customized = new UmbracoEditors.RichTextEditor.Configuration();
        var defaults = new UmbracoEditors.RichTextEditor.Configuration();

        // Act
        customized.Extensions.ShouldNotBeNull().Remove("Umb.Tiptap.Bold");
        customized.Extensions.Add("Example.Extension");
        customized.ToolbarRows.ShouldNotBeNull()[0][1].Remove("Umb.Tiptap.Toolbar.Bold");
        customized.ToolbarRows[0][1].Add("Example.Toolbar");

        // Assert
        defaults.Extensions.ShouldNotBeNull().ShouldContain("Umb.Tiptap.Bold");
        defaults.Extensions.ShouldNotContain("Example.Extension");
        defaults.ToolbarRows.ShouldNotBeNull()[0][1].ShouldContain("Umb.Tiptap.Toolbar.Bold");
        defaults.ToolbarRows[0][1].ShouldNotContain("Example.Toolbar");
        var otherEditor = new UmbracoEditors.BlockList.Configuration();
        otherEditor.ToolbarRows.ShouldBeNull();
    }

    [Fact]
    public void CodeFirstEditorConfiguration_FromType_SerializesCustomizedRichTextDefaults() {
        // Arrange
        // Act
        var configuration = CodeFirstEditorConfiguration.FromType(
            typeof(CustomizedRichTextConfiguration), "Umbraco.RichText");

        // Assert
        using var json = System.Text.Json.JsonDocument.Parse(configuration.Json);
        var extensions = json.RootElement.GetProperty("extensions").EnumerateArray()
            .Select(item => item.GetString()).ToArray();
        extensions.ShouldContain("Example.Extension");
        extensions.ShouldNotContain("Umb.Tiptap.Bold");
        json.RootElement.GetProperty("maxImageSize").GetInt32().ShouldBe(500);
        json.RootElement.GetProperty("overlaySize").GetString().ShouldBe("medium");
        var group = configuration.TypedSettings.ShouldNotBeNull()["toolbar"][0][1].EnumerateArray()
            .Select(item => item.GetString()).ToArray();
        group.ShouldContain("Example.Toolbar");
        group.ShouldNotContain("Umb.Tiptap.Toolbar.Bold");
    }

    [Fact]
    public void CodeFirstEditorConfiguration_FromType_AllowsRawRichTextToolbar() {
        // Arrange
        // Act
        var configuration = CodeFirstEditorConfiguration.FromType(
            typeof(RawRichTextConfiguration), "Umbraco.RichText");

        // Assert
        using var json = System.Text.Json.JsonDocument.Parse(configuration.Json);
        json.RootElement.GetProperty("toolbar")[0][0][0].GetString().ShouldBe("Example.Toolbar");
        json.RootElement.TryGetProperty("extensions", out _).ShouldBeFalse();
        configuration.TypedSettings.ShouldBeNull();
    }

    [Fact]
    public void UCFTabAttribute_NameAndSortOrder_LeavesAliasNull() {
        // Arrange
        // Act
        var attribute = new UCFTabAttribute("Product details", 2);

        // Assert
        attribute.Alias.ShouldBeNull();
        attribute.Name.ShouldBe("Product details");
        attribute.SortOrder.ShouldBe(2);
    }

    private static class TestEditor {
        public const string Alias = "Umbraco.TextBox";
        public const string UiAlias = "Umb.PropertyEditorUi.TextBox";
    }

    private static class TestConfiguration {
        public const string Json = """{"maxChars":255}""";
    }

    private sealed class TestTextConfiguration : CodeFirstEditorConfigurationDefinition {
        public int? MaxChars { get; set; } = 255;
    }

    [UCFElementType("testElement", "Test Element")]
    private sealed class TestElement {
        public const string ModelTypeAlias = "testElement";
    }

    private sealed class TestBlockConfiguration : UmbracoEditors.BlockList.Configuration {
        public TestBlockConfiguration() {
            UseLiveEditing = true;
            BlockTypes = [new(typeof(TestElement))];
            ValidationRange = new() { Min = 1, Max = 3 };
        }
    }

    private sealed class TestRichTextConfiguration : UmbracoEditors.RichTextEditor.Configuration {
        public TestRichTextConfiguration() {
            Extensions = null;
            MaxImageSize = null;
            OverlaySize = null;
            BlockTypes = [];
            ToolbarRows = [[["bold", "italic"]]];
            StatusbarGroups = [["words"]];
            EditorDimensions = new() { Height = 400 };
            StylesheetPaths = ["/editor.css"];
        }
    }

    private sealed class CustomizedRichTextConfiguration : UmbracoEditors.RichTextEditor.Configuration {
        public CustomizedRichTextConfiguration() {
            Extensions!.Remove("Umb.Tiptap.Bold");
            Extensions.Add("Example.Extension");
            ToolbarRows![0][1].Remove("Umb.Tiptap.Toolbar.Bold");
            ToolbarRows[0][1].Add("Example.Toolbar");
        }
    }

    private sealed class RawRichTextConfiguration : UmbracoEditors.RichTextEditor.Configuration {
        public RawRichTextConfiguration() {
            Extensions = null;
            ToolbarRows = null;
            Toolbar = System.Text.Json.JsonSerializer.SerializeToElement(new[] { new[] { new[] { "Example.Toolbar" } } });
        }
    }
}
