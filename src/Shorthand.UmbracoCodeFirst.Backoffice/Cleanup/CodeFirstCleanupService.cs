using Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;
using Shorthand.UmbracoCodeFirst.Metadata;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Cleanup;

public sealed class CodeFirstCleanupService(
    ICodeFirstDefinitionRegistry registry,
    IDataTypeService dataTypeService,
    IContentTypeService contentTypeService,
    IMediaTypeService mediaTypeService,
    IMemberTypeService memberTypeService,
    CodeFirstOwnershipStore ownershipStore) {
    private const string _ownershipKey = "shorthand.umbraco-codefirst";

    public async Task<CodeFirstCleanupResponse> PreviewAsync() {
        IReadOnlyList<IDataType> dataTypes = (await dataTypeService.GetAllAsync()).ToArray();
        IReadOnlyList<IContentType> documentTypes = contentTypeService.GetAll()
            .Concat(registry.Definitions
                .Where(definition => definition.Kind == CodeFirstDefinitionKind.DocumentType)
                .Select(definition => contentTypeService.Get(definition.Alias))
                .OfType<IContentType>())
            .GroupBy(contentType => contentType.Alias, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
        IReadOnlyList<IMediaType> mediaTypes = mediaTypeService.GetAll().ToArray();
        IReadOnlyList<IMemberType> memberTypes = memberTypeService.GetAll().ToArray();
        var dataTypeAliases = registry.Definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.DataType)
            .Select(definition => definition.Alias)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var documentTypeAliases = registry.Definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.DocumentType)
            .Select(definition => definition.Alias)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mediaTypeAliases = registry.Definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.MediaType)
            .Select(definition => definition.Alias)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var memberTypeAliases = registry.Definitions
            .Where(definition => definition.Kind == CodeFirstDefinitionKind.MemberType)
            .Select(definition => definition.Alias)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<CodeFirstCleanupItem> items = [];

        foreach(var dataType in dataTypes) {
            if(TryGetOwnedAlias(dataType.ConfigurationData, out var alias)
                && alias is not null
                && !dataTypeAliases.Contains(alias)) {
                items.Add(new("DataType", alias, dataType.Name ?? alias));
            }
        }

        foreach(var documentType in documentTypes) {
            if(!TryGetOwnedAlias(documentType.Description, out var alias)
                || alias is null) {
                alias = documentType.Alias;
                if(ownershipStore.Get(alias, documentType.Key) is null) {
                    continue;
                }
            }

            if(!documentTypeAliases.Contains(alias)) {
                items.Add(new("DocumentType", alias, documentType.Name ?? alias));
                continue;
            }

            var definition = registry.Definitions.Single(
                candidate => candidate.Kind == CodeFirstDefinitionKind.DocumentType
                    && string.Equals(candidate.Alias, alias, StringComparison.OrdinalIgnoreCase));
            AddObsoleteMembers(items, documentType, definition);
        }

        foreach(var mediaType in mediaTypes) {
            AddTypeCleanupItems(items, mediaType, CodeFirstDefinitionKind.MediaType, mediaTypeAliases);
        }

        foreach(var memberType in memberTypes) {
            AddTypeCleanupItems(items, memberType, CodeFirstDefinitionKind.MemberType, memberTypeAliases);
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
                    TryGetOwnedAlias(candidate.ConfigurationData, out var alias)
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

        if(item.Kind == "DocumentType") {
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

        if(item.Kind == "MediaType") {
            var mediaType = mediaTypeService.Get(item.Alias);
            if(mediaType is null || !IsOwned(mediaType, item.Alias)) {
                return false;
            }

            var result = await mediaTypeService.DeleteAsync(
                mediaType.Key,
                Umbraco.Cms.Core.Constants.Security.SuperUserKey);
            if(result != Umbraco.Cms.Core.Services.OperationStatus.ContentTypeOperationStatus.Success) {
                throw new InvalidOperationException(
                    $"Could not delete the media type '{item.Alias}': {result}.");
            }

            return true;
        }

        if(item.Kind == "MemberType") {
            var memberType = memberTypeService.Get(item.Alias);
            if(memberType is null || !IsOwned(memberType, item.Alias)) {
                return false;
            }

            var result = await memberTypeService.DeleteAsync(
                memberType.Key,
                Umbraco.Cms.Core.Constants.Security.SuperUserKey);
            if(result != Umbraco.Cms.Core.Services.OperationStatus.ContentTypeOperationStatus.Success) {
                throw new InvalidOperationException(
                    $"Could not delete the member type '{item.Alias}': {result}.");
            }

            return true;
        }

        if(item.ParentAlias is null) {
            return false;
        }

        IContentTypeComposition? parent = item.ParentKind switch {
            nameof(CodeFirstDefinitionKind.MediaType) => mediaTypeService.Get(item.ParentAlias),
            nameof(CodeFirstDefinitionKind.MemberType) => memberTypeService.Get(item.ParentAlias),
            _ => contentTypeService.Get(item.ParentAlias),
        };
        var owned = parent is IContentType typedContentType
            ? IsOwned(typedContentType, item.ParentAlias)
            : parent is not null && IsOwned(parent, item.ParentAlias);
        if(parent is null || !owned) {
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

        if(item.ParentKind == nameof(CodeFirstDefinitionKind.MediaType)) {
            await mediaTypeService.UpdateAsync(
                (IMediaType)parent,
                Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        } else if(item.ParentKind == nameof(CodeFirstDefinitionKind.MemberType)) {
            await memberTypeService.UpdateAsync(
                (IMemberType)parent,
                Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        } else {
            await contentTypeService.UpdateAsync(
                (IContentType)parent,
                Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        }

        return true;
    }

    private static void AddObsoleteMembers(
        ICollection<CodeFirstCleanupItem> items,
        IContentTypeComposition contentType,
        CodeFirstDefinition definition) {
        var propertiesByAlias = definition.Properties
            .ToDictionary(property => property.Alias, StringComparer.OrdinalIgnoreCase);
        var compositionPropertyAliases = contentType.CompositionPropertyTypes
            .Select(property => property.Alias)
            .ToHashSet(StringComparer.Ordinal);

        foreach(var property in contentType.PropertyTypes
                    .Where(property => !compositionPropertyAliases.Contains(property.Alias))
                    .Where(property => !propertiesByAlias.TryGetValue(property.Alias, out var desiredProperty)
                        || !string.Equals(
                            property.Alias,
                            desiredProperty.Alias,
                            StringComparison.Ordinal))) {
            items.Add(new(
                "Property",
                property.Alias,
                property.Name,
                definition.Alias,
                definition.Kind == CodeFirstDefinitionKind.DocumentType
                    ? null
                    : definition.Kind.ToString()));
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
            .Where(property => !propertiesByAlias.TryGetValue(property.Alias, out var desiredProperty)
                || !string.Equals(
                    property.Alias,
                    desiredProperty.Alias,
                    StringComparison.Ordinal))
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
                definition.Alias,
                definition.Kind == CodeFirstDefinitionKind.DocumentType
                    ? null
                    : definition.Kind.ToString()));
        }
    }

    private void AddTypeCleanupItems<T>(
        ICollection<CodeFirstCleanupItem> items,
        T type,
        CodeFirstDefinitionKind kind,
        IReadOnlySet<string> aliases)
        where T : class, IContentTypeComposition {
        if(!IsOwned(type, type.Alias)) {
            return;
        }

        if(!aliases.Contains(type.Alias)) {
            items.Add(new(kind.ToString(), type.Alias, type.Name ?? type.Alias));
            return;
        }

        var definition = registry.Definitions.Single(candidate =>
            candidate.Kind == kind
            && string.Equals(candidate.Alias, type.Alias, StringComparison.OrdinalIgnoreCase));
        AddObsoleteMembers(items, type, definition);
    }

    private bool IsOwned(IContentTypeComposition type, string alias) =>
        ownershipStore.Get(alias, type.Key) is not null;

    private bool IsOwned(IContentType contentType, string alias) =>
        ownershipStore.Get(alias, contentType.Key) is not null
        || contentType.Description?.Contains(
            $"{_ownershipKey}:{alias}",
            StringComparison.Ordinal) == true;

    private static bool TryGetOwnedAlias(
        IDictionary<string, object> configuration,
        out string? alias) {
        if(configuration.TryGetValue(_ownershipKey, out var value)
            && value is string stringValue) {
            alias = stringValue;
            return true;
        }

        alias = null;
        return false;
    }

    private static bool TryGetOwnedAlias(string? description, out string? alias) {
        var prefix = $"{_ownershipKey}:";
        var marker = description?
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
