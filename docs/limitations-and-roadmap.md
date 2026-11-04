# Limitations and roadmap

## Implemented

- `UCFDataType`, `UCFDocumentType`, `UCFProperty`, `UCFDescription`, `UCFTab`, `UCFGroup`,
  `UCFValidation`, `UCFComposite`, with derivable tab/group/validation attributes
- Registry, published-model and Data Type `Alias` generation
- Data Type creation/update including editor UI alias and raw JSON configuration
- Document Type creation/update: name, description, icon, element/root/library flags, tabs, groups,
  properties (mandatory, validation, label position, sort order), compositions
- Non-destructive, ownership-based reconciliation with per-definition error reporting
- Document, Media and Member Type synchronization with folders, tabs/groups and properties
- Backoffice page with split tables, links, property details, settings, errors and localization
- Explicit backoffice cleanup preview and confirmation for obsolete owned items
- Analyzers UCF100–UCF107, generator diagnostic UCF001

## Declared and synchronized

The Umbraco adapter applies these settings where the Umbraco 18.3 API exposes them directly:

- `DefaultTemplateAlias` and `AllowedTemplateAliases` for Document Types
- `AllowedChildAliases` for Document, Media and Member Types
- `Variations` for supported content-type categories

`IsListView` is exposed as metadata and status information, but a concrete Umbraco list-view
definition key is required to persist the setting. The current boolean is therefore not used to
invent or select a list-view entity.

## Remaining limitations

- Typed, per-editor Data Type settings (raw `ConfigurationJson` only)
- Referencing Umbraco built-in or manually created Data Types from `UCFProperty`
- Changing the Data Type of an existing property
- Automatic deletion/migration (removed definitions, properties, tabs, groups remain until the
  explicit cleanup action is applied)
- Analyzer code fixes
- Persisting the synchronization report across restarts
- Per-property/group ownership markers (cleanup currently treats direct structure on an owned
  type as code-first-managed)
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

1. Add concrete list-view entity aliases/keys.
2. Typed settings for common editors and validation of `ConfigurationJson`.
3. Opt-in destructive migrations beyond the explicit cleanup workflow.
4. Per-member ownership markers and finer-grained manual-structure preservation.
5. Code fixes for the analyzers; switch help links to `main`.
