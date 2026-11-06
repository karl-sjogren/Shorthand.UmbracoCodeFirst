using Shorthand.UmbracoCodeFirst.Metadata;

namespace Shorthand.UmbracoCodeFirst.Synchronization;

public sealed record SynchronizationItem(
    CodeFirstDefinitionKind Kind,
    string Alias,
    string Name,
    SynchronizationStatus Status,
    string? Message = null);
