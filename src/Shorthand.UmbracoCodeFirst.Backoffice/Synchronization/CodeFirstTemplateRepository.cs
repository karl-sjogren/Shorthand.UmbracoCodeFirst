using System.Text;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Persistence.Querying;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;

internal sealed class CodeFirstTemplateRepository(
    ITemplateRepository inner,
    ICodeFirstTemplatePaths paths,
    ICoreScopeProvider scopeProvider) : ITemplateRepository {
    public ITemplate? Get(int id) => Map(inner.Get(id));
    public ITemplate? Get(Guid id) => Map(inner.Get(id));
    public ITemplate? Get(string? alias) => Map(inner.Get(alias));
    public IEnumerable<ITemplate> GetMany(params int[]? ids) => inner.GetMany(ids).Select(MapRequired).ToArray();

    IEnumerable<ITemplate> Umbraco.Cms.Core.Persistence.IReadRepository<Guid, ITemplate>.GetMany(params Guid[]? ids) =>
        ((Umbraco.Cms.Core.Persistence.IReadRepository<Guid, ITemplate>)inner).GetMany(ids).Select(MapRequired).ToArray();

    public bool Exists(int id) => inner.Exists(id);
    public bool Exists(Guid id) => inner.Exists(id);
    public IEnumerable<ITemplate> GetAll(params string[] aliases) => inner.GetAll(aliases).Select(MapRequired).ToArray();
    public IEnumerable<ITemplate> GetChildren(int layoutTemplateId) => inner.GetChildren(layoutTemplateId).Select(MapRequired).ToArray();
    public IEnumerable<ITemplate> GetDescendants(int layoutTemplateId) => inner.GetDescendants(layoutTemplateId).Select(MapRequired).ToArray();
    public IEnumerable<ITemplate> Get(IQuery<ITemplate> query) => inner.Get(query).Select(MapRequired).ToArray();
    public int Count(IQuery<ITemplate>? query) => inner.Count(query);
    public void Save(ITemplate entity) => inner.Save(entity);

    public void Delete(ITemplate entity) {
        if(paths.GetOwnedPath(entity) is not null && paths.GetOwnedFilePath(entity.Alias + ".cshtml", _ => entity) is null) {
            throw new InvalidOperationException($"A root view conflicts with code-first template '{entity.Alias}'.");
        }

        inner.Delete(entity);
        paths.Forget(entity.Alias);
    }

    public Stream GetFileContentStream(string filepath) =>
        MapPath(filepath) is { } path ? paths.Views.OpenFile(path) : inner.GetFileContentStream(filepath);

    public void SetFileContent(string filepath, Stream content) {
        if(MapPath(filepath) is { } path) {
            paths.Views.AddFile(path, content, true);
        } else {
            inner.SetFileContent(filepath, content);
        }
    }

    public long GetFileSize(string filepath) =>
        MapPath(filepath) is { } path ? paths.Views.GetSize(path) : inner.GetFileSize(filepath);

    private string? MapPath(string filepath) => paths.GetOwnedFilePath(filepath, alias => {
        using var scope = scopeProvider.CreateCoreScope(autoComplete: true);
        return inner.Get(alias);
    });

    private ITemplate MapRequired(ITemplate template) => Map(template)!;

    private ITemplate? Map(ITemplate? template) {
        if(template is not Template concrete || paths.GetOwnedPath(template) is not { } path) {
            return template;
        }

        concrete.GetFileContent = _ => {
            using var reader = new StreamReader(paths.Views.OpenFile(path), Encoding.UTF8, true);
            return reader.ReadToEnd();
        };
        concrete.VirtualPath = paths.Views.GetUrl(path);
        return template;
    }
}
