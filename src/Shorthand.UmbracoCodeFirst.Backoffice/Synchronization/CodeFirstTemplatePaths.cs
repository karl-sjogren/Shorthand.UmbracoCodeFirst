using System.Collections.Concurrent;
using Shorthand.UmbracoCodeFirst.Metadata;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;

internal interface ICodeFirstTemplatePaths {
    void Register(CodeFirstDefinition definition);
    void Forget(string alias);
    bool ContainsAlias(string alias);
    string? GetOwnedPath(ITemplate template);
    string? GetVerifiedPath(string alias);
    string? GetPathForSave(ITemplate template);
    string? GetOwnedFilePath(string path, Func<string, ITemplate?> getTemplate);
    IFileSystem Views { get; }
}

internal sealed class CodeFirstTemplatePaths(
    ICodeFirstDefinitionRegistry registry,
    IKeyValueService keyValueService,
    FileSystems fileSystems) : ICodeFirstTemplatePaths {
    internal const string OwnershipKeyPrefix = "shorthand.umbraco-codefirst:template:";

    private readonly ConcurrentDictionary<string, string> _paths = new(
        registry.Definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.Template && IsNested(definition.TemplateViewPath))
            .GroupBy(definition => definition.Alias, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .Select(group => new KeyValuePair<string, string>(group.Key, group.Single().TemplateViewPath!)),
        StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, Guid> _verified = new(StringComparer.OrdinalIgnoreCase);

    internal static bool IsNested(string? path) =>
        path is not null && (path.Contains('/', StringComparison.Ordinal) || path.Contains('\\', StringComparison.Ordinal));

    public void Register(CodeFirstDefinition definition) {
        if(IsNested(definition.TemplateViewPath)) {
            _paths[definition.Alias] = definition.TemplateViewPath!;
        }
    }

    public bool ContainsAlias(string alias) => _paths.ContainsKey(alias);

    public void Forget(string alias) => _verified.TryRemove(alias, out _);

    public string? GetOwnedPath(ITemplate template) {
        if(!_paths.TryGetValue(template.Alias, out var path)
            || !string.Equals(keyValueService.GetValue(OwnershipKey(template.Alias)), template.Key.ToString("D"),
                StringComparison.OrdinalIgnoreCase)) {
            Forget(template.Alias);
            return null;
        }

        _verified[template.Alias] = template.Key;
        return path;
    }

    public string? GetVerifiedPath(string alias) =>
        _verified.TryGetValue(alias, out var key)
        && string.Equals(keyValueService.GetValue(OwnershipKey(alias)), key.ToString("D"), StringComparison.OrdinalIgnoreCase)
        && _paths.TryGetValue(alias, out var path)
            ? path
            : null;

    public string? GetPathForSave(ITemplate template) =>
        template is CodeFirstNewTemplate { IsCreating: true } && _paths.TryGetValue(template.Alias, out var path)
            ? path
            : GetOwnedPath(template);

    public string? GetOwnedFilePath(string path, Func<string, ITemplate?> getTemplate) {
        if(!path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase)
            || path.Contains('/', StringComparison.Ordinal)
            || path.Contains('\\', StringComparison.Ordinal)) {
            return null;
        }

        var alias = path[..^7];
        if(!_paths.ContainsKey(alias) || Views.FileExists(path)) {
            return null;
        }

        var template = getTemplate(alias);
        return template is null ? null : GetOwnedPath(template);
    }

    public IFileSystem Views => fileSystems.MvcViewsFileSystem
        ?? throw new InvalidOperationException("The Umbraco views filesystem is unavailable.");

    internal static string OwnershipKey(string alias) => OwnershipKeyPrefix + alias.ToLowerInvariant();
}

internal sealed class CodeFirstNewTemplate(
    Umbraco.Cms.Core.Strings.IShortStringHelper shortStringHelper,
    string name,
    string alias) : Template(shortStringHelper, name, alias) {
    internal bool IsCreating { get; set; } = true;
}
