# Backoffice extension

The `Shorthand.UmbracoCodeFirst.Backoffice` project adds a read-only **Code First** page to the
Umbraco backoffice at **Settings > Advanced > Code First**
(`/umbraco/section/settings/view/code-first`).

## What the page shows

- **Header** – title, description, an overall synchronization status, the time of the last run and
  metric tiles (generated definitions, created, updated, unchanged, skipped, failed). Failures are
  highlighted both here and in the tables.
- **Data Types table** – name, alias, **Editor** (formatted like Umbraco: `EditorUiAlias |
  EditorAlias`), status, message and a link to the Data Type in the backoffice.
- **Content Types table** – name, alias, property count, status, message and a link to the
  Document Type. Rows expand to show the type's properties (alias, name, Data Type, group/tab,
  mandatory, etc.).
- **Failure messages** – for a failed item, the exception message from the failed synchronization.
- **Cleanup** – a button opens a review panel listing obsolete owned types, properties, groups and
  tabs. The user can cancel or explicitly proceed; cleanup is never automatic.

Links use the persisted Umbraco keys, so they appear once an item has actually been created.

## Localization

All user-facing strings go through Umbraco's localization system. English is registered
(`Client/src/localization/en.ts`); further languages can be added as additional localization
registrations in `Client/src/entrypoints/manifest.ts`.

## API

The page reads from a Management API endpoint:

- Route: `/umbraco/management/api/v1/shorthandumbracocodefirstbackoffice/status`
  (and `/ping`), group `Shorthand.UmbracoCodeFirst.Backoffice`
- Authorization: `AuthorizationPolicies.BackOfficeAccess` (backoffice users only)
- The controller maps the generated registry plus the latest `SynchronizationReport` to DTOs
  (`CodeFirstStatusModels.cs`): separate Data Type and Content Type collections, detailed property
  DTOs, backoffice URLs, editor aliases and status messages.

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
