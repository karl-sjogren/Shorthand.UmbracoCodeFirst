# Backoffice extension

The `Shorthand.UmbracoCodeFirst.Backoffice` project adds a read-only **Code First** page to the
Umbraco backoffice at **Settings > Advanced > Code First**
(`/umbraco/section/settings/workspace/shorthand-code-first`).

## What the page shows

- **Header** – title, description, an overall synchronization status, the time of the last run and
  metric tiles (generated definitions, created, updated, unchanged, skipped, failed). Failures are
  highlighted both here and in the tables.
- **Data Types table** – name, alias, **Editor** (formatted like Umbraco: `EditorUiAlias |
  EditorAlias`), status, message and row actions.
- **Document, Element, Media and Member Type tables** – name, alias, folder, property count,
  status, message and row actions. Rows expand to show type
  settings, compositions, tabs/groups and properties (alias, name, Data Type, mandatory,
  validation, etc.).
- **Failure messages** – for a failed item, the exception message from the failed synchronization.
- **Row actions** – where available, open the corresponding Umbraco workspace or inspect generated
  code. The source action opens the actual generated C# in a read-only,
  syntax-highlighted viewer. Choose a file to see the original source, definition-specific output
  or complete shared registry; templates also include the complete shared template aliases file.
  Partial declarations can have multiple original files. The originals are snapshots from the build,
  not live files.
- **Cleanup** – a button opens a review panel listing obsolete owned types, properties, groups and
  tabs. The user can cancel or explicitly proceed; cleanup is never automatic.
- **Reconciliation preview** – the API exposes the latest synchronization report together with the
  non-destructive cleanup preview for diagnostics and tooling.

Links use the persisted Umbraco keys, so they appear once an item has actually been created.
The project's GitHub repository is linked from the backoffice Help menu.

The generated-code action requires the project containing the generated registry to be built in
**Debug** and the running host to use the **Development** environment. If models are in a separate
project, that models project must be built in Debug even if the host is built separately. Only
backoffice users with Settings access can retrieve the source. The catalog is embedded in the Debug
assembly, including complete original files that may contain unrelated sensitive information.
Do not distribute Debug builds if this source is sensitive; the runtime gate does not remove it
from the assembly.

## Localization

Backoffice interface strings use Umbraco's localization system. English and Swedish are registered
(`Client/src/localization/en.ts` and `sv.ts`); further languages can be added as additional
localization registrations in `Client/src/entrypoints/manifest.ts`. Definition content and
diagnostic messages supplied by the server are displayed as received.

## API

The page reads from a Management API endpoint:

- Route: `/umbraco/management/api/v1/shorthandumbracocodefirstbackoffice/status`
  (plus `/generated-source/{id}`, `/reconciliation-preview`, `/cleanup-preview`, `/cleanup` and `/ping`), group
  `Shorthand.UmbracoCodeFirst.Backoffice`
- Authorization: `AuthorizationPolicies.BackOfficeAccess` (backoffice users only)
- Generated-source requests additionally require `AuthorizationPolicies.SectionAccessSettings`,
  a Development host and a Debug-generated catalog; responses are not cached.
- The controller maps the generated registry plus the latest `SynchronizationReport` to DTOs
  (`CodeFirstStatusModels.cs`): separate Data Type, Document Type, Element Type, Media Type and
  Member Type collections, detailed settings/property DTOs, backoffice URLs, editor aliases and
  status messages.

The report is the in-memory report of the current process; restarting the site re-runs the
synchronization and refreshes it.

See [Explicit cleanup](cleanup.md) for the candidate and ownership rules.

## Building the client

The TypeScript source is in `src/Shorthand.UmbracoCodeFirst.Backoffice/Client`; the built bundle is
emitted to `wwwroot/App_Plugins/ShorthandUmbracoCodeFirstBackoffice` and must not be edited by hand.

```powershell
cd src/Shorthand.UmbracoCodeFirst.Backoffice/Client
npm install
npm run build      # or: npm run watch
```

The sample site's blog collection view lives separately in
`src/Shorthand.UmbracoCodeFirst.Umbraco.Backoffice/Client`. It uses the same npm build and watch
commands, emits its own plugin bundle, and is not included in the distributed backoffice package.
The Aspire AppHost watches both clients during local development.
