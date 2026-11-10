using System.Text.Json;
using System.Text.Json.Nodes;
using Shorthand.UmbracoCodeFirst.Backoffice.Contracts;
using Shorthand.UmbracoCodeFirst.Metadata;
using Umbraco.Cms.Core.Services;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;

internal static class BlockEditorConfigurationResolver {
    public static string Resolve(
        CodeFirstDefinition definition,
        IContentTypeService contentTypes,
        ICodeFirstOwnershipStore ownership) {
        var configuration = definition.EditorConfiguration!;
        var json = JsonNode.Parse(configuration.Json)?.AsObject()
            ?? throw new InvalidOperationException("The block editor configuration must be a JSON object.");
        if(configuration.BlockTypes is not null && json.ContainsKey("blocks")) {
            throw new InvalidOperationException($"Data type '{definition.Alias}' specifies both typed blocks and raw blocks JSON.");
        }

        if(configuration.BlockGroups is not null && json.ContainsKey("blockGroups")) {
            throw new InvalidOperationException($"Data type '{definition.Alias}' specifies both typed groups and raw block groups JSON.");
        }

        foreach(var (name, value) in configuration.TypedSettings ?? new Dictionary<string, JsonElement>()) {
            if(json.ContainsKey(name)) {
                throw new InvalidOperationException($"Data type '{definition.Alias}' specifies both typed and raw '{name}' configuration.");
            }

            json[name] = JsonNode.Parse(value.GetRawText());
        }

        var groupKeys = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var usedGroupKeys = new HashSet<Guid>();
        if(configuration.BlockGroups is not null) {
            var groups = new JsonArray();
            foreach(var group in configuration.BlockGroups) {
                if(string.IsNullOrWhiteSpace(group.Alias) || !groupKeys.TryAdd(
                    group.Alias,
                    group.Key ?? CodeFirstBlockReferences.CreateKey("group", definition.Alias, group.Alias))) {
                    throw new InvalidOperationException($"Data type '{definition.Alias}' has an invalid or duplicate block group alias '{group.Alias}'.");
                }

                if(groupKeys[group.Alias] == Guid.Empty || !usedGroupKeys.Add(groupKeys[group.Alias])) {
                    throw new InvalidOperationException($"Data type '{definition.Alias}' has an invalid or duplicate block group key for '{group.Alias}'.");
                }

                groups.Add(new JsonObject { ["key"] = groupKeys[group.Alias], ["name"] = group.Name });
            }

            json["blockGroups"] = groups;
        }

        if(configuration.BlockTypes is not null) {
            var blocks = new JsonArray();
            foreach(var block in configuration.BlockTypes) {
                var contentAlias = CodeFirstBlockReferences.GetAlias(block.Content);
                var data = new JsonObject {
                    ["contentElementTypeKey"] = ResolveElementKey(block.Content),
                };
                if(block.Settings is not null) {
                    data["settingsElementTypeKey"] = ResolveElementKey(block.Settings);
                }

                Add(data, "label", block.Label);
                Add(data, "thumbnail", block.Thumbnail);
                Add(data, "iconColor", block.IconColor);
                Add(data, "backgroundColor", block.BackgroundColor);
                Add(data, "editorSize", block.EditorSize);
                Add(data, "forceHideContentEditorInOverlay", block.ForceHideContentEditorInOverlay);
                Add(data, "displayInline", block.DisplayInline);
                Add(data, "inlineEditing", block.InlineEditing);
                Add(data, "allowAtRoot", block.AllowAtRoot);
                Add(data, "allowInAreas", block.AllowInAreas);
                Add(data, "areaGridColumns", block.AreaGridColumns);
                Add(data, "rowMinSpan", block.RowMinSpan);
                Add(data, "rowMaxSpan", block.RowMaxSpan);
                if(block.ColumnSpans is not null) {
                    data["columnSpanOptions"] = new JsonArray(
                        block.ColumnSpans.Select(span => (JsonNode?)new JsonObject { ["columnSpan"] = span }).ToArray());
                }

                if(block.GroupAlias is not null) {
                    data["groupKey"] = ResolveGroupKey(block.GroupAlias);
                }

                if(block.Areas is not null) {
                    var areas = new JsonArray();
                    var aliases = new HashSet<string>(StringComparer.Ordinal);
                    var areaKeys = new HashSet<Guid>();
                    foreach(var area in block.Areas) {
                        if(string.IsNullOrWhiteSpace(area.Alias) || !aliases.Add(area.Alias)) {
                            throw new InvalidOperationException($"Block '{contentAlias}' has an invalid or duplicate area alias '{area.Alias}'.");
                        }

                        var areaKey = area.Key ?? CodeFirstBlockReferences.CreateKey("area", definition.Alias, contentAlias, area.Alias);
                        if(areaKey == Guid.Empty || !areaKeys.Add(areaKey)) {
                            throw new InvalidOperationException($"Block '{contentAlias}' has an invalid or duplicate area key for '{area.Alias}'.");
                        }

                        var areaJson = new JsonObject {
                            ["key"] = areaKey,
                            ["alias"] = area.Alias,
                        };
                        Add(areaJson, "columnSpan", area.ColumnSpan);
                        Add(areaJson, "rowSpan", area.RowSpan);
                        Add(areaJson, "minAllowed", area.MinAllowed);
                        Add(areaJson, "maxAllowed", area.MaxAllowed);
                        Add(areaJson, "createLabel", area.CreateLabel);
                        if(area.SpecifiedAllowance is not null) {
                            var permissions = new JsonArray();
                            foreach(var permission in area.SpecifiedAllowance) {
                                var permissionJson = new JsonObject();
                                if(permission.Element is not null) {
                                    permissionJson["elementTypeKey"] = ResolveElementKey(permission.Element);
                                }

                                if(permission.GroupAlias is not null) {
                                    permissionJson["groupKey"] = ResolveGroupKey(permission.GroupAlias);
                                }

                                Add(permissionJson, "minAllowed", permission.MinAllowed);
                                Add(permissionJson, "maxAllowed", permission.MaxAllowed);
                                permissions.Add(permissionJson);
                            }

                            areaJson["specifiedAllowance"] = permissions;
                        }

                        areas.Add(areaJson);
                    }

                    data["areas"] = areas;
                }

                blocks.Add(data);
            }

            json["blocks"] = blocks;
        }

        return json.ToJsonString();

        Guid ResolveElementKey(CodeFirstElementReference reference) {
            var alias = CodeFirstBlockReferences.GetAlias(reference);
            var element = contentTypes.Get(alias);
            if(element is null || !element.IsElement || ownership.Get(alias, element.Key) is null) {
                throw new InvalidOperationException(
                    $"Block reference '{alias}' for data type '{definition.Alias}' does not resolve to an owned element type.");
            }

            return element.Key;
        }

        Guid ResolveGroupKey(string alias) =>
            groupKeys.TryGetValue(alias, out var key)
                ? key
                : throw new InvalidOperationException($"Data type '{definition.Alias}' references unknown block group '{alias}'.");
    }

    private static void Add<T>(JsonObject target, string name, T? value) {
        if(value is not null) {
            target[name] = JsonSerializer.SerializeToNode(value);
        }
    }
}
