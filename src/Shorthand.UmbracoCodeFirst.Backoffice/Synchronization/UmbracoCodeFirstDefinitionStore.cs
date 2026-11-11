using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Shorthand.UmbracoCodeFirst.Backoffice.Contracts;
using Shorthand.UmbracoCodeFirst.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;

internal sealed class UmbracoCodeFirstDefinitionStore(
        IDataTypeService dataTypeService,
        IDataTypeContainerService dataTypeContainerService,
        IContentTypeService contentTypeService,
        IMediaTypeService mediaTypeService,
        IMemberTypeService memberTypeService,
        IContentTypeContainerService contentTypeContainerService,
        IMediaTypeContainerService mediaTypeContainerService,
        IMemberTypeContainerService memberTypeContainerService,
        ITemplateService templateService,
        PropertyEditorCollection propertyEditors,
        IConfigurationEditorJsonSerializer configurationSerializer,
        IShortStringHelper shortStringHelper,
        ICodeFirstOwnershipStore ownershipStore,
        ILogger<UmbracoCodeFirstDefinitionStore> logger) : ICodeFirstDefinitionStore, ICodeFirstBlockDefinitionStore {
    private const string _ownershipKey = "shorthand.umbraco-codefirst";
    private readonly ConcurrentDictionary<string, IDataType> _ownedDataTypes = new(StringComparer.Ordinal);

    public async Task<SynchronizationResult> PrepareBlockDataTypeAsync(
        CodeFirstDefinition definition,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if(await FindOwnedDataTypeAsync(definition.Alias) is not null) {
            return new(SynchronizationStatus.Unchanged);
        }

        logger.LogDebug("Creating placeholder for block data type {DefinitionAlias}", definition.Alias);
        return ToResult(await ApplyDataTypeAsync(definition with { EditorConfiguration = null, ConfigurationJson = null }));
    }

    public async Task<SynchronizationResult> ApplyAsync(
            CodeFirstDefinition definition,
            CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogDebug(
            "Applying code-first {DefinitionKind} {DefinitionAlias} ({DefinitionName})",
            definition.Kind,
            definition.Alias,
            definition.Name);

        try {
            var status = definition.Kind switch {
                CodeFirstDefinitionKind.DataType => await ApplyDataTypeAsync(definition),
                CodeFirstDefinitionKind.DocumentType => await ApplyDocumentTypeAsync(definition),
                CodeFirstDefinitionKind.MediaType => await ApplyMediaTypeAsync(definition),
                _ => await ApplyMemberTypeAsync(definition),
            };

            if(status == SynchronizationStatus.Skipped) {
                logger.LogWarning(
                    "Skipped code-first {DefinitionKind} {DefinitionAlias} because the existing definition is not owned by code-first",
                    definition.Kind,
                    definition.Alias);
            } else {
                logger.LogDebug(
                    "Applied code-first {DefinitionKind} {DefinitionAlias}: {Status}",
                    definition.Kind,
                    definition.Alias,
                    status);
            }

            return ToResult(status);
        } catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested) {
            throw;
        } catch(Exception exception) {
            logger.LogError(
                exception,
                "Failed to apply code-first {DefinitionKind} {DefinitionAlias}",
                definition.Kind,
                definition.Alias);
            throw;
        }
    }

    private static SynchronizationResult ToResult(SynchronizationStatus status) =>
        status == SynchronizationStatus.Skipped
            ? new(status, "An existing definition is not owned by code-first and was left unchanged.")
            : new(status);

    private Task<SynchronizationStatus> ApplyMediaTypeAsync(CodeFirstDefinition definition) =>
        ApplyNonDocumentTypeAsync(
            definition,
            mediaTypeService,
            GetMediaTypeParentIdAsync,
            (parentId, alias) => new MediaType(shortStringHelper, parentId) {
                Alias = alias,
            });

    private Task<SynchronizationStatus> ApplyMemberTypeAsync(CodeFirstDefinition definition) =>
        ApplyNonDocumentTypeAsync(
            definition,
            memberTypeService,
            GetMemberTypeParentIdAsync,
            (parentId, alias) => new MemberType(shortStringHelper, parentId) {
                Alias = alias,
            });

    private async Task<SynchronizationStatus> ApplyNonDocumentTypeAsync<T>(
            CodeFirstDefinition definition,
            IContentTypeBaseService<T> service,
            Func<string?, Task<int>> getParentId,
            Func<int, string, T> factory)
            where T : class, IContentTypeComposition {
        var existing = service.Get(definition.Alias);
        if(existing is null) {
            var parentId = await getParentId(definition.FolderPath);

            var created = factory(parentId, definition.Alias);
            created.Name = definition.Name;

            ApplyTypeSettings(created, definition.Settings);

            ApplyAllowedChildren(created, definition.Settings, service.Get);

            await ApplyGroupsAndPropertiesAsync(created, definition);

            var result = await service.CreateAsync(created, UmbracoConstants.Security.SuperUserKey);
            if(!result.Success) {
                throw new InvalidOperationException(
                    $"Could not create the {definition.Kind} '{definition.Alias}': {result.Result}.");
            }

            var createdType = service.Get(definition.Alias)
                ?? throw new InvalidOperationException(
                    $"The created {definition.Kind} '{definition.Alias}' could not be loaded.");

            ownershipStore.Set(definition.Alias, createdType.Key, []);

            return SynchronizationStatus.Created;
        }

        if(ownershipStore.Get(definition.Alias, existing.Key) is null) {
            return SynchronizationStatus.Skipped;
        }

        var changed = existing.Name != definition.Name;
        existing.Name = definition.Name;

        var parentIdForExisting = await getParentId(definition.FolderPath);
        if(existing.ParentId != parentIdForExisting) {
            existing.ParentId = parentIdForExisting;
            changed = true;
        }

        changed |= ApplyTypeSettings(existing, definition.Settings);
        changed |= ApplyAllowedChildren(existing, definition.Settings, service.Get);
        changed |= await ApplyGroupsAndPropertiesAsync(existing, definition);

        if(!changed) {
            return SynchronizationStatus.Unchanged;
        }

        var update = await service.UpdateAsync(existing, UmbracoConstants.Security.SuperUserKey);
        if(!update.Success) {
            throw new InvalidOperationException(
                $"Could not update the {definition.Kind} '{definition.Alias}': {update.Result}.");
        }

        ownershipStore.Set(definition.Alias, existing.Key, []);

        return SynchronizationStatus.Updated;
    }

    private async Task<SynchronizationStatus> ApplyDataTypeAsync(CodeFirstDefinition definition) {
        var configurationJson = definition.EditorConfiguration is { } editorConfiguration
            && (editorConfiguration.BlockTypes is not null || editorConfiguration.BlockGroups is not null
                || editorConfiguration.TypedSettings is not null)
                ? BlockEditorConfigurationResolver.Resolve(definition, contentTypeService, ownershipStore)
                : definition.EffectiveConfigurationJson;
        var existing = await FindOwnedDataTypeAsync(definition.Alias);
        if(existing is not null) {
            if(!IsOwned(existing)) {
                return SynchronizationStatus.Skipped;
            }

            IDataEditor? replacementEditor = null;
            if(existing.EditorAlias != definition.EditorAlias
                && (definition.EditorAlias is null || !propertyEditors.TryGet(definition.EditorAlias, out replacementEditor))) {
                throw new InvalidOperationException(
                    $"The data type '{definition.Alias}' references unknown editor '{definition.EditorAlias}'.");
            }

            _ownedDataTypes[definition.Alias] = existing;
            var typedBlockConfiguration = definition.EditorConfiguration is { } configuration
                && (configuration.BlockTypes is not null || configuration.BlockGroups is not null
                    || configuration.TypedSettings is not null);
            var oldName = existing.Name;
            var oldParentId = existing.ParentId;
            var oldEditorUiAlias = existing.EditorUiAlias;
            var oldEditor = existing.Editor;
            var oldConfiguration = typedBlockConfiguration || replacementEditor is not null
                ? new Dictionary<string, object>(existing.ConfigurationData)
                : null;
            try {
                var changed = existing.Name != definition.Name;
                existing.Name = definition.Name;

                var parentId = await GetDataTypeParentIdAsync(definition.FolderPath);
                if(existing.ParentId != parentId) {
                    existing.ParentId = parentId;
                    changed = true;
                }

                if(existing.EditorUiAlias != definition.EditorUiAlias) {
                    logger.LogDebug(
                        "Changing UI editor alias for data type {DefinitionAlias} from {PreviousEditorUiAlias} to {EditorUiAlias}",
                        definition.Alias,
                        existing.EditorUiAlias,
                        definition.EditorUiAlias);
                    existing.EditorUiAlias = definition.EditorUiAlias;
                    changed = true;
                }

                if(replacementEditor is not null) {
                    logger.LogDebug(
                        "Changing editor alias for data type {DefinitionAlias} from {PreviousEditorAlias} to {EditorAlias}",
                        definition.Alias,
                        existing.EditorAlias,
                        definition.EditorAlias);
                    existing.Editor = replacementEditor;
                    changed = true;
                }

                if(!string.IsNullOrWhiteSpace(configurationJson)) {
                    logger.LogDebug("Applying configuration for data type {DefinitionAlias}", definition.Alias);
                }

                changed |= ApplyConfiguration(existing, configurationJson, typedBlockConfiguration);
                return !changed
                    ? SynchronizationStatus.Unchanged
                    : await UpdateDataTypeAsync(existing, definition);
            } catch {
                if(oldConfiguration is not null) {
                    existing.Name = oldName;
                    existing.ParentId = oldParentId;
                    existing.EditorUiAlias = oldEditorUiAlias;
                    existing.Editor = oldEditor;
                    existing.ConfigurationData = oldConfiguration;
                }

                throw;
            }
        }

        if(definition.EditorAlias is null || !propertyEditors.TryGet(definition.EditorAlias, out var editor)) {
            throw new InvalidOperationException(
                $"The data type '{definition.Alias}' references unknown editor '{definition.EditorAlias}'.");
        }

        var dataTypeParentId = await GetDataTypeParentIdAsync(definition.FolderPath);
        var dataType = new DataType(editor, configurationSerializer, dataTypeParentId) {
            Name = definition.Name,
            EditorUiAlias = definition.EditorUiAlias,
            ConfigurationData = new Dictionary<string, object> {
                [_ownershipKey] = definition.Alias,
            },
        };

        if(!string.IsNullOrWhiteSpace(configurationJson)) {
            logger.LogDebug("Applying configuration for data type {DefinitionAlias}", definition.Alias);
        }

        ApplyConfiguration(dataType, configurationJson);

        var result = await dataTypeService.CreateAsync(dataType, UmbracoConstants.Security.SuperUserKey);
        if(!result.Success) {
            throw new InvalidOperationException(
                $"Could not create the data type '{definition.Alias}': {result.Status}.");
        }

        _ownedDataTypes[definition.Alias] = result.Result;

        return SynchronizationStatus.Created;
    }

    private async Task<SynchronizationStatus> UpdateDataTypeAsync(IDataType dataType, CodeFirstDefinition definition) {
        var result = await dataTypeService.UpdateAsync(dataType, UmbracoConstants.Security.SuperUserKey);
        if(!result.Success) {
            throw new InvalidOperationException(
                $"Could not update the data type '{definition.Alias}': {result.Status}.");
        }

        _ownedDataTypes[definition.Alias] = result.Result;

        return SynchronizationStatus.Updated;
    }

    private async Task<SynchronizationStatus> ApplyDocumentTypeAsync(CodeFirstDefinition definition) {
        var contentType = contentTypeService.Get(definition.Alias);
        if(contentType is null) {
            var contentTypeParentId = await GetContentTypeParentIdAsync(definition.FolderPath);

            ContentType created = new(shortStringHelper, contentTypeParentId) {
                Alias = definition.Alias,
                Name = definition.Name,
                Description = definition.Settings?.Description,
            };

            ApplyTypeSettings(created, definition.Settings);

            ApplyCompositions(created, definition);

            ApplyAllowedChildren(created, definition.Settings, contentTypeService.Get);

            await ApplyTemplatesAsync(created, definition.Settings);

            await ApplyGroupsAndPropertiesAsync(created, definition);

            return await CreateContentTypeAsync(created, definition);
        }

        var ownership = ownershipStore.Get(definition.Alias, contentType.Key);
        var migrated = MigrateLegacyOwnership(contentType, definition.Alias, out var legacyCompositions);
        if(ownership is null && !migrated) {
            return SynchronizationStatus.Skipped;
        }

        var changed = contentType.Name != definition.Name;
        contentType.Name = definition.Name;

        var parentId = await GetContentTypeParentIdAsync(definition.FolderPath);
        if(contentType.ParentId != parentId) {
            contentType.ParentId = parentId;
            changed = true;
        }

        changed |= ApplyTypeSettings(contentType, definition.Settings);
        changed |= ApplyAllowedChildren(contentType, definition.Settings, contentTypeService.Get);
        changed |= ApplyPropertyGroups(contentType, definition);
        changed |= ApplyCompositions(contentType, definition, ownership?.CompositionAliases ?? legacyCompositions);
        changed |= await ApplyTemplatesAsync(contentType, definition.Settings);
        changed |= await ApplyGroupsAndPropertiesAsync(contentType, definition);

        if(!changed) {
            ownershipStore.Set(
                definition.Alias,
                contentType.Key,
                definition.CompositionAliases ?? []);

            return SynchronizationStatus.Unchanged;
        }

        var result = await UpdateContentTypeAsync(contentType);
        ownershipStore.Set(
            definition.Alias,
            contentType.Key,
            definition.CompositionAliases ?? []);

        return result;
    }

    private async Task<SynchronizationStatus> CreateContentTypeAsync(
            ContentType contentType,
            CodeFirstDefinition definition) {
        var result = await contentTypeService.CreateAsync(contentType, UmbracoConstants.Security.SuperUserKey);
        if(!result.Success) {
            throw new InvalidOperationException(
                $"Could not create the document type '{definition.Alias}': {result.Result}.");
        }

        ownershipStore.Set(
            definition.Alias,
            contentType.Key,
            definition.CompositionAliases ?? []);

        return SynchronizationStatus.Created;
    }

    private async Task<SynchronizationStatus> UpdateContentTypeAsync(IContentType contentType) {
        var result = await contentTypeService.UpdateAsync(contentType, UmbracoConstants.Security.SuperUserKey);
        if(!result.Success) {
            throw new InvalidOperationException(
                $"Could not update the document type '{contentType.Alias}': {result.Result}.");
        }

        return SynchronizationStatus.Updated;
    }

    private static bool IsOwned(IDataType dataType) =>
        dataType.ConfigurationData.TryGetValue(_ownershipKey, out var value) && value is string;

    private async Task<IDataType?> FindOwnedDataTypeAsync(string alias) {
        if(_ownedDataTypes.TryGetValue(alias, out var cached)) {
            return cached;
        }

        var existing = (await dataTypeService.GetAllAsync())
            .FirstOrDefault(dataType =>
                dataType.ConfigurationData.TryGetValue(_ownershipKey, out var value) &&
                string.Equals(value as string, alias, StringComparison.Ordinal));

        if(existing is not null) {
            _ownedDataTypes[alias] = existing;
        }

        return existing;
    }

    private async Task<int> GetDataTypeParentIdAsync(string? folderPath) {
        var parentId = -1;
        Guid? parentKey = null;

        foreach(var segment in GetFolderSegments(folderPath)) {
            var container = (await dataTypeContainerService.GetAllAsync())
                .FirstOrDefault(candidate =>
                    candidate.ParentId == parentId
                    && string.Equals(candidate.Name, segment, StringComparison.Ordinal));
            if(container is null) {
                var result = await dataTypeContainerService.CreateAsync(
                    null,
                    segment,
                    parentKey,
                    UmbracoConstants.Security.SuperUserKey);

                if(!result.Success || result.Result is null) {
                    throw new InvalidOperationException(
                        $"Could not create the Data Type folder '{segment}': {result.Status}.");
                }

                container = result.Result;
                logger.LogDebug(
                    "Created data type folder {FolderName} under parent {ParentId}",
                    segment,
                    parentId);
            }

            parentId = container.Id;
            parentKey = container.Key;
        }

        return parentId;
    }

    private async Task<int> GetContentTypeParentIdAsync(string? folderPath) {
        return await GetContentTypeParentIdAsync(folderPath, contentTypeContainerService);
    }

    private async Task<int> GetContentTypeParentIdAsync(
            string? folderPath,
            IContentTypeContainerService containerService) {
        var parentId = -1;
        Guid? parentKey = null;

        foreach(var segment in GetFolderSegments(folderPath)) {
            var container = (await containerService.GetAllAsync())
                .FirstOrDefault(candidate =>
                    candidate.ParentId == parentId
                    && string.Equals(candidate.Name, segment, StringComparison.Ordinal));

            if(container is null) {
                var result = await containerService.CreateAsync(
                    null,
                    segment,
                    parentKey,
                    UmbracoConstants.Security.SuperUserKey);

                if(!result.Success || result.Result is null) {
                    throw new InvalidOperationException(
                        $"Could not create the Document Type folder '{segment}': {result.Status}.");
                }

                container = result.Result;
                logger.LogDebug(
                    "Created document type folder {FolderName} under parent {ParentId}",
                    segment,
                    parentId);
            }

            parentId = container.Id;
            parentKey = container.Key;
        }

        return parentId;
    }

    private async Task<int> GetMediaTypeParentIdAsync(string? folderPath) {
        var parentId = -1;
        Guid? parentKey = null;

        foreach(var segment in GetFolderSegments(folderPath)) {
            var container = (await mediaTypeContainerService.GetAllAsync())
                .FirstOrDefault(candidate =>
                    candidate.ParentId == parentId
                    && string.Equals(candidate.Name, segment, StringComparison.Ordinal));

            if(container is null) {
                var result = await mediaTypeContainerService.CreateAsync(
                    null,
                    segment,
                    parentKey,
                    UmbracoConstants.Security.SuperUserKey);

                if(!result.Success || result.Result is null) {
                    throw new InvalidOperationException(
                        $"Could not create the Media Type folder '{segment}': {result.Result}.");
                }

                container = result.Result;
                logger.LogDebug(
                    "Created media type folder {FolderName} under parent {ParentId}",
                    segment,
                    parentId);
            }

            parentId = container.Id;
            parentKey = container.Key;
        }

        return parentId;
    }

    private async Task<int> GetMemberTypeParentIdAsync(string? folderPath) {
        var parentId = -1;
        Guid? parentKey = null;

        foreach(var segment in GetFolderSegments(folderPath)) {
            var container = (await memberTypeContainerService.GetAllAsync())
                .FirstOrDefault(candidate =>
                    candidate.ParentId == parentId
                    && string.Equals(candidate.Name, segment, StringComparison.Ordinal));

            if(container is null) {
                var result = await memberTypeContainerService.CreateAsync(
                    null,
                    segment,
                    parentKey,
                    UmbracoConstants.Security.SuperUserKey);

                if(!result.Success || result.Result is null) {
                    throw new InvalidOperationException(
                        $"Could not create the Member Type folder '{segment}': {result.Result}.");
                }

                container = result.Result;
                logger.LogDebug(
                    "Created member type folder {FolderName} under parent {ParentId}",
                    segment,
                    parentId);
            }

            parentId = container.Id;
            parentKey = container.Key;
        }

        return parentId;
    }

    private static IEnumerable<string> GetFolderSegments(string? folderPath) {
        folderPath ??= string.Empty;

        return folderPath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(segment => segment is not "." and not "..");
    }

    private bool ApplyCompositions(
            IContentType contentType,
            CodeFirstDefinition definition,
            IReadOnlyList<string>? ownedCompositionAliases = null) {
        var changed = false;
        var desiredAliases = definition.CompositionAliases ?? [];
        var desired = desiredAliases.ToHashSet(StringComparer.Ordinal);
        var owned = (ownedCompositionAliases ?? []).ToHashSet(StringComparer.Ordinal);

        foreach(var existingAlias in contentType.CompositionAliases().ToArray()) {
            if(desired.Contains(existingAlias)
                || !owned.Contains(existingAlias)) {
                continue;
            }

            contentType.RemoveContentType(existingAlias);
            logger.LogDebug(
                "Removing owned composition {CompositionAlias} from document type {DefinitionAlias}",
                existingAlias,
                definition.Alias);
            changed = true;
        }

        foreach(var compositionAlias in desiredAliases) {
            var composition = contentTypeService.Get(compositionAlias);
            if(composition is null) {
                throw new InvalidOperationException(
                    $"The document type '{definition.Alias}' references unknown composition '{compositionAlias}'.");
            }

            if(!contentType.ContentTypeCompositionExists(compositionAlias)) {
                contentType.AddContentType(composition);
                logger.LogDebug(
                    "Adding composition {CompositionAlias} to document type {DefinitionAlias}",
                    compositionAlias,
                    definition.Alias);
                changed = true;
            }
        }

        return changed;
    }

    private bool MigrateLegacyOwnership(
            IContentType contentType,
            string alias,
            out IReadOnlyList<string> compositionAliases) {
        var lines = (contentType.Description ?? string.Empty)
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        var ownershipMarker = $"{_ownershipKey}:{alias}";
        if(!lines.Contains(ownershipMarker, StringComparer.Ordinal)) {
            compositionAliases = [];
            return false;
        }

        compositionAliases = lines
            .Where(line => line.StartsWith(
                $"{_ownershipKey}:composition:",
                StringComparison.Ordinal))
            .Select(line => line[$"{_ownershipKey}:composition:".Length..])
            .ToArray();

        var descriptionLines = lines
            .Where(line => !string.Equals(line, ownershipMarker, StringComparison.Ordinal)
                && !line.StartsWith($"{_ownershipKey}:composition:", StringComparison.Ordinal));

        contentType.Description = string.Join(Environment.NewLine, descriptionLines);
        ownershipStore.Set(alias, contentType.Key, compositionAliases);
        logger.LogDebug("Migrated legacy ownership for document type {DefinitionAlias}", alias);
        return true;
    }

    private static bool ApplyConfiguration(
        IDataType dataType,
        string? configurationJson,
        bool onlyIfChanged = false) {
        if(string.IsNullOrWhiteSpace(configurationJson)) {
            return false;
        }

        var configuration = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(configurationJson);
        if(configuration is null) {
            throw new InvalidOperationException("The Data Type configuration JSON must be a JSON object.");
        }

        var changed = false;
        foreach(var item in configuration) {
            if(onlyIfChanged
                && dataType.ConfigurationData.TryGetValue(item.Key, out var existing)
                && (JsonNode.DeepEquals(
                    JsonSerializer.SerializeToNode(existing),
                    JsonNode.Parse(item.Value.GetRawText()))
                    || (existing is null && item.Key is "blocks" or "blockGroups"
                        && item.Value.ValueKind == JsonValueKind.Array && item.Value.GetArrayLength() == 0))) {
                continue;
            }

            dataType.ConfigurationData[item.Key] = ConvertJsonElement(item.Value);
            changed = true;
        }

        return onlyIfChanged ? changed : true;
    }

    private static object ConvertJsonElement(JsonElement element) =>
        element.ValueKind switch {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(property => property.Name, property => ConvertJsonElement(property.Value)),
            JsonValueKind.Array when element.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String)
                => element.EnumerateArray().Select(item => item.GetString()!).ToArray(),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(item => ConvertJsonElement(item))
                .ToArray(),
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.Null => null!,
            _ => throw new InvalidOperationException($"Unsupported JSON value kind '{element.ValueKind}' in Data Type configuration."),
        };

    private async Task<bool> ApplyGroupsAndPropertiesAsync(
            IContentTypeComposition contentType,
            CodeFirstDefinition definition) {
        var changed = ApplyPropertyGroups(contentType, definition);

        foreach(var property in definition.Properties) {
            var dataType = await FindOwnedDataTypeAsync(property.DataTypeAlias);
            if(dataType is null) {
                throw new InvalidOperationException(
                    $"The document type '{definition.Alias}' references unknown data type '{property.DataTypeAlias}'.");
            }

            var existingProperty = contentType.PropertyTypes.FirstOrDefault(candidate => candidate.Alias == property.Alias);

            var desiredGroupAlias = property.GroupAlias ?? property.TabAlias;
            if(existingProperty is not null) {
                if(existingProperty.DataTypeKey != dataType.Key) {
                    if(existingProperty.ValueStorageType != dataType.DatabaseType) {
                        throw new InvalidOperationException(
                            $"The {definition.Kind} '{definition.Alias}' property '{property.Alias}' cannot change " +
                            $"to data type '{property.DataTypeAlias}' because its storage type would change from " +
                            $"'{existingProperty.ValueStorageType}' to '{dataType.DatabaseType}'. Migrate existing values and change the data type manually.");
                    }

                    existingProperty.DataTypeId = dataType.Id;
                    existingProperty.DataTypeKey = dataType.Key;
                    existingProperty.PropertyEditorAlias = dataType.EditorAlias;
                    existingProperty.ValueStorageType = dataType.DatabaseType;
                    logger.LogDebug(
                        "Changing data type of property {PropertyAlias} on {DefinitionKind} {DefinitionAlias} to {DataTypeAlias}",
                        property.Alias,
                        definition.Kind,
                        definition.Alias,
                        property.DataTypeAlias);
                    changed = true;
                }

                if(ApplyPropertySettings(existingProperty, property)) {
                    logger.LogDebug(
                        "Updating property {PropertyAlias} on {DefinitionKind} {DefinitionAlias}",
                        property.Alias,
                        definition.Kind,
                        definition.Alias);
                    changed = true;
                }

                var currentGroupAlias = contentType
                    .PropertyGroups
                    .FirstOrDefault(group => group.PropertyTypes?.Any(candidate => candidate.Alias == property.Alias) == true)
                    ?.Alias;

                if(!string.Equals(currentGroupAlias, desiredGroupAlias, StringComparison.Ordinal)) {
                    if(!contentType.MovePropertyType(existingProperty.Alias, desiredGroupAlias)) {
                        throw new InvalidOperationException(
                            $"The document type '{definition.Alias}' could not move property '{property.Alias}' " +
                            $"to group '{desiredGroupAlias ?? "<none>"}'.");
                    }

                    logger.LogDebug(
                        "Moving property {PropertyAlias} on {DefinitionKind} {DefinitionAlias} from group {PreviousGroupAlias} to {GroupAlias}",
                        property.Alias,
                        definition.Kind,
                        definition.Alias,
                        currentGroupAlias,
                        desiredGroupAlias);
                    changed = true;
                }

                continue;
            }

            PropertyType propertyType = new(shortStringHelper, dataType) {
                Alias = property.Alias,
                Name = property.Name,
            };

            ApplyPropertySettings(propertyType, property);

            if(string.IsNullOrWhiteSpace(desiredGroupAlias)) {
                contentType.AddPropertyType(propertyType);
            } else {
                contentType.AddPropertyType(
                    propertyType,
                    desiredGroupAlias,
                    property.GroupAlias is not null
                        ? property.GroupName ?? property.GroupAlias
                        : property.TabName ?? desiredGroupAlias);
            }

            var addedProperty = contentType.PropertyTypes.FirstOrDefault(candidate => candidate.Alias == property.Alias);
            if(addedProperty is not null) {
                ApplyPropertySettings(addedProperty, property);
            }

            logger.LogDebug(
                "Adding property {PropertyAlias} using data type {DataTypeAlias} to {DefinitionKind} {DefinitionAlias} in group {GroupAlias}",
                property.Alias,
                property.DataTypeAlias,
                definition.Kind,
                definition.Alias,
                desiredGroupAlias);
            changed = true;
        }

        return changed;
    }

    private static bool ApplyPropertyGroups(
            IContentTypeComposition contentType,
            CodeFirstDefinition definition) {
        var changed = false;

        var desiredAliases = definition.EffectiveTabs
            .Concat(definition.EffectiveGroups)
            .Select(group => group.Alias)
            .ToHashSet(StringComparer.Ordinal);

        foreach(var existingGroup in contentType.PropertyGroups.ToArray()) {
            if(desiredAliases.Contains(existingGroup.Alias)
                || existingGroup.PropertyTypes?.Any() == true
                || string.IsNullOrWhiteSpace(existingGroup.Name)
                || !string.Equals(
                    existingGroup.Alias,
                    ToLegacyAlias(existingGroup.Name),
                    StringComparison.Ordinal)) {
                continue;
            }

            contentType.RemovePropertyGroup(existingGroup.Alias);
            changed = true;
        }

        foreach(var tab in definition.EffectiveTabs) {
            changed |= ApplyPropertyGroup(contentType, definition.Alias, tab, PropertyGroupType.Tab);
        }

        foreach(var group in definition.EffectiveGroups) {
            changed |= ApplyPropertyGroup(contentType, definition.Alias, group, PropertyGroupType.Group);
        }

        return changed;
    }

    private static bool ApplyPropertyGroup(
            IContentTypeComposition contentType,
            string definitionAlias,
            CodeFirstGroupDefinition definition,
            PropertyGroupType type) {
        var changed = false;

        var existingGroup = contentType.PropertyGroups.FirstOrDefault(candidate => candidate.Alias == definition.Alias);
        if(existingGroup is null) {
            var legacyAlias = ToLegacyAlias(definition.Name);
            existingGroup = contentType.PropertyGroups
                .FirstOrDefault(candidate =>
                    candidate.Type == type
                    && (string.Equals(candidate.Alias, legacyAlias, StringComparison.Ordinal)
                        || (string.Equals(candidate.Name, definition.Name, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(
                                candidate.Alias,
                                definition.Name,
                                StringComparison.OrdinalIgnoreCase))));

            if(existingGroup is not null && existingGroup.Alias != definition.Alias) {
                existingGroup.Alias = definition.Alias;
                changed = true;
            }
        }

        if(existingGroup is null) {
            if(!contentType.AddPropertyGroup(definition.Alias, definition.Name)) {
                throw new InvalidOperationException(
                    $"The document type '{definitionAlias}' could not add property group '{definition.Alias}'.");
            }

            existingGroup = contentType
                .PropertyGroups
                .First(candidate => candidate.Alias == definition.Alias);

            changed = true;
        }

        if(existingGroup.Name != definition.Name) {
            existingGroup.Name = definition.Name;
            changed = true;
        }

        if(existingGroup.SortOrder != definition.SortOrder) {
            existingGroup.SortOrder = definition.SortOrder;
            changed = true;
        }

        if(existingGroup.Type != type) {
            existingGroup.Type = type;
            changed = true;
        }

        return changed;
    }

    private static string ToLegacyAlias(string value) {
        if(value.Length == 0) {
            return value;
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    private static bool ApplyTypeSettings(
            IContentTypeComposition contentType,
            CodeFirstTypeSettings? settings) {
        if(settings is null) {
            return false;
        }

        var changed = false;

        var description = settings.Description;
        if(contentType.Description != description) {
            contentType.Description = description;
            changed = true;
        }

        if(contentType.Icon != settings.Icon) {
            contentType.Icon = settings.Icon;
            changed = true;
        }

        if(contentType.IsElement != settings.IsElement) {
            contentType.IsElement = settings.IsElement;
            changed = true;
        }

        if(contentType.AllowedAsRoot != settings.AllowAsRoot) {
            contentType.AllowedAsRoot = settings.AllowAsRoot;
            changed = true;
        }

        if(contentType.AllowedInLibrary != settings.AllowInLibrary) {
            contentType.AllowedInLibrary = settings.AllowInLibrary;
            changed = true;
        }

        if(settings.Variations is not null) {
            var variationValue = string.Equals(settings.Variations, "Invariant", StringComparison.OrdinalIgnoreCase)
                ? nameof(ContentVariation.Nothing) : settings.Variations;

            if(!Enum.TryParse<ContentVariation>(variationValue, true, out var variations)) {
                throw new InvalidOperationException(
                    $"The content type variations value '{settings.Variations}' is invalid.");
            }

            if(contentType.Variations != variations) {
                contentType.Variations = variations;
                changed = true;
            }
        }

        return changed;
    }

    private static bool ApplyAllowedChildren<T>(
            IContentTypeComposition contentType,
            CodeFirstTypeSettings? settings,
            Func<string, T?> resolve) where T : class, IContentTypeComposition {
        var aliases = settings?.AllowedChildAliases ?? [];

        var desired = aliases
            .Select((alias, index) => {
                var child = resolve(alias) ?? throw new InvalidOperationException($"The content type '{contentType.Alias}' references unknown allowed child '{alias}'.");
                return new ContentTypeSort(child.Key, index, child.Alias);
            })
            .ToArray();

        var existing = contentType.AllowedContentTypes ?? [];
        if(existing.Select(item => item.Alias).SequenceEqual(desired.Select(item => item.Alias))) {
            return false;
        }

        contentType.AllowedContentTypes = desired;

        return true;
    }

    private async Task<bool> ApplyTemplatesAsync(
            IContentType contentType,
            CodeFirstTypeSettings? settings) {
        if(settings is null) {
            return false;
        }

        var allowedAliases = settings.AllowedTemplateAliases ?? [];
        var templates = new List<ITemplate>(allowedAliases.Count);
        foreach(var alias in allowedAliases) {
            var template = await templateService.GetAsync(alias)
                ?? throw new InvalidOperationException($"The document type '{contentType.Alias}' references unknown template '{alias}'.");
            templates.Add(template);
        }

        var changed = !(contentType.AllowedTemplates ?? [])
            .Select(template => template.Alias)
            .SequenceEqual(templates.Select(template => template.Alias));

        if(changed) {
            contentType.AllowedTemplates = templates;
        }

        if(settings.DefaultTemplateAlias is null) {
            return changed;
        }

        var defaultTemplate = await templateService.GetAsync(settings.DefaultTemplateAlias)
            ?? throw new InvalidOperationException($"The document type '{contentType.Alias}' references unknown default template '{settings.DefaultTemplateAlias}'.");

        if(!templates.Any(template => string.Equals(template.Alias, defaultTemplate.Alias, StringComparison.Ordinal))) {
            throw new InvalidOperationException(
                $"The default template '{defaultTemplate.Alias}' must be included in the allowed templates for document type '{contentType.Alias}'.");
        }

        if(!string.Equals(
                contentType.DefaultTemplate?.Alias,
                defaultTemplate.Alias,
                StringComparison.Ordinal)) {
            contentType.SetDefaultTemplate(defaultTemplate);
            changed = true;
        }

        return changed;
    }

    private static bool ApplyPropertySettings(IPropertyType propertyType, CodeFirstPropertyDefinition definition) {
        var changed = false;

        if(propertyType.Name != definition.Name) {
            propertyType.Name = definition.Name;
            changed = true;
        }

        if(propertyType.Description != definition.Description) {
            propertyType.Description = definition.Description;
            changed = true;
        }

        if(propertyType.SortOrder != definition.SortOrder) {
            propertyType.SortOrder = definition.SortOrder;
            changed = true;
        }

        if(propertyType.Mandatory != definition.Mandatory) {
            propertyType.Mandatory = definition.Mandatory;
            changed = true;
        }

        if(propertyType.ValidationRegExp != definition.ValidationExpression) {
            propertyType.ValidationRegExp = definition.ValidationExpression;
            changed = true;
        }

        if(propertyType.ValidationRegExpMessage != definition.ValidationMessage) {
            propertyType.ValidationRegExpMessage = definition.ValidationMessage;
            changed = true;
        }

        if(propertyType.LabelOnTop != definition.LabelOnTop) {
            propertyType.LabelOnTop = definition.LabelOnTop;
            changed = true;
        }

        return changed;
    }
}
