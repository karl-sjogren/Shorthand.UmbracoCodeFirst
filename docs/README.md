# Shorthand.UmbracoCodeFirst documentation

Shorthand.UmbracoCodeFirst lets you describe Umbraco Data Types and Document Types as partial C#
classes decorated with attributes. A Roslyn source generator turns those declarations into metadata
and strongly typed published models, and at startup the site creates or updates the matching
definitions in Umbraco. A backoffice page under **Settings** shows the result.

> **Status: MVP.** Document Types and Data Types are supported. Media Types and Member Types are
> not implemented yet. See [Limitations and roadmap](limitations-and-roadmap.md) for exactly what
> is and is not covered.

## Contents

| Document | What it covers |
| -------- | -------------- |
| [Architecture](architecture.md) | Projects, data flow from C# to Umbraco, the runtime lifecycle |
| [Getting started](getting-started.md) | Declaring Data Types and Document Types, a complete example |
| [Attribute reference](attributes.md) | Every attribute, its arguments, aliases and defaults |
| [Source generator](source-generator.md) | What is generated, naming rules, diagnostics |
| [Synchronization](synchronization.md) | How definitions are created/updated, ownership, what is never touched |
| [Compositions](compositions.md) | `UCFComposite`, ordering and ownership of compositions |
| [Backoffice extension](backoffice.md) | The Settings > Code First page and its API |
| [Analyzers](analyzers/README.md) | Compile-time rules UCF100–UCF107 |
| [Development](development.md) | Building, testing, the local Aspire host |
| [Limitations and roadmap](limitations-and-roadmap.md) | What is implemented, partially implemented and planned |
