using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.Cms.Core.Services;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Synchronization;

public sealed class CodeFirstOwnershipStore(IKeyValueService keyValueService) {
    private const string _keyPrefix = "shorthand.umbraco-codefirst:content-type:";

    public CodeFirstDocumentTypeOwnership? Get(string alias, Guid documentTypeKey) {
        var value = keyValueService.GetValue(GetKey(alias));
        if(string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var ownership = JsonSerializer.Deserialize<CodeFirstDocumentTypeOwnership>(value);
        return ownership?.DocumentTypeKey == documentTypeKey
            ? ownership
            : null;
    }

    public void Set(
        string alias,
        Guid documentTypeKey,
        IEnumerable<string> compositionAliases) {
        var ownership = new CodeFirstDocumentTypeOwnership(
            documentTypeKey,
            alias,
            compositionAliases.ToArray());
        keyValueService.SetValue(GetKey(alias), JsonSerializer.Serialize(ownership));
    }

    private static string GetKey(string alias) => $"{_keyPrefix}{alias}";
}

public sealed record CodeFirstDocumentTypeOwnership(
    [property: JsonPropertyName("ContentTypeKey")]
    Guid DocumentTypeKey,
    string Alias,
    IReadOnlyList<string> CompositionAliases);
