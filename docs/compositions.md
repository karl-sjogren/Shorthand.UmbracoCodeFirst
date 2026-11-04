# Document Type compositions

Umbraco Document Types can be composed of other Document Types. C# inheritance cannot express a type
composed of several others, so composition is declared explicitly:

```csharp
[UCFDocumentType("seoComposition", "SEO composition")]
public partial class SeoComposition { /* properties */ }

[UCFDocumentType("productPage", "Product Page")]
[UCFComposite(typeof(SeoComposition))]
[UCFComposite(typeof(Article))]
public partial class ProductPage { /* properties */ }
```

`UCFComposite` is repeatable, class-targeted and not inherited. The target must itself be a
`UCFDocumentType`.

## Compile-time validation

| Rule | Reports |
| ---- | ------- |
| [UCF106](analyzers/UCF106.md) | Target is not a `UCFDocumentType`. |
| [UCF107](analyzers/UCF107.md) | Duplicate composition, a type composing itself, or a composition cycle. |

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

The sample `ProductPage` composes `Article`, and the integration tests assert that `productPage`
contains the `article` composition.
