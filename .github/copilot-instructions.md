# Copilot instructions for this repository

## Build, test, and validation commands

- Required SDK: `.NET SDK 10.0.403` from `global.json`.
- Restore packages:
  - `dotnet restore`
- Full solution build:
  - `dotnet build --configuration Release --no-restore`
- Full test suite:
  - `dotnet test test/Shorthand.UmbracoCodeFirst.Tests/Shorthand.UmbracoCodeFirst.Tests.csproj --configuration Release`
  - `dotnet test test/Shorthand.UmbracoCodeFirst.SourceGenerators.Tests/Shorthand.UmbracoCodeFirst.SourceGenerators.Tests.csproj --configuration Release`
  - `dotnet test test/Shorthand.UmbracoCodeFirst.Analyzers.Tests/Shorthand.UmbracoCodeFirst.Analyzers.Tests.csproj --configuration Release`
  - `dotnet test test/Shorthand.UmbracoCodeFirst.Backoffice.Tests/Shorthand.UmbracoCodeFirst.Backoffice.Tests.csproj --configuration Release`
  - `dotnet test test/Shorthand.UmbracoCodeFirst.InternalSourceGenerators.Tests/Shorthand.UmbracoCodeFirst.InternalSourceGenerators.Tests.csproj --configuration Release`
- Opt-in Umbraco integration tests:
  - PowerShell: `$env:RUN_UMBRACO_INTEGRATION_TESTS='true'`
  - `dotnet test test/Shorthand.UmbracoCodeFirst.Umbraco.Tests/Shorthand.UmbracoCodeFirst.Umbraco.Tests.csproj --configuration Release`
- Packaged-consumer lifecycle test (requires Docker and a solution restore):
  - `pwsh -File scripts/test-package-consumer.ps1`
  - The fixture restores freshly packed local NuGet packages and is intentionally outside the solution.
- Single test run (filter on a fully qualified name):
  - `dotnet test test/Shorthand.UmbracoCodeFirst.Tests/Shorthand.UmbracoCodeFirst.Tests.csproj --filter "FullyQualifiedName~Shorthand.UmbracoCodeFirst.Tests.UnitTest1.TestAsync"`
  - Discover available tests with:
    - `dotnet test test/Shorthand.UmbracoCodeFirst.Tests/Shorthand.UmbracoCodeFirst.Tests.csproj --list-tests`
- Backoffice client build:
  - `cd src/Shorthand.UmbracoCodeFirst.Backoffice/Client`
  - `npm install`
  - `npm test`
  - `npm run build`
  - `npm run watch` while developing the custom Umbraco backoffice extension
- Backoffice client tests use Vitest. Keep pure rendering/state helpers testable without importing
  the browser-only Umbraco runtime where possible.
- Local Aspire site lifecycle:
  - Start from the AppHost project: `cd src/Shorthand.UmbracoCodeFirst.AppHost` then `aspire start`.
  - Stop the AppHost resources from the same directory with `aspire stop`.
  - The local Umbraco backoffice is normally available at `https://localhost:44363/umbraco/`.
  - Use the AppHost for smoke testing so SQL Server, storage, Redis, and the backoffice client
    process are started with the same configuration as the local environment.
- Live smoke verification uses the local Aspire site at `https://localhost:44363/umbraco/`.
  Confirm the authenticated Settings > Advanced > Code First page loads all five definition tables,
  reports zero synchronization failures, and produces no browser console errors. Keep credentials
  local and never place them in source, tests, or documentation.
- Available MCP servers:
  - `aspire`: inspect AppHost resources, logs, traces, and Aspire documentation.
  - `microsoft/playwright-mcp`: navigate and interact with the local Umbraco site, inspect
    accessibility snapshots, network requests, and browser console messages.
  - `ide`: obtain editor selection and language-server diagnostics.
  - `github-mcp-server`: GitHub repository/code/user lookups when GitHub operations are required.
  - `NuGet`: package metadata, version checks, vulnerability review, and package updates.
  - Deferred MCP tools must be discovered with the tool search mechanism before direct invocation;
    do not guess deferred tool names or arguments.
