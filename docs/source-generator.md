# Source generator

`CodeFirstRegistryGenerator` (project `Shorthand.UmbracoCodeFirst.SourceGenerators`) is an
incremental generator. It runs over `partial` code-first classes and marked root-level Razor views and
emits registry, model and alias sources. Output is deterministic (definitions are ordered by fully qualified
name).

## Generated output

### Registry – `CodeFirstRegistry.g.cs`

`Shorthand.UmbracoCodeFirst.Generated.GeneratedCodeFirstDefinitionRegistry` implements
`ICodeFirstDefinitionRegistry` and exposes a `Definitions` list of `CodeFirstDefinition` records.
Each definition carries:

- Kind (`DataType` / `DocumentType`), alias, name
- Templates: filename-derived alias, display name and a content hash for cluster coordination
- Data Types: editor alias, editor UI alias, configuration JSON
- Document Types: `CodeFirstTypeSettings`, the property list (`CodeFirstPropertyDefinition` with
  alias, name, Data Type alias, description, group/tab alias+name+sort, sort order, mandatory,
  validation expression/message, label-on-top) and composition aliases (sorted, de-duplicated).

`CodeFirstDefinition` also computes `EffectiveTabs` and `EffectiveGroups` from the properties.

### Published models – `<Type>.PublishedModel.g.cs`

For every `UCFDocument` class a partial implementation is emitted in the class's own namespace:

- `[PublishedModel("alias")]` and `: PublishedContentModel`
- `ModelTypeAlias`, `ModelItemType` (`Content`, or `Element` for `UCFElementType`),
  `GetModelContentType`, `GetModelPropertyType<TValue>`
- a constructor `(IPublishedContent, IPublishedValueFallback)`
- one generated property per `UCFProperty`, implemented with
  `this.Value<T>(_publishedValueFallback, "alias")` and decorated with
  `[ImplementPropertyType("alias")]`

This follows the shape of Umbraco's ModelsBuilder output without depending on its internals. The
property declarations in your class are `partial` definitions; do not add your own implementation.

### Property aliases

Code-first document, element, media and member types expose a generated `PropertyAliases` class.
For each `UCFProperty`, its constant is named after the CLR property and contains the same explicit
or display-name-derived alias emitted in the registry. For example, `Article.PropertyAliases.Title`
is a compile-time string constant; `CodeFirstHelper<Article>.Alias(x => x.Title)` returns the same
alias and supports normal property rename refactoring.

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

Marked root-level views generate `Shorthand.UmbracoCodeFirst.UmbracoTemplates` constants.
The identifier is the filename alias with its first character capitalized, and the value retains
the exact filename case. These constants can be used in document template settings in the same
project; see [UCFTemplate](attributes.md#ucftemplate--razor-view).

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

`typeof(Target)` arguments of `UCFComposite` are resolved to the target's `UCFDocument` alias.
A target without `UCFDocument` falls back to a derived alias; analyzer UCF106 reports it.

## Generator diagnostics

| Id | Severity | Meaning |
| -- | -------- | ------- |
| UCF001 | Error | A `UCFProperty` has a CLR type that cannot be generated as a published-model property (pointer, function pointer or `void`). |
| UCF116 | Error | A model declaration conflicts with the generated `PropertyAliases` class. |
| UCF117 | Error | A UCF attribute references a property alias generated in the same compilation. |
| UCF118 | Error | A media or member model's container prevents generation of its property aliases. |
| UCF120 | Error | A template directive or filename is invalid. |
| UCF121 | Error | Multiple marked views have the same template alias (case-insensitive). |
| UCF122 | Error | Multiple template aliases generate the same `UmbracoTemplates` constant. |

Other rules include those reported by the [analyzers](analyzers/README.md).

## Supported property types

Any non-pointer, non-`void` CLR type is emitted as `Value<T>`; whether Umbraco can convert the
stored value to `T` is decided at runtime by the property value converters. There is no collection
or editor-specific mapping logic beyond that.
