# Limitations and roadmap

## Implemented

- `UCFDataType`, `UCFContentType`, `UCFProperty`, `UCFDescription`, `UCFTab`, `UCFGroup`,
  `UCFValidation`, `UCFComposite`, with derivable tab/group/validation attributes
- Registry, published-model and Data Type `Alias` generation
- Data Type creation/update including editor UI alias and raw JSON configuration
- Document Type creation/update: name, description, icon, element/root/library flags, tabs, groups,
  properties (mandatory, validation, label position, sort order), compositions
- Non-destructive, ownership-based reconciliation with per-definition error reporting
- Backoffice page with split tables, links, property details, errors and localization
- Explicit backoffice cleanup preview and confirmation for obsolete owned items
- Analyzers UCF100–UCF107, generator diagnostic UCF001

## Declared but not yet synchronized

`UCFContentType` accepts these, and they reach the metadata, but the Umbraco adapter ignores them:

- `DefaultTemplateAlias`, `AllowedTemplateAliases`
- `AllowedChildAliases`
- `Variations`
- `IsListView`

## Not implemented

- Media Types and Member Types (`MediaTypes/ImageAsset.cs` and `MemberTypes/MemberProfile.cs` are
  empty placeholders)
- Typed, per-editor Data Type settings (raw `ConfigurationJson` only)
- Referencing Umbraco built-in or manually created Data Types from `UCFProperty`
- Changing the Data Type of an existing property
- Any deletion/migration (removed definitions, properties, tabs, groups stay in Umbraco)
- Analyzer code fixes
- Persisting the synchronization report across restarts; a dry-run mode
- Per-property/group ownership markers (cleanup currently treats direct structure on an owned
  Document Type as code-first-managed)
- Multi-version support: the code targets Umbraco 18.3 and .NET 10 only

## Known behaviours worth knowing

- Document Type and composition ownership is stored in Umbraco's persisted key/value store and is
  not shown in the editor-facing description. Legacy description markers are removed during the
  next synchronization.
- A property with both `UCFTab` and `UCFGroup` is placed in the group.
- Generated aliases lower-case letters inside words (`ProductPage` → `productpage`); supply explicit
  aliases for mixed case.
- A Document Type that already exists without the marker is skipped, not adopted.
- Analyzer help links point at the `feature/initial-implementation` branch until merged.

## Possible next stages

1. Apply templates, allowed children, variations and list view settings.
2. Media Type and Member Type support reusing the shared metadata/synchronization abstractions.
3. Typed settings for common editors and validation of `ConfigurationJson`.
4. Opt-in destructive operations and a dry-run mode.
5. Code fixes for the analyzers; switch help links to `main`.