- There is no standalone `lint` script in the repo; build-time analyzers are the effective linting step. The repository explicitly enables warnings-as-errors and code-style enforcement in `Directory.Build.props`.

## High-level architecture

This repo is a small Umbraco app template built around a layered, local-development setup:

- `src/Shorthand.UmbracoCodeFirst` is the core library/package.
- `src/Shorthand.UmbracoCodeFirst.Umbraco` is the actual Umbraco web app. Its `Program.cs` wires `CreateUmbracoBuilder()`, `AddBackOffice()`, `AddWebsite()`, and `BootUmbracoAsync()`; this is the runtime entry point for the site.
- `src/Shorthand.UmbracoCodeFirst.Backoffice` is a Razor Class Library and custom backoffice extension for Umbraco. The TypeScript client lives under `Client/` and is built into `wwwroot/App_Plugins/ShorthandUmbracoCodeFirstBackoffice`.
- `src/Shorthand.UmbracoCodeFirst.AppHost` is the Aspire-based local orchestrator: it creates an Umbraco app, wires in SQL Server, Azure storage, Redis, and the JS backoffice extension watch process.
- `src/Shorthand.UmbracoCodeFirst.ServiceDefaults` contains shared .NET Aspire defaults used by the app host and hosted services.
- `src/Shorthand.UmbracoCodeFirst.SourceGenerators` is present as a dedicated code-generation project, so source generation is part of the repo's expected design even if the generated code hasn't been expanded yet.
- `test/` contains the test projects and shared test helpers. The repo uses `xUnit`, `Shouldly`,
  `FakeItEasy`, Roslyn analyzer/generator verifier tests, and the newer `Microsoft.Testing.Platform`
  runner. The backoffice client uses Vitest separately.

## Key conventions

- This repo uses central package management via `Directory.Packages.props`; do not add ad hoc package versions in project files.
- The solution is a `.slnx` file, not a traditional `.sln`.
- Repository-wide build settings are enforced in `Directory.Build.props` and `src/Directory.Build.props`:
  - `TargetFramework` is `net10.0`
  - `Nullable` is enabled
  - `TreatWarningsAsErrors` is enabled
  - `EnforceCodeStyleInBuild` is enabled
  - `ImplicitUsings` is enabled
- Use the repo's existing patterns for tests and infrastructure instead of inventing a parallel setup. The tests are under `test/`, and the repo intentionally wires in test helpers and coverage support via the shared props files.
- Structure test cases into Arrange, Act, and Assert sections, each marked with `// Arrange`,
  `// Act`, and `// Assert` comments. Keep setup, the operation under test, and checks in their
  respective sections; use a single `// Act & Assert` comment when a statement does both.
- Preserve the layered coverage model: fast core/generator/analyzer/backoffice tests are service
  independent; SQL-backed Umbraco tests are opt-in; browser automation is supplementary smoke
  coverage rather than the sole assertion source.
- When adding a new public definition setting or definition kind, add coverage at the metadata or
  generator layer, synchronizer/persistence layer, status/API mapping layer, and integration layer
  when the Umbraco runtime supports it.
- Synchronization tests must preserve the non-destructive ownership invariant: code-first-owned
  definitions may be updated, unowned definitions and manual structure must be preserved, and
  skipped or failed outcomes must remain visible in reports.
- For any backoffice UI work, update the TypeScript client in `src/Shorthand.UmbracoCodeFirst.Backoffice/Client` and rebuild with `npm run build` or `npm run watch`; the generated bundle is not hand-edited.
- The app host is the easiest path for running the application locally; it sets unattended Umbraco installation values and spins up the required backing services.
- CI is defined in `.github/workflows/build.yml` and follows the same pattern as the local commands: restore, build, and run tests with coverage.
