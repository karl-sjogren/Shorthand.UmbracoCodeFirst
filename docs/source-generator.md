# Source generator

`CodeFirstRegistryGenerator` (project `Shorthand.UmbracoCodeFirst.SourceGenerators`) is an
incremental generator. It runs over `partial` code-first classes and marked Razor views below the configured views directory and
emits registry, model and alias sources. Output is deterministic (definitions are ordered by fully qualified
name).

## Generated output

In Debug builds the generator also emits `CodeFirstSourceCatalog.g.cs`, which embeds the exact
generated files and the original declaration files (including Razor templates) for the backoffice
source viewer. These are build-time snapshots; complete original files may include unrelated
sensitive information. Shared files are stored once and presented in full for each applicable
definition. The catalog is absent in Release or when the configuration
is unknown. For a separate models project, its build configuration controls catalog generation,
independently of the host project's configuration. Debug assemblies therefore contain generated
and original source even when deployed to a non-Development environment; avoid distributing them if that source
is sensitive. The viewer itself additionally requires a Development host and Settings access.

### Registry – `CodeFirstRegistry.g.cs`

`Shorthand.UmbracoCodeFirst.Generated.GeneratedCodeFirstDefinitionRegistry` implements
`ICodeFirstDefinitionRegistry` and exposes a `Definitions` list of `CodeFirstDefinition` records.
Each definition carries:

- Kind (`DataType` / `DocumentType`), alias, name
- Templates: relative-path-derived alias, display name and a content hash for cluster coordination
- Data Types: editor alias, editor UI alias, configuration JSON
- Document Types: `CodeFirstTypeSettings`, the property list (`CodeFirstPropertyDefinition` with
  alias, name, Data Type alias, description, group/tab alias+name+sort, sort order, mandatory,
  validation expression/message, label-on-top) and composition aliases (sorted, de-duplicated).

`CodeFirstDefinition` also computes `EffectiveTabs` and `EffectiveGroups` from the properties.

### Published models – `<Type>.PublishedModel.g.cs`

For every `UCFDocument` and `UCFElementType` class a partial implementation is emitted in the
class's own namespace:

- `[PublishedModel("alias")]` and `: PublishedContentModel` for documents, or
  `: PublishedElementModel` for elements
- `ModelTypeAlias`, `ModelItemType` (`Content`, or `Element` for `UCFElementType`),
  `GetModelContentType`, `GetModelPropertyType<TValue>`
- a constructor `(IPublishedContent, IPublishedValueFallback)` for documents, or
  `(IPublishedElement, IPublishedValueFallback)` for elements
- one generated property per directly declared `UCFProperty`, implemented with
  `this.Value<T>(_publishedValueFallback, "alias")` and decorated with
  `[ImplementPropertyType("alias")]`

This follows the shape of Umbraco's ModelsBuilder output without depending on its internals. The
property declarations in your class are `partial` definitions; do not add your own implementation.
Composition targets additionally expose a generated interface and static getters, and consumers
receive properties from same-compilation composites. See [Compositions](compositions.md).

### Property aliases

Code-first document, element, media and member types expose a generated `PropertyAliases` class.
For each `UCFProperty`, its constant is named after the CLR property and contains the same explicit
or display-name-derived alias emitted in the registry. For example, `Article.PropertyAliases.Title`
is a compile-time string constant; `CodeFirstHelper<Article>.Alias(x => x.Title)` returns the same
alias and supports normal property rename refactoring. Document and element consumers also expose
constants for properties from same-compilation composites.

If a model already declares `PropertyAliases`, UCF116 reports the conflict. UCF117 reports a
generated property alias used by a UCF attribute in the same compilation: the generator cannot
read its own output during its initial pass. Other C# attribute arguments and references from a
previously compiled model can use the generated constants. For media and member models, UCF118
reports file-local types or nested types whose containing types are not partial.

### Data Type alias – `<Type>.DataType.g.cs`

