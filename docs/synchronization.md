# Synchronization

`CodeFirstSynchronizer` reads the generated registry and asks `ICodeFirstDefinitionStore` to apply
each definition; `UmbracoCodeFirstDefinitionStore` is the Umbraco implementation. It runs at every
site startup after Umbraco has booted.

## Outcomes

Each definition yields a `SynchronizationItem` with one of:

| Status | Meaning |
| ------ | ------- |
| `Created` | Did not exist; created. |
| `Updated` | Owned by code-first and changed; saved. |
| `Unchanged` | Owned by code-first and already matching. |
| `Skipped` | An item with that alias exists but is **not** owned by code-first; left untouched. |
| `Failed` | An exception occurred; the message is recorded. Other definitions still run. |

The `SynchronizationReport` aggregates the items with a completion timestamp. It is held in memory
only (see [Architecture](architecture.md#runtime-lifecycle)).

## Ownership

Ownership is how code-first knows it may update something.

- **Data Types** – the Data Type's configuration data contains the key `shorthand.umbraco-codefirst`
  whose value is the code-first alias. Lookups find Data Types by that marker, not by name.
- **Document Types and compositions** – ownership is stored in Umbraco's persisted key/value store,
  keyed by the Document Type alias and persisted key. It is not part of the editor-facing
  description.

Because ownership lives in Umbraco's own data, anything created manually in the backoffice, or by
other tools, is never modified. A Document Type that already exists without ownership metadata is
reported as `Skipped`. Sites upgraded from an earlier version are migrated automatically: legacy
description markers are removed during synchronization.

## Data Types

1. Look for an owned Data Type with the alias. If it exists, update name, editor UI alias and merge
   `ConfigurationJson` (only saved if something changed; configuration merging currently always
   counts as a change when configuration JSON is present).
2. Otherwise resolve the editor (`EditorAlias`) from Umbraco's property editors and create the Data
   Type with the ownership marker. An unknown editor fails the definition.

Created or found Data Types are cached for the run so Document Types can reference them
immediately. Property Data Type references resolve only against owned (code-first) Data Types; an
unknown reference fails that Document Type.

## Document Types

For a new Document Type: create with name, description, icon, element/root/library flags, then
compositions, tabs/groups, properties.

For an existing **owned** Document Type, the following are reconciled:

- Name, description, icon, `IsElement`, `AllowAsRoot`, `AllowInLibrary`
- Compositions (added; owned ones removed when dropped from code)
- Tabs and groups: created if missing; name, sort order and type (tab vs group) updated
- Properties: created if missing; existing properties have name, description, sort order,
  mandatory, validation expression/message and label-on-top updated, and are moved to the desired
  tab/group

### Never done automatically

- Deleting Data Types, Document Types, properties, tabs or groups (removing something from code
  leaves it in Umbraco)
- Changing the Data Type of an existing property
- Touching items that are not code-first-owned
- Removing compositions that were not previously added by code-first

### Not yet applied

These are present in the metadata (and visible to the generator) but the Umbraco adapter does not
write them yet: default template, allowed templates, allowed child types, variations and list view.
They are tracked in [Limitations and roadmap](limitations-and-roadmap.md).

## Error handling

Failures are caught per definition, logged with the alias, and stored as `Failed` with the exception
message. Startup continues. The backoffice shows the message so you can diagnose it; typical causes
are an unknown editor alias, an unknown Data Type alias on a property, an unknown composition alias,
or an Umbraco validation failure.
