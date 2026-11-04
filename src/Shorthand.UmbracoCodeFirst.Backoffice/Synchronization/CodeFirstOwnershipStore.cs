using System.Text.Json;
using Umbraco.Cms.Core.Services;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;

public sealed class CodeFirstOwnershipStore(IKeyValueService keyValueService) {
    private const string _keyPrefix = "shorthand.umbraco-codefirst:content-type:";

    public CodeFirstContentTypeOwnership? Get(string alias, Guid contentTypeKey) {
        var value = keyValueService.GetValue(GetKey(alias));
        if(string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var ownership = JsonSerializer.Deserialize<CodeFirstContentTypeOwnership>(value);
        return ownership?.ContentTypeKey == contentTypeKey
            ? ownership
            : null;
    }

    public void Set(
        string alias,
        Guid contentTypeKey,
        IEnumerable<string> compositionAliases) {
        var ownership = new CodeFirstContentTypeOwnership(
            contentTypeKey,
            alias,
            compositionAliases.ToArray());
        keyValueService.SetValue(GetKey(alias), JsonSerializer.Serialize(ownership));
    }

    private static string GetKey(string alias) => $"{_keyPrefix}{alias}";
}

public sealed record CodeFirstContentTypeOwnership(
    Guid ContentTypeKey,
    string Alias,
    IReadOnlyList<string> CompositionAliases);
