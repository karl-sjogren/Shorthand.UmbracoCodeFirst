using Shorthand.UmbracoCodeFirst.Metadata;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Cleanup;

public sealed class CodeFirstCleanupService(
    ICodeFirstDefinitionRegistry registry,
    IDataTypeService dataTypeService,
    IContentTypeService contentTypeService) {
    private const string _ownershipKey = "shorthand.umbraco-codefirst";

    public async Task<CodeFirstCleanupResponse> PreviewAsync() {
        IReadOnlyList<IDataType> dataTypes = (await dataTypeService.GetAllAsync()).ToArray();
        IReadOnlyList<IContentType> contentTypes = contentTypeService.GetAll().ToArray();
        var dataTypeAliases = registry.Definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.DataType)
            .Select(definition => definition.Alias)
            .ToHashSet(StringComparer.Ordinal);
        var contentTypeAliases = registry.Definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.DocumentType)
            .Select(definition => definition.Alias)
            .ToHashSet(StringComparer.Ordinal);
        List<CodeFirstCleanupItem> items = [];

        foreach(var dataType in dataTypes) {
            if(TryGetOwnedAlias(dataType.ConfigurationData, out string? alias)
                && alias is not null
                && !dataTypeAliases.Contains(alias)) {
                items.Add(new("DataType", alias, dataType.Name ?? alias));
            }
        }

        foreach(var contentType in contentTypes) {
            if(!TryGetOwnedAlias(contentType.Description, out string? alias)
                || alias is null) {
                continue;
            }

            if(!contentTypeAliases.Contains(alias)) {
                items.Add(new("ContentType", alias, contentType.Name ?? alias));
                continue;
            }

            var definition = registry.Definitions.Single(
                candidate => candidate.Kind == CodeFirstDefinitionKind.DocumentType
                    && candidate.Alias == alias);
            AddObsoleteMembers(items, contentType, definition);
        }

        return new(items);
    }

    public async Task<CodeFirstCleanupResult> ApplyAsync() {
        var preview = await PreviewAsync();
        List<CodeFirstCleanupItem> removed = [];
        List<CodeFirstCleanupFailure> failed = [];

        foreach(var item in preview.Items
                    .OrderBy(item => item.Kind is "Property" or "Group" or "Tab" ? 0 : 1)
                    .ThenBy(item => item.ParentAlias, StringComparer.Ordinal)
                    .ThenBy(item => item.Alias, StringComparer.Ordinal)) {
            try {
                if(await RemoveAsync(item)) {
                    removed.Add(item);
                }
            } catch(Exception exception) {
                failed.Add(new(item, exception.Message));
            }
        }

        return new(removed.Count, removed, failed);
    }

    private async Task<bool> RemoveAsync(CodeFirstCleanupItem item) {
        if(item.Kind == "DataType") {
            var dataType = (await dataTypeService.GetAllAsync())
                .FirstOrDefault(candidate =>
                    TryGetOwnedAlias(candidate.ConfigurationData, out string? alias)
                    && string.Equals(alias, item.Alias, StringComparison.Ordinal));
            if(dataType is null) {
                return false;
            }

            var result = await dataTypeService.DeleteAsync(
                dataType.Key,
                Umbraco.Cms.Core.Constants.Security.SuperUserKey);
            if(!result.Success) {
                throw new InvalidOperationException(
                    $"Could not delete the data type '{item.Alias}': {result.Status}.");
            }

            return true;
        }

        if(item.Kind == "ContentType") {
            var contentType = contentTypeService.Get(item.Alias);
            if(contentType is null || !IsOwned(contentType, item.Alias)) {
                return false;
            }

            var result = await contentTypeService.DeleteAsync(
                contentType.Key,
                Umbraco.Cms.Core.Constants.Security.SuperUserKey);
            if(result != Umbraco.Cms.Core.Services.OperationStatus.ContentTypeOperationStatus.Success) {
                throw new InvalidOperationException(
                    $"Could not delete the content type '{item.Alias}': {result}.");
            }

            return true;
        }

        if(item.ParentAlias is null) {
            return false;
        }

        var parent = contentTypeService.Get(item.ParentAlias);
        if(parent is null || !IsOwned(parent, item.ParentAlias)) {
            return false;
        }

        if(item.Kind == "Property") {
            if(!parent.PropertyTypes.Any(property => property.Alias == item.Alias)
                || parent.CompositionPropertyTypes.Any(property => property.Alias == item.Alias)) {
                return false;
            }

            parent.RemovePropertyType(item.Alias);
        } else {
            var group = parent.PropertyGroups
                .FirstOrDefault(candidate => candidate.Alias == item.Alias);
            if(group is null
                || parent.CompositionPropertyGroups.Any(candidate => candidate.Alias == item.Alias)
                || group.PropertyTypes?.Any() == true) {
                return false;
            }

            parent.RemovePropertyGroup(item.Alias);
        }

        await contentTypeService.UpdateAsync(
            parent,
            Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        return true;
    }

    private static void AddObsoleteMembers(
        ICollection<CodeFirstCleanupItem> items,
        IContentType contentType,
        CodeFirstDefinition definition) {
        var propertyAliases = definition.Properties
            .Select(property => property.Alias)
            .ToHashSet(StringComparer.Ordinal);
        var compositionPropertyAliases = contentType.CompositionPropertyTypes
            .Select(property => property.Alias)
            .ToHashSet(StringComparer.Ordinal);

        foreach(var property in contentType.PropertyTypes
                    .Where(property => !compositionPropertyAliases.Contains(property.Alias))
                    .Where(property => !propertyAliases.Contains(property.Alias))) {
            items.Add(new("Property", property.Alias, property.Name, definition.Alias));
        }

        var desiredGroupAliases = definition.EffectiveGroups
            .Concat(definition.EffectiveTabs)
            .Select(group => group.Alias)
            .ToHashSet(StringComparer.Ordinal);
        var compositionGroupAliases = contentType.CompositionPropertyGroups
            .Select(group => group.Alias)
            .ToHashSet(StringComparer.Ordinal);
        var obsoletePropertyAliases = contentType.PropertyTypes
            .Where(property => !compositionPropertyAliases.Contains(property.Alias))
            .Where(property => !propertyAliases.Contains(property.Alias))
            .Select(property => property.Alias)
            .ToHashSet(StringComparer.Ordinal);

        foreach(var group in contentType.PropertyGroups
                    .Where(group => !compositionGroupAliases.Contains(group.Alias))
                    .Where(group => !desiredGroupAliases.Contains(group.Alias))
                    .Where(group => group.PropertyTypes is null
                        || group.PropertyTypes.All(property => obsoletePropertyAliases.Contains(property.Alias)))) {
            items.Add(new(
                group.Type == PropertyGroupType.Tab ? "Tab" : "Group",
                group.Alias,
                group.Name ?? group.Alias,
                definition.Alias));
        }
    }

    private static bool IsOwned(IContentType contentType, string alias) =>
        contentType.Description?.Contains(
            $"{_ownershipKey}:{alias}",
            StringComparison.Ordinal) == true;

    private static bool TryGetOwnedAlias(
        IDictionary<string, object> configuration,
        out string? alias) {
        if(configuration.TryGetValue(_ownershipKey, out object? value)
            && value is string stringValue) {
            alias = stringValue;
            return true;
        }

        alias = null;
        return false;
    }

    private static bool TryGetOwnedAlias(string? description, out string? alias) {
        string prefix = $"{_ownershipKey}:";
        string? marker = description?
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(line => line.StartsWith(prefix, StringComparison.Ordinal)
                && !line.StartsWith($"{_ownershipKey}:composition:", StringComparison.Ordinal));
        if(marker is not null) {
            alias = marker[prefix.Length..];
            return true;
        }

        alias = null;
        return false;
    }
}
