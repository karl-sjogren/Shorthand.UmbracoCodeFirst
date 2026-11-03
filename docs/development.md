# Development

## Build and test

```powershell
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test test/Shorthand.UmbracoCodeFirst.Tests/Shorthand.UmbracoCodeFirst.Tests.csproj --configuration Release
dotnet format --verify-no-changes
```

Warnings are errors and code style is enforced in builds (`Directory.Build.props`). Packages are
managed centrally in `Directory.Packages.props`; the solution file is `.slnx`. CI
(`.github/workflows/build.yml`) restores, builds and tests with coverage.

## Test projects

Every production project has a `ProjectName.Tests` project under `test/`.

| Project | Covers |
| ------- | ------ |
| `Shorthand.UmbracoCodeFirst.Tests` | Synchronizer ordering (data types first, compositions first) using a fake store |
| `…SourceGenerators.Tests` | Generated registry/models, derived group/tab attributes, Data Type aliases, compositions, UCF001 |
| `…Analyzers.Tests` | Each analyzer rule and the help links of all diagnostics |
| `…Backoffice.Tests` | Status DTO mapping: split tables, URLs, property details, editor aliases |
| `…Umbraco.Tests` | Generated registry content, and an integration test that boots the site against a throw-away SQL Server container (Testcontainers.MsSql) and asserts persisted Data Types, `productPage`, groups and the `article` composition |
| `…AppHost.Tests`, `…ServiceDefaults.Tests` | Small host/defaults contract tests |
| `…TestHelpers` | Shared helpers, including the reusable Umbraco integration fixture |

Conventions: test names are `MethodUnderTest_ScenarioBeingTested_ExpectedResult`, and each test has
`// Arrange`, `// Act` and `// Assert` sections. Integration tests are marked
`[Trait("Category", "Integration")]` and skip themselves when the required container runtime is not
available.

## Running the sample site

The Aspire AppHost starts the site with SQL Server, an Azure storage emulator, Redis and the
backoffice client watcher, with unattended Umbraco installation values:

```powershell
dotnet run --project src/Shorthand.UmbracoCodeFirst.AppHost
```

The integration tests deliberately do **not** use the AppHost; they use only a SQL Server
container.

## Adding a feature

1. Extend the attribute and/or metadata record (`Shorthand.UmbracoCodeFirst`).
2. Emit it from the generator and add generator tests.
3. Apply it in `UmbracoCodeFirstDefinitionStore` and extend the integration test.
4. Add an analyzer rule + `docs/analyzers/UCFxxx.md` page if the new input can be invalid.
5. Surface it in the status DTOs / client if it is useful for diagnostics.
