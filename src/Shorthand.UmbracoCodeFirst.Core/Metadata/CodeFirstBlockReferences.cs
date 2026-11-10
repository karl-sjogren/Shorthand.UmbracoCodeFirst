using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Shorthand.UmbracoCodeFirst.Attributes;

namespace Shorthand.UmbracoCodeFirst.Metadata;

public static class CodeFirstBlockReferences {
    public static string GetAlias(CodeFirstElementReference reference) {
        ArgumentNullException.ThrowIfNull(reference);
        if(reference.ModelType is { } modelType) {
            if(modelType.GetCustomAttribute<UCFElementTypeAttribute>() is null) {
                throw new InvalidOperationException($"Type '{modelType.FullName}' is not a code-first element type.");
            }

            var alias = modelType.GetField(
                "ModelTypeAlias",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)?.GetRawConstantValue() as string;
            return !string.IsNullOrWhiteSpace(alias)
                ? alias
                : throw new InvalidOperationException($"Element type '{modelType.FullName}' has no generated model alias.");
        }

        return !string.IsNullOrWhiteSpace(reference.Alias)
            ? reference.Alias
            : throw new InvalidOperationException("A block element reference must have an alias or model type.");
    }

    public static IEnumerable<CodeFirstElementReference> GetReferences(CodeFirstEditorConfiguration configuration) {
        foreach(var block in configuration.BlockTypes ?? []) {
            yield return block.Content;
            if(block.Settings is not null) {
                yield return block.Settings;
            }

            foreach(var area in block.Areas ?? []) {
                foreach(var permission in area.SpecifiedAllowance ?? []) {
                    if(permission.Element is not null) {
                        yield return permission.Element;
                    }
                }
            }
        }
    }

    public static Guid CreateKey(params string[] parts) {
        var value = string.Join("\0", parts);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        hash[6] = (byte)((hash[6] & 0x0f) | 0x80);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }
}
