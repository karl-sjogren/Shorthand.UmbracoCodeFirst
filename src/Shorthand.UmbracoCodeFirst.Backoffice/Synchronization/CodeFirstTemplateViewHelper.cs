using System.Text;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;

internal sealed class CodeFirstTemplateViewHelper(
    IViewHelper inner,
    ICodeFirstTemplatePaths paths) : IViewHelper {
    public bool ViewExists(ITemplate template) =>
        paths.GetPathForSave(template) is { } path ? paths.Views.FileExists(path) : inner.ViewExists(template);

    public string GetFileContents(ITemplate template) =>
        paths.GetPathForSave(template) is { } path ? Read(path) : inner.GetFileContents(template);

    public string CreateView(ITemplate template, bool overWrite = false) =>
        paths.GetPathForSave(template) is { } path ? Read(path) : inner.CreateView(template, overWrite);

    public string? UpdateViewFile(ITemplate template, string? currentAlias = null) {
        if(currentAlias is not null && !string.Equals(currentAlias, template.Alias, StringComparison.Ordinal)
            && paths.GetVerifiedPath(currentAlias) is not null) {
            throw new InvalidOperationException($"The code-first template '{currentAlias}' cannot be renamed in Umbraco.");
        }

        if(paths.GetPathForSave(template) is not { } path) {
            return inner.UpdateViewFile(template, currentAlias);
        }

        var content = template.Content ?? string.Empty;
        if(!string.Equals(Read(path), content, StringComparison.Ordinal)) {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            paths.Views.AddFile(path, stream, true);
        }

        return content;
    }

    public string ViewPath(string alias) => paths.GetVerifiedPath(alias) ?? inner.ViewPath(alias);

    private string Read(string path) {
        using var reader = new StreamReader(paths.Views.OpenFile(path), Encoding.UTF8, true);
        return reader.ReadToEnd();
    }
}
