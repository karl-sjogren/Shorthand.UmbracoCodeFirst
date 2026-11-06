# Shorthand.UmbracoCodeFirst.Backoffice

This Razor Class Library adds the Code First page to the Umbraco backoffice. It exposes a
backoffice-only API and ships the TypeScript bundle that displays synchronization status and
performs ownership-aware cleanup.

## What belongs here

- `Composers/` registers synchronization, status reporting, cleanup, and the Code First OpenAPI
  document with Umbraco.
- `Controllers/` exposes the protected management API used by the backoffice client.
- `Synchronization/` applies generated Code First definitions to Umbraco services and records
  ownership metadata for content types.
- `Cleanup/` previews and removes obsolete **Code First-owned** definitions and standalone
  members. It preserves manually managed definitions and composed members.
- `Contracts/` contains service contracts intended for consumers of the backoffice assembly, such
  as `ICodeFirstCleanupService`.
- `Client/` contains the Lit/Vite backoffice extension source.

The host must reference this project and call Umbraco's composer registration (`AddComposers()`).
Umbraco discovers the two `IComposer` implementations and the package manifest loads the client
bundle from `/App_Plugins/ShorthandUmbracoCodeFirstBackoffice/`.

## Management API

Every endpoint requires the Umbraco backoffice access policy. The client calls these versioned
routes:

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/umbraco/management/api/v1/shorthandumbracocodefirstbackoffice/ping` | Connectivity check |
| `GET` | `/umbraco/management/api/v1/shorthandumbracocodefirstbackoffice/status` | Definition and synchronization status |
| `GET` | `/umbraco/management/api/v1/shorthandumbracocodefirstbackoffice/cleanup-preview` | Lists safely removable items |
| `GET` | `/umbraco/management/api/v1/shorthandumbracocodefirstbackoffice/reconciliation-preview` | Combines synchronization and cleanup previews |
| `POST` | `/umbraco/management/api/v1/shorthandumbracocodefirstbackoffice/cleanup` | Removes the previewed Code First-owned items |

The OpenAPI document name is `shorthandumbracocodefirstbackoffice`.

## Client development

Run these commands from `Client/`:

```powershell
npm install
npm test
npm run build
```

`npm run build` type-checks and bundles the extension into
`wwwroot/App_Plugins/ShorthandUmbracoCodeFirstBackoffice/`. Do not edit that output manually.
Use `npm run watch` while developing against a running Umbraco site.

`npm run generate-client` regenerates the OpenAPI client from the endpoint configured in
`Client/package.json`; start the local Umbraco application first.

## Verification

Server changes are covered by the Backoffice test project:

```powershell
dotnet test test/Shorthand.UmbracoCodeFirst.Backoffice.Tests/Shorthand.UmbracoCodeFirst.Backoffice.Tests.csproj --configuration Release
```

For browser smoke testing, start the AppHost and open the authenticated **Settings > Advanced >
Code First** page. Confirm that all five definition tables load, synchronization failures are
visible, and the browser console has no errors.