A `public const string Alias` on each Data Type class, so `UCFProperty` can reference
`MyDataType.Alias`. The generator resolves these references itself (including when the constant has
not been bound yet), so ordering of files does not matter.

### Template aliases – `UmbracoTemplates.g.cs`

Marked views generate `Shorthand.UmbracoCodeFirst.UmbracoTemplates` constants.
Root views use the filename stem; nested views use a hierarchical C# shape such as
`UmbracoTemplates.Features.Blog.Default`, whose value is the flattened alias
`Features_Blog_Default`. The generator can consume a configurable external Views directory, so
the models project owns both the constants and template registry entries while the site owns the
physical views. See [UCFTemplate](attributes.md#ucftemplate--razor-view) for configuration and
the single-registry requirement.

## Alias derivation

When no alias is supplied, separators and PascalCase word boundaries are detected. The first word
is lower-case; each following word starts upper-case; all other letters are lower-cased.

| Name | Alias |
| ---- | ----- |
| `Body text` | `bodyText` |
| `Product Page` | `productPage` |
| `SEO` | `seo` |
| `ProductPage` | `productPage` |

Because letters inside a word are lower-cased, supply explicit aliases when you need mixed-case
(`"productPage"`).

## Composition resolution

`typeof(Target)` arguments of `UCFComposite` are resolved to the target's document or element alias.
An invalid target falls back to a derived alias; analyzer UCF106 reports it. Only same-compilation
targets contribute generated model interfaces and properties; referenced targets retain their
registry aliases but do not contribute a generated consumer contract.

## Generator diagnostics

| Id | Severity | Meaning |
| -- | -------- | ------- |
| [UCF001](analyzers/generator-diagnostics.md#ucf001) | Error | A published-model property has an unsupported CLR type. |
| [UCF111](analyzers/generator-diagnostics.md#ucf111) | Error | A Data Type configuration has no usable settings or JSON. |
| [UCF112](analyzers/generator-diagnostics.md#ucf112) | Error | A constant configuration setting has an unsupported type. |
| [UCF113](analyzers/generator-diagnostics.md#ucf113) | Error | An allowed-child alias expression cannot be resolved. |
| [UCF114](analyzers/generator-diagnostics.md#ucf114) | Error | A collection configuration or its target is invalid. |
| [UCF116](analyzers/generator-diagnostics.md#ucf116) | Error | A model declaration conflicts with the generated `PropertyAliases` class. |
| [UCF117](analyzers/generator-diagnostics.md#ucf117) | Error | A UCF attribute references a property alias generated in the same compilation. |
| [UCF118](analyzers/generator-diagnostics.md#ucf118) | Error | A media or member model's container prevents generation of its property aliases. |
| [UCF120](analyzers/generator-diagnostics.md#ucf120) | Error | A template directive or filename is invalid. |
| [UCF121](analyzers/generator-diagnostics.md#ucf121) | Error | Multiple marked views have the same template alias (case-insensitive). |
| [UCF122](analyzers/generator-diagnostics.md#ucf122) | Error | Multiple template aliases generate the same `UmbracoTemplates` constant. |
| [UCF123](analyzers/generator-diagnostics.md#ucf123) | Error | A property does not specify exactly one data type alias or valid data type key. |
| [UCF124](analyzers/generator-diagnostics.md#ucf124) | Error | The site and a referenced template-owning project both generate code-first registries. |
| [UCF125](analyzers/generator-diagnostics.md#ucf125) | Error | A generated composite property, getter, or interface conflicts with another member or type. |

Other rules include those reported by the [analyzers](analyzers/README.md).
The internal catalog generators also report [UCF200 and UCF201](analyzers/generator-diagnostics.md#internal-catalog-diagnostics).

## Supported property types

Any non-pointer, non-`void` CLR type is emitted as `Value<T>`; whether Umbraco can convert the
stored value to `T` is decided at runtime by the property value converters. There is no collection
or editor-specific mapping logic beyond that.
