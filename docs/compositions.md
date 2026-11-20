# Document and Element Type compositions

Umbraco Document Types can be composed of other Document Types, and Element Types of other Element
Types. C# inheritance cannot express a type composed of several others, so composition is declared
explicitly:

```csharp
[UCFDocument("seoComposition", "SEO composition")]
public partial class SeoComposition { /* properties */ }

[UCFDocument("productPage", "Product Page")]
[UCFComposite(typeof(SeoComposition))]
[UCFComposite(typeof(Article))]
public partial class ProductPage { /* properties */ }
```

`UCFComposite` is repeatable, class-targeted and not inherited. The target must have the same kind
as the consumer: `UCFDocument` for documents or `UCFElementType` for elements.

## Generated published models

For a composition target in the same compilation, the generator emits an interface named after the
target with an `I` prefix, such as `ISeoComposition`. Document interfaces extend `IPublishedContent`;
element interfaces extend `IPublishedElement`. Both the composite model and its consuming models
implement the interface. Nested compositions inherit their ancestor interfaces, and consumers expose
all composed properties, including their `PropertyAliases` constants.

The declaring composite also provides static getters, such as
`SeoComposition.GetTitle(ISeoComposition model, IPublishedValueFallback fallback)`. The consumer
properties delegate to these getters. Shared properties reached through multiple composition paths
are emitted once.

Generated contracts and properties are supported only when the composing and composed models are
in the same compilation. A referenced project's composite still contributes its alias to registry
metadata, but its generated interface and properties are not added to the consuming model.

## Compile-time validation

| Rule | Reports |
| ---- | ------- |
| [UCF106](analyzers/UCF106.md) | Target is not a document or element definition. |
| [UCF107](analyzers/UCF107.md) | Duplicate composition, a type composing itself, or a composition cycle. |
| [UCF110](analyzers/UCF110.md) | The composing and composed types have different kinds. |
| [UCF125](analyzers/generator-diagnostics.md#ucf125) | A generated composite member or interface conflicts with another member or type. |

## Metadata

The generator resolves each `typeof(...)` to the target's alias and stores the sorted,
de-duplicated list in `CodeFirstDefinition.CompositionAliases`.

## Synchronization behaviour

- **Ordering** – composed types are synchronized before the types that compose them.
- **Adding** – missing compositions are added with Umbraco's `AddContentType`. A composition alias
  that does not exist in Umbraco fails the composing type with a descriptive message.
- **Ownership** – each composition added by code-first is recorded in Umbraco's persisted
  key/value store rather than the composing type's editor-facing description.
- **Removing** – if a composition is removed from code, it is removed from Umbraco **only** when
  its ownership record exists. Compositions added manually in the backoffice are preserved.
- Compositions of non-owned Document Types are never changed.

The sample `Article` and `ProductPage` compose `SeoComposite` and `HappySparkles`; `CoolElement`
composes `CompositeElement`.
