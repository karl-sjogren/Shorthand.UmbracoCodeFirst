# Shorthand.UmbracoCodeFirst

Code-first definitions for Umbraco Document Types and Data Types.

> [!WARNING]
> This whole project was vibe coded over a few evenings and the code quality is not great.
> I see this more of a proof of concept than a production-ready library. It will require
> a lot further refinement to become a robust and production-ready library. But it's kinda cool.

Instead of configuring every definition manually in the Umbraco backoffice, declare partial C#
classes and properties with `UCFDocumentType`, `UCFDataType`, `UCFProperty`, tabs, groups,
validation attributes, and compositions. A Roslyn source generator creates the definition registry
and typed published-model members. When the Umbraco application starts, the synchronizer creates or
updates the matching definitions.

The repository also contains a backoffice extension under **Settings > Advanced > Code First**.
It displays generated definitions, synchronization results, failures, and an explicit cleanup
preview for obsolete code-first-owned items.

> **Current status:** This is an active MVP. Document Types, Data Types, compositions, generated
> published models, analyzers, synchronization, and the backoffice status page are implemented.
> Media Types and Member Types are not implemented yet, and synchronization is intentionally
> non-destructive. See the [limitations and roadmap](docs/limitations-and-roadmap.md).

## Example

```csharp
using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.PropertyEditors;

[UCFDataType(
    "Article title",
    typeof(UmbracoEditors.TextBox))]
public partial class ArticleTitleDataType;

[UCFDocumentType("article", "Article", AllowAsRoot = true)]
public partial class Article {
    [UCFProperty(
        "title",
        "Title",
        ArticleTitleDataType.Alias,
        Mandatory = true)]
    [UCFTab("content", "Content")]
    public partial string Title { get; }
}
```

Classes and code-first properties must be `partial`; the generator supplies the generated members.
Use `UCFComposite(typeof(OtherDocumentType))` to compose another Document Type rather than using C#
inheritance. See the [getting started guide](docs/getting-started.md) for a complete example and
the [attribute reference](docs/attributes.md) for all supported settings.

## How it works

```text
C# partial classes and attributes
              |
              v
Roslyn source generator and analyzers
              |
              +--> definition registry
              +--> typed published models
              +--> compile-time diagnostics
              |
              v
CodeFirstSynchronizer after Umbraco boots
              |
              v
Umbraco Data Types and Document Types
              |
              v
Settings > Advanced > Code First
```

Definitions are reconciled by stable aliases and code-first ownership markers. Existing manual
definitions are preserved, and synchronization does not delete obsolete definitions or properties.
Destructive cleanup is a separate, explicit backoffice action with a preview and confirmation step.

## Repository structure

| Path | Purpose |
| --- | --- |
| `src/Shorthand.UmbracoCodeFirst` | Public attributes, metadata contracts, editor aliases, and synchronization abstractions |
| `src/Shorthand.UmbracoCodeFirst.SourceGenerators` | Incremental Roslyn source generator |
| `src/Shorthand.UmbracoCodeFirst.Analyzers` | Compile-time code-first diagnostics |
| `src/Shorthand.UmbracoCodeFirst.Backoffice` | Umbraco persistence, startup composition, API, cleanup service, and backoffice extension |
| `src/Shorthand.UmbracoCodeFirst.Umbraco` | Sample Umbraco site and example models |
| `src/Shorthand.UmbracoCodeFirst.AppHost` | .NET Aspire local orchestrator |
| `test/` | Unit, analyzer, source-generator, backoffice, and opt-in SQL-backed integration tests |
| `docs/` | Detailed guides for attributes, architecture, synchronization, compositions, cleanup, and development |

## Requirements

- .NET SDK 10.0.403, selected by [`global.json`](global.json)
- Umbraco CMS 18.3
- Docker Desktop for the opt-in SQL-backed integration tests
- Node.js and npm for the backoffice client

## Build and test

Restore and build the solution:

```powershell
dotnet restore
dotnet build --configuration Release --no-restore
```

Run the ordinary .NET tests:

```powershell
dotnet test test/Shorthand.UmbracoCodeFirst.Tests/Shorthand.UmbracoCodeFirst.Tests.csproj --configuration Release
```

Run the backoffice tests:

```powershell
dotnet test test/Shorthand.UmbracoCodeFirst.Backoffice.Tests/Shorthand.UmbracoCodeFirst.Backoffice.Tests.csproj --configuration Release
```

The Umbraco integration tests are opt-in because they start SQL Server in Docker:

```powershell
$env:RUN_UMBRACO_INTEGRATION_TESTS = "true"
dotnet test test/Shorthand.UmbracoCodeFirst.Umbraco.Tests/Shorthand.UmbracoCodeFirst.Umbraco.Tests.csproj --configuration Release
```

Build the backoffice client:

```powershell
cd src/Shorthand.UmbracoCodeFirst.Backoffice/Client
npm install
npm run build
```

Formatting and analyzers are enforced during the build. To check formatting without changing
files:

```powershell
dotnet format --verify-no-changes
```

## Local sample site

The sample site is hosted through the Aspire app host. It provisions the local supporting services
and runs the Umbraco application together with the backoffice client workflow. The sample models are
examples and are not used by the isolated integration tests.

See [development](docs/development.md) for local hosting, test categories, and troubleshooting.

## Documentation

- [Getting started](docs/getting-started.md)
- [Architecture](docs/architecture.md)
- [Attribute reference](docs/attributes.md)
- [Property editor metadata](docs/property-editors.md)
- [Source generator](docs/source-generator.md)
- [Synchronization and ownership](docs/synchronization.md)
- [Compositions](docs/compositions.md)
- [Backoffice extension](docs/backoffice.md)
- [Explicit cleanup](docs/cleanup.md)
- [Analyzers](docs/analyzers/README.md)
- [Development](docs/development.md)
- [Limitations and roadmap](docs/limitations-and-roadmap.md)
