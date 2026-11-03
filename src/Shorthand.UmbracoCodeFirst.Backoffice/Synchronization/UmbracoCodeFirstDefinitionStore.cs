using System.Collections.Concurrent;
using System.Text.Json;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.Synchronization;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Strings;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;

public sealed class UmbracoCodeFirstDefinitionStore(
    IDataTypeService dataTypeService,
    IContentTypeService contentTypeService,
    PropertyEditorCollection propertyEditors,
    IConfigurationEditorJsonSerializer configurationSerializer,
    IShortStringHelper shortStringHelper) : ICodeFirstDefinitionStore {
    private const string _ownershipKey = "shorthand.umbraco-codefirst";
    private readonly ConcurrentDictionary<string, IDataType> _ownedDataTypes = new(StringComparer.Ordinal);

    public async Task<SynchronizationStatus> ApplyAsync(
        CodeFirstDefinition definition,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();

        return definition.Kind == CodeFirstDefinitionKind.DataType
            ? await ApplyDataTypeAsync(definition)
            : await ApplyDocumentTypeAsync(definition);
    }

    private async Task<SynchronizationStatus> ApplyDataTypeAsync(CodeFirstDefinition definition) {
        IDataType? existing = await FindOwnedDataTypeAsync(definition.Alias);
        if(existing is not null) {
            if(!IsOwned(existing)) {
                return SynchronizationStatus.Skipped;
            }

            _ownedDataTypes[definition.Alias] = existing;
            bool changed = existing.Name != definition.Name;
            existing.Name = definition.Name;
            if(existing.EditorUiAlias != definition.EditorUiAlias) {
                existing.EditorUiAlias = definition.EditorUiAlias;
                changed = true;
            }

            changed |= ApplyConfiguration(existing, definition.ConfigurationJson);
            return !changed
                ? SynchronizationStatus.Unchanged
                : await UpdateDataTypeAsync(existing, definition);
        }

        if(definition.EditorAlias is null ||
            !propertyEditors.TryGet(definition.EditorAlias, out IDataEditor? editor)) {
            throw new InvalidOperationException(
                $"The data type '{definition.Alias}' references unknown editor '{definition.EditorAlias}'.");
        }

        DataType dataType = new(editor, configurationSerializer, -1) {
            Name = definition.Name,
            EditorUiAlias = definition.EditorUiAlias,
            ConfigurationData = new Dictionary<string, object> {
                [_ownershipKey] = definition.Alias,
            },
        };
        ApplyConfiguration(dataType, definition.ConfigurationJson);
        Attempt<IDataType, DataTypeOperationStatus> result =
            await dataTypeService.CreateAsync(dataType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        if(!result.Success) {
            throw new InvalidOperationException(
                $"Could not create the data type '{definition.Alias}': {result.Status}.");
        }

        _ownedDataTypes[definition.Alias] = result.Result;
        return SynchronizationStatus.Created;
    }

    private async Task<SynchronizationStatus> UpdateDataTypeAsync(IDataType dataType, CodeFirstDefinition definition) {
        Attempt<IDataType, DataTypeOperationStatus> result =
            await dataTypeService.UpdateAsync(dataType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        if(!result.Success) {
            throw new InvalidOperationException(
                $"Could not update the data type '{definition.Alias}': {result.Status}.");
        }

        _ownedDataTypes[definition.Alias] = result.Result;
        return SynchronizationStatus.Updated;
    }

    private async Task<SynchronizationStatus> ApplyDocumentTypeAsync(CodeFirstDefinition definition) {
        IContentType? contentType = contentTypeService.Get(definition.Alias);
        if(contentType is null) {
            ContentType created = new(shortStringHelper, -1) {
                Alias = definition.Alias,
                Name = definition.Name,
                Description = BuildDescription(definition.Settings?.Description, definition.Alias),
            };
            ApplyTypeSettings(created, definition.Settings, definition.Alias);
            await ApplyGroupsAndPropertiesAsync(created, definition);
            return await CreateContentTypeAsync(created);
        }

        if(!IsOwned(contentType, definition.Alias)) {
            return SynchronizationStatus.Skipped;
        }

        bool changed = contentType.Name != definition.Name;
        contentType.Name = definition.Name;
        changed |= ApplyTypeSettings(contentType, definition.Settings, definition.Alias);
        changed |= await ApplyGroupsAndPropertiesAsync(contentType, definition);

        if(!changed) {
            return SynchronizationStatus.Unchanged;
        }

        return await UpdateContentTypeAsync(contentType);
    }

    private async Task<SynchronizationStatus> CreateContentTypeAsync(ContentType contentType) {
        await contentTypeService.CreateAsync(contentType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        return SynchronizationStatus.Created;
    }

    private async Task<SynchronizationStatus> UpdateContentTypeAsync(IContentType contentType) {
        await contentTypeService.UpdateAsync(contentType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        return SynchronizationStatus.Updated;
    }

    private static bool IsOwned(IDataType dataType) =>
        dataType.ConfigurationData.TryGetValue(_ownershipKey, out object? value) &&
        value is string;

    private async Task<IDataType?> FindOwnedDataTypeAsync(string alias) {
        if(_ownedDataTypes.TryGetValue(alias, out IDataType? cached)) {
            return cached;
        }

        IDataType? existing = (await dataTypeService.GetAllAsync())
            .FirstOrDefault(dataType =>
                dataType.ConfigurationData.TryGetValue(_ownershipKey, out object? value) &&
                string.Equals(value as string, alias, StringComparison.Ordinal));
        if(existing is not null) {
            _ownedDataTypes[alias] = existing;
        }

        return existing;
    }

    private static bool IsOwned(IContentType contentType, string alias) =>
        contentType.Description?.Contains(OwnershipMarker(alias), StringComparison.Ordinal) == true;

    private static string OwnershipMarker(string alias) => $"{_ownershipKey}:{alias}";

    private static string BuildDescription(string? description, string alias) =>
        string.IsNullOrWhiteSpace(description)
            ? OwnershipMarker(alias)
            : $"{description}{Environment.NewLine}{OwnershipMarker(alias)}";

    private static bool ApplyConfiguration(IDataType dataType, string? configurationJson) {
        if(string.IsNullOrWhiteSpace(configurationJson)) {
            return false;
        }

        Dictionary<string, JsonElement>? configuration =
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(configurationJson);
        if(configuration is null) {
            throw new InvalidOperationException("The Data Type configuration JSON must be a JSON object.");
        }

        foreach(KeyValuePair<string, JsonElement> item in configuration) {
            dataType.ConfigurationData[item.Key] = ConvertJsonElement(item.Value);
        }

        return true;
    }

    private static object ConvertJsonElement(JsonElement element) =>
        element.ValueKind switch {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(property => property.Name, property => ConvertJsonElement(property.Value)),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(ConvertJsonElement)
                .ToArray(),
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt32(out int intValue) => intValue,
            JsonValueKind.Number when element.TryGetInt64(out long longValue) => longValue,
            JsonValueKind.Number when element.TryGetDecimal(out decimal decimalValue) => decimalValue,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.Null => null!,
            _ => throw new InvalidOperationException(
                $"Unsupported JSON value kind '{element.ValueKind}' in Data Type configuration."),
        };

    private async Task<bool> ApplyGroupsAndPropertiesAsync(
        IContentType contentType,
        CodeFirstDefinition definition) {
        bool changed = false;
        foreach(CodeFirstGroupDefinition tab in definition.EffectiveTabs) {
            changed |= ApplyPropertyGroup(contentType, definition.Alias, tab, PropertyGroupType.Tab);
        }

        foreach(CodeFirstGroupDefinition group in definition.EffectiveGroups) {
            changed |= ApplyPropertyGroup(contentType, definition.Alias, group, PropertyGroupType.Group);
        }

        foreach(CodeFirstPropertyDefinition property in definition.Properties) {
            IDataType? dataType = await FindOwnedDataTypeAsync(property.DataTypeAlias);
            if(dataType is null) {
                throw new InvalidOperationException(
                    $"The document type '{definition.Alias}' references unknown data type '{property.DataTypeAlias}'.");
            }

            IPropertyType? existingProperty = contentType.PropertyTypes
                .FirstOrDefault(candidate => candidate.Alias == property.Alias);
            string? desiredGroupAlias = property.GroupAlias ?? property.TabAlias;
            if(existingProperty is not null) {
                changed |= ApplyPropertySettings(existingProperty, property);
                string? currentGroupAlias = contentType.PropertyGroups
                    .FirstOrDefault(group => group.PropertyTypes?.Any(
                        candidate => candidate.Alias == property.Alias) == true)
                    ?.Alias;
                if(!string.Equals(currentGroupAlias, desiredGroupAlias, StringComparison.Ordinal)) {
                    if(!contentType.MovePropertyType(existingProperty.Alias, desiredGroupAlias)) {
                        throw new InvalidOperationException(
                            $"The document type '{definition.Alias}' could not move property '{property.Alias}' " +
                            $"to group '{desiredGroupAlias ?? "<none>"}'.");
                    }

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

            changed = true;
        }

        return changed;
    }

    private static bool ApplyPropertyGroup(
        IContentType contentType,
        string definitionAlias,
        CodeFirstGroupDefinition definition,
        PropertyGroupType type) {
        bool changed = false;
        PropertyGroup? existingGroup = contentType.PropertyGroups
            .FirstOrDefault(candidate => candidate.Alias == definition.Alias);
        if(existingGroup is null) {
            if(!contentType.AddPropertyGroup(definition.Alias, definition.Name)) {
                throw new InvalidOperationException(
                    $"The document type '{definitionAlias}' could not add property group '{definition.Alias}'.");
            }

            existingGroup = contentType.PropertyGroups
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

    private static bool ApplyTypeSettings(
        IContentType contentType,
        CodeFirstTypeSettings? settings,
        string alias) {
        if(settings is null) {
            return false;
        }

        bool changed = false;
        string description = BuildDescription(settings.Description, alias);
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

        return changed;
    }

    private static bool ApplyPropertySettings(IPropertyType propertyType, CodeFirstPropertyDefinition definition) {
        bool changed = false;
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
