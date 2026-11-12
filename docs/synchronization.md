# Synchronization

`CodeFirstSynchronizer` reads the generated registry and asks `ICodeFirstDefinitionStore` to apply
each definition; `UmbracoCodeFirstDefinitionStore` is the Umbraco implementation. The package runs
it automatically during Umbraco startup when the runtime reaches `Run`, including after install or
upgrade. Set `Shorthand:UmbracoCodeFirst:DisableStartupSynchronization` to `true` to opt out.
Remove existing explicit startup calls to avoid running the same synchronization twice. An explicit
call through the registered `ICodeFirstSynchronizer` remains available. Call it outside any existing
Umbraco scope; otherwise it fails without writing.

By default, synchronization is serialized only within the current process. If multiple instances
share a database, enable cluster coordination on every instance:

```json
{
  "Shorthand": {
    "UmbracoCodeFirst": {
      "EnableClusterCoordination": true
    }
  }
}
```

Without cluster coordination, startup and explicit synchronization bypass the shared claim.
**Do not run multiple instances or other writers against the same database without coordination**:
they can synchronize concurrently or apply different definitions. Disabling coordination does not
clear a pre-existing claim; if it is enabled later, stale claims still require the recovery
procedure below. Restart the application after changing this setting.

## Outcomes

Each definition yields a `SynchronizationItem` with one of:

| Status | Meaning |
| ------ | ------- |
| `Created` | Did not exist; created. |
| `Updated` | Owned by code-first and changed; saved. |
| `Unchanged` | Owned by code-first and already matching. |
| `Skipped` | The store did not apply the definition. For example, an existing type is not owned by code-first and is left untouched. The reason is reported when available. |
| `Failed` | An exception occurred; the message is recorded. Other definitions still run. |

