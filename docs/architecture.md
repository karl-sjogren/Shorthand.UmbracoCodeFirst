# Architecture

## Overview

```text
 partial classes + attributes          (your code)
            │
            ▼
 Roslyn source generator               (compile time)
   ├─ GeneratedCodeFirstDefinitionRegistry   -> metadata for every definition
   ├─ <Type>.PublishedModel.g.cs             -> typed Umbraco published model per Document Type
   └─ <Type>.DataType.g.cs                   -> `Alias` constant per Data Type
            │
            ▼
 Analyzers                              (compile time, editor feedback)
            │
            ▼
 CodeFirstSynchronizer                  (site startup, after Umbraco boots)
   └─ UmbracoCodeFirstDefinitionStore   -> IDataTypeService / IContentTypeService
            │
            ▼
 Backoffice page (Settings > Advanced > Code First)  <- Management API endpoint
```

## Projects

| Project | Purpose |
| ------- | ------- |
| `Shorthand.UmbracoCodeFirst` | The public contract: attributes, the immutable metadata records (`CodeFirstDefinition` and friends), the Umbraco editor registry (`UmbracoEditors`), and the Umbraco-independent synchronizer (`CodeFirstSynchronizer`, report types). |
| `Shorthand.UmbracoCodeFirst.SourceGenerators` | Incremental generator (`CodeFirstRegistryGenerator`) producing the registry, published models and Data Type alias constants. |
| `Shorthand.UmbracoCodeFirst.Analyzers` | Dedicated analyzers for diagnostics UCF100–UCF109 plus the UCF102 code fix. |
| `Shorthand.UmbracoCodeFirst.Backoffice` | Razor class library: the Umbraco persistence adapter (`UmbracoCodeFirstDefinitionStore`), the DI composer, the Management API controller and the TypeScript client for the Settings page. |
| `Shorthand.UmbracoCodeFirst.Umbraco` | Sample Umbraco site. Its `Models/` folder contains the sample Data Types and Document Types and runs the synchronizer at startup. |
| `Shorthand.UmbracoCodeFirst.AppHost` | .NET Aspire host for local development (SQL Server, Azure storage emulator, Redis, backoffice client watcher). |
| `Shorthand.UmbracoCodeFirst.ServiceDefaults` | Shared Aspire service defaults. |

Test projects live under `test/` and follow `ProjectName.Tests`; see [Development](development.md).

## Design principles

- **The metadata contract is Umbraco-independent.** `CodeFirstDefinition` is a plain record keyed by
  aliases (strings), so the generator never needs Umbraco types and the synchronizer can be tested
  with a fake store.
- **Persistence is isolated.** Everything that touches Umbraco lives in
  `UmbracoCodeFirstDefinitionStore`, behind the `ICodeFirstDefinitionStore` interface.
- **Non-destructive.** The synchronizer never deletes Data Types, Document Types or properties and
  only modifies definitions it owns. See [Synchronization](synchronization.md).
- **Explicit, not inferred.** Editors, Data Types and property settings are declared with
  attributes; nothing is inferred from CLR types.
- **Failures are visible.** A failing definition does not abort startup; it is recorded as `Failed`
  with its message and shown in the backoffice.

## Runtime lifecycle

1. `ShorthandUmbracoCodeFirstComposer` registers (as singletons) the registry, the report store,
   the store implementation and `CodeFirstSynchronizer`.
2. The registry is found by `CodeFirstDefinitionRegistryDiscovery.Discover()`, which scans loaded
   assemblies for the first parameterless `ICodeFirstDefinitionRegistry` implementation – in
   practice the generated `Shorthand.UmbracoCodeFirst.Generated.GeneratedCodeFirstDefinitionRegistry`.
   If none is found an empty registry is used.
3. The package's Umbraco startup notification handler awaits synchronization once the runtime
   reaches `Run`. A database-backed coordination claim serializes participating instances; see
   [Synchronization](synchronization.md#clustered-deployments-and-recovery).
4. The resulting `SynchronizationReport` is kept in memory (`ISynchronizationReportStore`) and is
   what the backoffice endpoint returns. It is not persisted, so it reflects the latest run on
   the running process.

## Ordering

`CodeFirstSynchronizer` applies definitions in this order:

1. All Data Types, ordered by alias.
2. Document Types, ordered so that every composition is applied before the types that compose it
   (depth-first over `CompositionAliases`, ties broken by alias).

Data Types come first so property Data Type references resolve, including Data Types created
during the same run.
