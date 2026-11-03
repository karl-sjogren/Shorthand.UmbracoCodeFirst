# Source generator

`CodeFirstRegistryGenerator` (project `Shorthand.UmbracoCodeFirst.SourceGenerators`) is an
incremental generator. It runs over `partial` classes carrying `UCFContentType` or `UCFDataType` and
emits three kinds of source. Output is deterministic (definitions are ordered by fully qualified
name).

## Generated output

### Registry – `CodeFirstRegistry.g.cs`

`Shorthand.UmbracoCodeFirst.Generated.GeneratedCodeFirstDefinitionRegistry` implements
`ICodeFirstDefinitionRegistry` and exposes a `Definitions` list of `CodeFirstDefinition` records.
Each definition carries:

- Kind (`DataType` / `DocumentType`), alias, name
- Data Types: editor alias, editor UI alias, configuration JSON
- Document Types: `CodeFirstTypeSettings`, the property list (`CodeFirstPropertyDefinition` with
  alias, name, Data Type alias, description, group/tab alias+name+sort, sort order, mandatory,
  validation expression/message, label-on-top) and composition aliases (sorted, de-duplicated).

`CodeFirstDefinition` also computes `EffectiveTabs` and `EffectiveGroups` from the properties.

### Published models – `<Type>.PublishedModel.g.cs`

For every `UCFContentType` class a partial implementation is emitted in the class's own namespace:

- `[PublishedModel("alias")]` and `: PublishedContentModel`
- `ModelTypeAlias`, `ModelItemType` (`Content`, or `Element` if `IsElement`),
  `GetModelContentType`, `GetModelPropertyType<TValue>`
- a constructor `(IPublishedContent, IPublishedValueFallback)`
- one generated property per `UCFProperty`, implemented with
  `this.Value<T>(_publishedValueFallback, "alias")` and decorated with
  `[ImplementPropertyType("alias")]`

This follows the shape of Umbraco's ModelsBuilder output without depending on its internals. The
property declarations in your class are `partial` definitions; do not add your own implementation.

### Data Type alias – `<Type>.DataType.g.cs`

A `public const string Alias` on each Data Type class, so `UCFProperty` can reference
`MyDataType.Alias`. The generator resolves these references itself (including when the constant has
not been bound yet), so ordering of files does not matter.

## Alias derivation

When no alias is supplied: every non-letter/digit character starts a new word; the first word is
lower-case; each following word starts upper-case; all other letters are lower-cased.

| Name | Alias |
| ---- | ----- |
| `Body text` | `bodyText` |
| `Product Page` | `productPage` |
| `SEO` | `seo` |
| `ProductPage` | `productpage` |

Because letters inside a word are lower-cased, supply explicit aliases when you need mixed-case
(`"productPage"`).

## Composition resolution

`typeof(Target)` arguments of `UCFComposite` are resolved to the target's `UCFContentType` alias.
A target without `UCFContentType` falls back to a derived alias; analyzer UCF106 reports it.

## Generator diagnostics

| Id | Severity | Meaning |
| -- | -------- | ------- |
| UCF001 | Error | A `UCFProperty` has a CLR type that cannot be generated as a published-model property (pointer, function pointer or `void`). |

All other rules are reported by the [analyzers](analyzers/README.md).

## Supported property types

Any non-pointer, non-`void` CLR type is emitted as `Value<T>`; whether Umbraco can convert the
stored value to `T` is decided at runtime by the property value converters. There is no collection
or editor-specific mapping logic beyond that.
