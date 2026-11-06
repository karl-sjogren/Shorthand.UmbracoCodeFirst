namespace Shorthand.UmbracoCodeFirst.Backoffice.Models;

public sealed record CodeFirstStatusProperty(
    string Alias,
    string Name,
    string DataTypeAlias,
    string? Description,
    string? GroupAlias,
    string? GroupName,
    string? TabAlias,
    string? TabName,
    int SortOrder,
    bool Mandatory,
    string? ValidationExpression,
    string? ValidationMessage,
    bool LabelOnTop);
