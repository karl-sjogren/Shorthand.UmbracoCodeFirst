using System.Text;
using System.Security.Cryptography;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;

internal interface ICodeFirstTemplateDefinitionStore {
    Task<SynchronizationStatus> ApplyAsync(CodeFirstDefinition definition, CancellationToken cancellationToken);
}

internal sealed class CodeFirstTemplateDefinitionStore(
    ITemplateService templateService,
    IKeyValueService keyValueService,
    FileSystems fileSystems,
    IShortStringHelper shortStringHelper) : ICodeFirstTemplateDefinitionStore {
    private const string _keyPrefix = "shorthand.umbraco-codefirst:template:";

    public async Task<SynchronizationStatus> ApplyAsync(
        CodeFirstDefinition definition,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        var existing = await templateService.GetAsync(definition.Alias);
        var ownershipKey = _keyPrefix + definition.Alias.ToLowerInvariant();
        if(existing is not null && !string.Equals(
            keyValueService.GetValue(ownershipKey), existing.Key.ToString("D"), StringComparison.OrdinalIgnoreCase)) {
            return SynchronizationStatus.Skipped;
        }

        if(existing is not null && !string.Equals(existing.Alias, definition.Alias, StringComparison.Ordinal)) {
            throw new InvalidOperationException($"Template alias '{existing.Alias}' does not match the view filename '{definition.Alias}'.");
        }

        var candidate = new Template(shortStringHelper, definition.Name, definition.Alias);
        if(!string.Equals(candidate.Alias, definition.Alias, StringComparison.Ordinal)) {
            throw new InvalidOperationException($"Template alias '{definition.Alias}' is not a valid Umbraco template alias.");
        }

        var views = fileSystems.MvcViewsFileSystem
            ?? throw new InvalidOperationException("The Umbraco views filesystem is unavailable.");
        var path = definition.TemplateViewPath
            ?? (definition.Alias + ".cshtml");
        if(!views.FileExists(path)) {
            throw new InvalidOperationException($"The view 'Views/{path}' for template '{definition.Alias}' does not exist.");
        }

        string content;
        using(var stream = views.OpenFile(path))
        using(var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true)) {
            content = await reader.ReadToEndAsync(cancellationToken);
        }

        if(string.IsNullOrWhiteSpace(content)) {
            throw new InvalidOperationException($"The view 'Views/{path}' for template '{definition.Alias}' is empty.");
        }

        if(existing is null) {
            var result = await templateService.CreateAsync(
                definition.Name, definition.Alias, content, UmbracoConstants.Security.SuperUserKey, Guid.NewGuid());
            if(!result.Success) {
                throw new InvalidOperationException($"Could not create template '{definition.Alias}': {result.Status}.");
            }

            if(!string.Equals(result.Result.Alias, definition.Alias, StringComparison.Ordinal)) {
                throw new InvalidOperationException($"Umbraco created template '{result.Result.Alias}' instead of '{definition.Alias}'.");
            }

            keyValueService.SetValue(ownershipKey, result.Result.Key.ToString("D"));
            return SynchronizationStatus.Created;
        }

        using var sha256 = SHA256.Create();
        var contentHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(content)));
        var existingContentHash = existing.Content is null
            ? null
            : Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(existing.Content)));
        if(string.Equals(existing.Name, definition.Name, StringComparison.Ordinal)
            && string.Equals(existingContentHash, contentHash, StringComparison.Ordinal)) {
            return SynchronizationStatus.Unchanged;
        }

        existing.Name = definition.Name;
        existing.Content = content;
        var updated = await templateService.UpdateAsync(existing, UmbracoConstants.Security.SuperUserKey);
        if(!updated.Success) {
            throw new InvalidOperationException($"Could not update template '{definition.Alias}': {updated.Status}.");
        }

        return SynchronizationStatus.Updated;
    }
}
