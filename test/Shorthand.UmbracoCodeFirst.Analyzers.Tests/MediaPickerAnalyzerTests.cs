using VerifyCS = Shorthand.UmbracoCodeFirst.Analyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<Shorthand.UmbracoCodeFirst.Analyzers.IncompatibleEditorTypeAnalyzer>;

namespace Shorthand.UmbracoCodeFirst.Analyzers.Tests;

public class MediaPickerAnalyzerTests {
    private const string _metadata = """
        {"editors":[{"alias":"Umbraco.MediaPicker3","returnType":"Umbraco.Cms.Core.Models.MediaWithCrops"}]}
        """;

    [Fact]
    public async Task PartialPropertyWithGeneratedImplementation_ReportsIncompatibleTypeAsync() {
        // Arrange
        var source = """
            using Shorthand.UmbracoCodeFirst.Attributes;
            namespace Shorthand.UmbracoCodeFirst.Attributes {
                public sealed class UCFDataTypeAttribute(string name, System.Type editor, System.Type configuration) : System.Attribute {
                    public string Alias { get; set; }
                }
                public sealed class UCFPropertyAttribute(string name, string dataTypeAlias) : System.Attribute;
            }
            public static class MediaPicker { public const string Alias = "Umbraco.MediaPicker3"; }
            public class Configuration { }
            [UCFDataType("Media Picker", typeof(MediaPicker), typeof(Configuration))]
            public partial class Picker { public const string Alias = "mediaPicker"; }
            public partial class Page {
                [UCFProperty("Image", Picker.Alias)]
                public partial string [|Image|] { get; }
            }
            public partial class Page {
                [System.CodeDom.Compiler.GeneratedCode("generator", "1")]
                public partial string Image => "";
            }
            """;

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("Umbraco.Cms.Core.Models.MediaWithCrops", true)]
    [InlineData("System.Collections.Generic.IEnumerable<Umbraco.Cms.Core.Models.MediaWithCrops>", false)]
    public async Task RawConfiguration_MaximumOneStillUsesMultipleShapeAsync(string type, bool diagnostic) {
        // Arrange
        var source = $$"""
            using Shorthand.UmbracoCodeFirst.Attributes;
            namespace Umbraco.Cms.Core.Models {
                public class MediaWithCrops { }
            }
            namespace Shorthand.UmbracoCodeFirst.Attributes {
                public sealed class UCFDataTypeAttribute(string name, System.Type editor) : System.Attribute {
                    public string Alias { get; set; }
                    public string ConfigurationJson { get; set; }
                }
                public sealed class UCFPropertyAttribute(string name, string dataTypeAlias) : System.Attribute;
            }
            public static class MediaPicker { public const string Alias = "Umbraco.MediaPicker3"; }
            [UCFDataType("Media Picker", typeof(MediaPicker),
                ConfigurationJson = "{\"multiple\":true,\"validationLimit\":{\"max\":1} }")]
            public class Picker { public const string Alias = "mediaPicker"; }
            public class Page {
                [UCFProperty("Image", Picker.Alias)]
                public {{type}} {{(diagnostic ? "[|Image|]" : "Image")}} { get; } = default!;
            }
            """;

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("false", "string", true)]
    [InlineData("false", "Umbraco.Cms.Core.Models.MediaWithCrops", false)]
    [InlineData("false", "System.Collections.Generic.IEnumerable<Umbraco.Cms.Core.Models.MediaWithCrops>", true)]
    [InlineData("true", "Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent", true)]
    [InlineData("true", "System.Collections.Generic.IEnumerable<Umbraco.Cms.Core.Models.MediaWithCrops>", false)]
    [InlineData("true", "System.Collections.Generic.IEnumerable<Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent>", false)]
    public async Task TypedMediaPicker_ValidatesMultiplicityAsync(string multiple, string type, bool diagnostic) {
        // Arrange
        var source = $$"""
            using Shorthand.UmbracoCodeFirst.Attributes;
            namespace Umbraco.Cms.Core.Models {
                public class MediaWithCrops { }
            }
            namespace Umbraco.Cms.Core.Models.PublishedContent {
                public interface IPublishedContent { }
            }
            namespace Shorthand.UmbracoCodeFirst.Attributes {
                public sealed class UCFDataTypeAttribute(string name, System.Type editor, System.Type configuration) : System.Attribute {
                    public string Alias { get; set; }
                }
                public sealed class UCFPropertyAttribute(string name, string dataTypeAlias) : System.Attribute;
            }
            public static class MediaPicker {
                public const string Alias = "Umbraco.MediaPicker3";
            }
            namespace Shorthand.UmbracoCodeFirst.PropertyEditors {
                public static class UmbracoEditors {
                    public static class MediaPicker {
                        public class Configuration { public bool Multiple { get; set; } }
                    }
                }
            }
            public class Configuration : Shorthand.UmbracoCodeFirst.PropertyEditors.UmbracoEditors.MediaPicker.Configuration {
                public Configuration() { Multiple = {{multiple}}; }
            }
            [UCFDataType("Media Picker", typeof(MediaPicker), typeof(Configuration))]
            public class Picker { public const string Alias = "mediaPicker"; }
            public class Page {
                [UCFProperty("Image", Picker.Alias)]
                public {{type}} {{(diagnostic ? "[|Image|]" : "Image")}} { get; } = default!;
            }
            """;

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("string", true)]
    [InlineData("Umbraco.Cms.Core.Models.MediaWithCrops", false)]
    [InlineData("System.Collections.Generic.IEnumerable<Umbraco.Cms.Core.Models.MediaWithCrops>", false)]
    public async Task BuiltInMediaPicker_UnknownMultiplicity_AcceptsEitherMediaShapeAsync(string type, bool diagnostic) {
        // Arrange
        var source = $$"""
            using Shorthand.UmbracoCodeFirst.Attributes;
            namespace Umbraco.Cms.Core.Models {
                public class MediaWithCrops { }
            }
            namespace Shorthand.UmbracoCodeFirst.Attributes {
                public sealed class UCFPropertyAttribute(string name) : System.Attribute {
                    public string DataTypeKey { get; set; }
                }
            }
            public class Page {
                [UCFProperty("Image", DataTypeKey = "AD9F0CF2-BDA2-45D5-9EA1-A63CFC873FD3")]
                public {{type}} {{(diagnostic ? "[|Image|]" : "Image")}} { get; } = default!;
            }
            """;

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, [("umbraco-property-editors.json", _metadata)]);
    }
}