The `SynchronizationReport` aggregates the items with a completion timestamp. It is held in memory
only (see [Architecture](architecture.md#runtime-lifecycle)).
Before the first report, definitions are shown as `Pending`, not counted as skipped, and no completion
time is shown. A definition absent from a completed report is shown as `Skipped` with an explanation.
If coordination fails, its error is shown even when there are no definitions.
Data types with the same code-first alias are reported as failed and are not applied, preventing
them from overwriting one another. Give each data type a unique name or set `UCFDataType.Alias`
explicitly. Aliases default to a camel-cased version of the data type name.

Custom store implementations must return `SynchronizationResult` from both
`ICodeFirstDefinitionStore.ApplyAsync` and `ICodeFirstBlockDefinitionStore.PrepareBlockDataTypeAsync`
(if implemented), instead of returning `SynchronizationStatus` directly. Include an explanatory
message for skipped results, for example `new SynchronizationResult(SynchronizationStatus.Skipped,
"The definition is managed externally.")`.

## Clustered deployments and recovery

When enabled, the registered synchronizer coordinates through a package-specific claim in Umbraco's
key/value store. It uses Umbraco's database-backed key/value write lock only for short claim transactions;
definition operations keep their normal separate scopes. Instances with identical definition
fingerprints synchronize serially and each holds its own in-memory report. An instance with
different definitions fails without writing while an existing participating instance remains
registered. Drain old instances before starting the new version, and restart any new instance
whose synchronization was blocked.

Graceful shutdown removes the instance registration. A crash or lost database connection can leave
a claim or registration behind: it is deliberately not expired or taken over automatically. If
recovery is required, **stop and verify every instance capable of synchronization is stopped**,
including paused or partitioned instances, then clear the value of the key
`shorthand.umbraco-codefirst:synchronization-coordination` using `IKeyValueService.SetValue(key, "")`
in an administrative recovery process with automatic synchronization disabled. Start instances
only after recovery is complete and the recovery process has stopped. Clearing the claim while a
writer is alive can allow concurrent unsafe writes.

For a single deployment, after stopping and verifying that all instances are stopped, one way to
recover is to temporarily insert the following immediately after `await app.BootUmbracoAsync()` in
the application's `Program.cs`:

```csharp
if(Environment.GetEnvironmentVariable("UCF_RECOVER") == "true") {
    using(var scope = app.Services.GetRequiredService<Umbraco.Cms.Core.Scoping.ICoreScopeProvider>()
        .CreateCoreScope()) {
        scope.EagerWriteLock(Umbraco.Cms.Core.Constants.Locks.KeyValues);
        app.Services.GetRequiredService<Umbraco.Cms.Core.Services.IKeyValueService>()
            .SetValue("shorthand.umbraco-codefirst:synchronization-coordination", "");
        scope.Complete();
    }

    return;
}
```

Run this temporary recovery process once with both `UCF_RECOVER=true` and
`Shorthand__UmbracoCodeFirst__DisableStartupSynchronization=true` in its environment, and wait for
it to exit. Then remove the temporary code and both environment settings before starting the
application normally. Do not leave this recovery code in the application or run it while any other
instance could be active.

This protection requires every writer to use the registered `ICodeFirstSynchronizer`. Stop
versions predating coordination before adopting it; independently constructed synchronizers,
other tools, and manual changes do not participate. Clustered instances must share a database and
its distributed locking implementation. Coordination failures and timeouts are reported as
failures, not silently bypassed.

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

1. Look for an owned Data Type with the alias. If it exists, update name, editor and editor UI aliases and merge
   `ConfigurationJson` (only saved if something changed; configuration merging currently always
   counts as a change when configuration JSON is present).
2. Otherwise resolve the editor (`EditorAlias`) from Umbraco's property editors and create the Data
   Type with the ownership marker. An unknown editor fails the definition.

Created or found Data Types are cached for the run so Document Types can reference them
immediately. Property Data Type references resolve only against owned (code-first) Data Types; an
unknown reference fails that Document Type.

Changing an existing Data Type's editor does not convert stored values or change its database
storage type. Different editors can expect incompatible value formats, so inspect existing content
and migrate it as needed before changing the editor. An unknown replacement editor fails
synchronization without applying the change.

Typed block Data Types are staged: a new Data Type is first created with a minimal configuration,
element types are synchronized, and then block references are resolved to persisted element keys.
This also permits an element to contain a property using the block Data Type. Existing block
configuration is not replaced if references, typed/raw conflicts, or block configuration validation
fail. Missing, ambiguous, failed, non-element, or unowned element references produce a failed
report. Unchanged typed block configuration is not rewritten on subsequent synchronizations.
See [Block editors](property-editors.md#block-editors) for authoring and key stability.

## Document Types

For a new Document, Media or Member Type: create with name, description, icon, root/library and
variation settings, allowed children where applicable, then tabs/groups and properties.

For an existing **owned** Document Type, the following are reconciled:

- Name, description, icon, element status, `AllowAsRoot`, and `AllowInLibrary`
- Variations and allowed child aliases
- Document Type templates: allowed templates and default template
- Compositions (added; owned ones removed when dropped from code)
- Tabs and groups: created if missing; name, sort order and type (tab vs group) updated
- Properties: created if missing; existing properties have name, description, sort order,
  mandatory, validation expression/message and label-on-top updated, and are moved to the desired
  tab/group. Their Data Type is updated when the old and new storage types match.

Changing a property's Data Type does not migrate or convert its existing values. Editors can use
different value formats even when their storage types match, so check existing content and plan any
needed migration before changing the definition. A change to a different storage type fails
synchronization for that definition; migrate existing values and change the Data Type manually.

### Explicit cleanup and dry-run diagnostics

The `cleanup-preview` endpoint and the combined `reconciliation-preview` endpoint report obsolete
owned definitions and structural members without changing Umbraco. Applying cleanup is a separate
explicit operation. Manual or unowned definitions and composition members are preserved.

### Never done automatically

- Deleting Data Types, Document Types, properties, tabs or groups (removing something from code
  leaves it in Umbraco)
- Touching items that are not code-first-owned
- Removing compositions that were not previously added by code-first

List-view intent is reported but not persisted from the current boolean metadata because Umbraco
requires a concrete list-view definition key.

## Error handling

Failures are caught per definition, logged with the alias, and stored as `Failed` with the exception
message. Startup continues. The backoffice shows the message so you can diagnose it; typical causes
are an unknown editor alias, an unknown Data Type alias on a property, an unknown composition alias,
or an Umbraco validation failure.
