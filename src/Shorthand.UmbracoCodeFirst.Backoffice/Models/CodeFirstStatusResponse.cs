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
    public IReadOnlyList<CodeFirstStatusItem> Definitions =>
        DataTypes.Concat(DocumentTypes).Concat(ElementTypes).Concat(MediaTypes).Concat(MemberTypes).ToArray();
}
