using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Umbraco.BlogEntities.DataTypes;

[UCFDataType(
    "Shell Code Editor",
    typeof(UmbracoEditors.CodeEditor),
    typeof(UmbracoBlogShellCodeEditorConfiguration))]
[UCFFolderPath("CodeFirst")]
public partial class UmbracoBlogShellCodeEditorDataType;

internal sealed class UmbracoBlogShellCodeEditorConfiguration : UmbracoEditors.CodeEditor.Configuration {
    public UmbracoBlogShellCodeEditorConfiguration() {
        Height = 600;
        LineNumbers = true;
        Language = "sh";
    }
}
