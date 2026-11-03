# Copilot instructions for this repository

## Build, test, and validation commands

- Required SDK: `.NET SDK 10.0.403` from `global.json`.
- Restore packages:
  - `dotnet restore`
- Full solution build:
  - `dotnet build --configuration Release --no-restore`
- Full test suite:
  - `dotnet test test/Shorthand.UmbracoCodeFirst.Tests/Shorthand.UmbracoCodeFirst.Tests.csproj --configuration Release`
- Single test run (filter on a fully qualified name):
  - `dotnet test test/Shorthand.UmbracoCodeFirst.Tests/Shorthand.UmbracoCodeFirst.Tests.csproj --filter "FullyQualifiedName~Shorthand.UmbracoCodeFirst.Tests.UnitTest1.TestAsync"`
  - Discover available tests with:
    - `dotnet test test/Shorthand.UmbracoCodeFirst.Tests/Shorthand.UmbracoCodeFirst.Tests.csproj --list-tests`
- Backoffice client build:
  - `cd src/Shorthand.UmbracoCodeFirst.Backoffice/Client`
  - `npm install`
  - `npm run build`
  - `npm run watch` while developing the custom Umbraco backoffice extension
- There is no standalone `lint` script in the repo; build-time analyzers are the effective linting step. The repository explicitly enables warnings-as-errors and code-style enforcement in `Directory.Build.props`.

## High-level architecture

This repo is a small Umbraco app template built around a layered, local-development setup:

- `src/Shorthand.UmbracoCodeFirst` is the core library/package.
- `src/Shorthand.UmbracoCodeFirst.Umbraco` is the actual Umbraco web app. Its `Program.cs` wires `CreateUmbracoBuilder()`, `AddBackOffice()`, `AddWebsite()`, and `BootUmbracoAsync()`; this is the runtime entry point for the site.
- `src/Shorthand.UmbracoCodeFirst.Backoffice` is a Razor Class Library and custom backoffice extension for Umbraco. The TypeScript client lives under `Client/` and is built into `wwwroot/App_Plugins/ShorthandUmbracoCodeFirstBackoffice`.
- `src/Shorthand.UmbracoCodeFirst.AppHost` is the Aspire-based local orchestrator: it creates an Umbraco app, wires in SQL Server, Azure storage, Redis, and the JS backoffice extension watch process.
- `src/Shorthand.UmbracoCodeFirst.ServiceDefaults` contains shared .NET Aspire defaults used by the app host and hosted services.
- `src/Shorthand.UmbracoCodeFirst.SourceGenerators` is present as a dedicated code-generation project, so source generation is part of the repo's expected design even if the generated code hasn't been expanded yet.
- `test/` contains the test projects and shared test helpers. The repo uses `xUnit`, `Shouldly`, `FakeItEasy`, and the newer `Microsoft.Testing.Platform` runner.

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
- For any backoffice UI work, update the TypeScript client in `src/Shorthand.UmbracoCodeFirst.Backoffice/Client` and rebuild with `npm run build` or `npm run watch`; the generated bundle is not hand-edited.
- The app host is the easiest path for running the application locally; it sets unattended Umbraco installation values and spins up the required backing services.
- CI is defined in `.github/workflows/build.yml` and follows the same pattern as the local commands: restore, build, and run tests with coverage.
