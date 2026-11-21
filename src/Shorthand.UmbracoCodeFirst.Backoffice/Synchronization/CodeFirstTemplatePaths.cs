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
    bool UsesCustomViews { get; }
}

internal sealed class CodeFirstTemplatePaths(
    ICodeFirstDefinitionRegistry registry,
    IKeyValueService keyValueService,
    FileSystems fileSystems,
    IFileSystem? customViews = null) : ICodeFirstTemplatePaths {
    internal const string OwnershipKeyPrefix = "shorthand.umbraco-codefirst:template:";

    private readonly ConcurrentDictionary<string, string> _paths = new(
        registry.Definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.Template
                && (customViews is not null || IsNested(definition.TemplateViewPath)))
            .GroupBy(definition => definition.Alias, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .Select(group => new KeyValuePair<string, string>(group.Key,
                group.Single().TemplateViewPath ?? group.Single().Alias + ".cshtml")),
        StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, Guid> _verified = new(StringComparer.OrdinalIgnoreCase);

    internal static bool IsNested(string? path) =>
        path is not null && (path.Contains('/', StringComparison.Ordinal) || path.Contains('\\', StringComparison.Ordinal));

    public void Register(CodeFirstDefinition definition) {
        if(UsesCustomViews || IsNested(definition.TemplateViewPath)) {
            _paths[definition.Alias] = definition.TemplateViewPath ?? definition.Alias + ".cshtml";
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
        if(!path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) || DefaultViews.FileExists(path)) {
            return null;
        }

        string alias;
        if(IsNested(path)) {
            var matches = _paths.Where(pair => string.Equals(pair.Value.Replace('\\', '/'), path.Replace('\\', '/'),
                StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
            if(matches.Length != 1) {
                return null;
            }

            alias = matches[0].Key;
        } else {
            alias = path[..^7];
        }

        if(!_paths.ContainsKey(alias)) {
            return null;
        }

        var template = getTemplate(alias);
        return template is null ? null : GetOwnedPath(template);
    }

    private IFileSystem DefaultViews => fileSystems.MvcViewsFileSystem
        ?? throw new InvalidOperationException("The Umbraco views filesystem is unavailable.");

    public IFileSystem Views => customViews ?? DefaultViews;
    public bool UsesCustomViews => customViews is not null;

    internal static string OwnershipKey(string alias) => OwnershipKeyPrefix + alias.ToLowerInvariant();
}

internal sealed class CodeFirstNewTemplate(
    Umbraco.Cms.Core.Strings.IShortStringHelper shortStringHelper,
    string name,
    string alias) : Template(shortStringHelper, name, alias) {
    internal bool IsCreating { get; set; } = true;
}
