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

## Analyzer release tracking

The analyzer, source generator, and internal source generator projects each have
`AnalyzerReleases.Shipped.md` and `AnalyzerReleases.Unshipped.md` files. Add new diagnostics, or
changes to a diagnostic's category, default severity, or enabled status, to the owning project's
unshipped file; record removals there too. When publishing an analyzer package, move its unshipped
entries into a new release section in its shipped file. The
`Microsoft.CodeAnalysis.Analyzers` build targets automatically include both files as analyzer
additional inputs.

Roslyn's release file format accepts numeric versions only. The existing `0.0.2.79` section is a
baseline snapshot of the rules published by `0.0.2-alpha-79`, not a claim that the rules were
introduced in that specific prerelease. For subsequent prereleases, use the numeric form
`major.minor.patch.runNumber` (for example, `0.0.2.80` for `0.0.2-alpha-80`). If a final release
needs a section after prereleases, use a revision greater than their run numbers (for example,
`0.0.2.2147483647` for `0.0.2`). The internal generator is not published as an analyzer package,
so its rules remain unshipped.

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
