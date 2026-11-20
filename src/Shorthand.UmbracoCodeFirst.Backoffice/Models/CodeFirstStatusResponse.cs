using System.Reflection;

namespace Shorthand.UmbracoCodeFirst.Backoffice.Models;

public sealed record CodeFirstStatusResponse(
        int DefinitionCount,
        int DataTypeCount,
        int DocumentTypeCount,
        int ElementTypeCount,
        int MediaTypeCount,
        int MemberTypeCount,
        int CreatedCount,
        int UpdatedCount,
        int UnchangedCount,
        int SkippedCount,
        int FailedCount,
        DateTimeOffset CompletedAt,
        IReadOnlyList<CodeFirstStatusItem> DataTypes,
        IReadOnlyList<CodeFirstStatusItem> DocumentTypes,
        IReadOnlyList<CodeFirstStatusItem> ElementTypes,
        IReadOnlyList<CodeFirstStatusItem> MediaTypes,
        IReadOnlyList<CodeFirstStatusItem> MemberTypes) {
    public bool HasReport => CompletedAt != DateTimeOffset.MinValue;

    public string? Version => typeof(CodeFirstStatusResponse).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0];

    public string? SynchronizationError { get; init; }

    public int TemplateCount { get; init; }

    public IReadOnlyList<CodeFirstStatusItem> Templates { get; init; } = [];

    public IReadOnlyList<CodeFirstStatusItem> Definitions =>
        DataTypes.Concat(DocumentTypes).Concat(ElementTypes).Concat(MediaTypes).Concat(MemberTypes).Concat(Templates).ToArray();
}
