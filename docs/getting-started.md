# Getting started

## Requirements

- .NET SDK 10.0.403 (`global.json`), `net10.0`
- Umbraco 18.3
- The consuming project references `Shorthand.UmbracoCodeFirst`, the source generator and the
  analyzers (in this repository the sample site does this with project references), and the
  `Shorthand.UmbracoCodeFirst.Backoffice` project for synchronization and the backoffice page.

Declaration classes must be `partial` and the code-first properties must be `partial` properties
(the generator supplies their implementation).

## 1. Declare a Data Type

A Data Type is a partial class with `UCFDataType`. Use the `UmbracoEditors` constants rather than
raw alias strings.

```csharp
using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.PropertyEditors;

[UCFDataType(
    "codeFirstDecimal",
    "Code First Decimal",
    UmbracoEditors.Decimal.Alias,
    EditorUiAlias = UmbracoEditors.Decimal.UiAlias,
    ConfigurationJson = """{"step":0.01}""")]
public partial class CodeFirstDecimalDataType;
```

The generator adds `public const string Alias = "codeFirstDecimal";` to the class, so properties can
reference it as `CodeFirstDecimalDataType.Alias`.

## 2. Declare a Document Type

```csharp
[UCFContentType("article", "Article", Icon = "icon-document", AllowAsRoot = true)]
[UCFDescription("An article managed by the code-first sample model.")]
public partial class Article {
    [UCFProperty("title", "Title", CodeFirstTextDataType.Alias,
        SortOrder = 0, Mandatory = true, LabelOnTop = true)]
    [UCFDescription("The article headline.")]
    [UCFTab("content", "Content")]
    public partial string Title { get; }

    [UCFProperty("Body text", CodeFirstLongTextDataType.Alias, SortOrder = 1)]
    [UCFTab("Content")]
    public partial string BodyText { get; }
}
```

Aliases are optional. When omitted they are derived from the name (`"Body text"` → `bodyText`); see
[naming rules](source-generator.md#alias-derivation).

## 3. Reuse layout and validation with derived attributes

`UCFTab`, `UCFGroup` and `UCFValidation` are not sealed. Derive from them once and reuse the result
instead of repeating arguments (analyzers UCF102/UCF103 suggest this):

```csharp
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class PricingTabAttribute() : UCFTabAttribute("pricing", "Pricing", 1);

[UCFProperty("price", "Price", CodeFirstDecimalDataType.Alias)]
[PricingTab]
public partial decimal Price { get; }
```

## 4. Compose Document Types

```csharp
[UCFContentType("productPage", "Product Page")]
[UCFComposite(typeof(Article))]
public partial class ProductPage { /* ... */ }
```

See [Compositions](compositions.md).

## 5. Run the site

On startup the synchronizer creates/updates the Data Types and Document Types. Open
**Settings > Advanced > Code First** in the backoffice to inspect the outcome of the last run.

The generated published model is the partial class itself, so after startup you can use it like a
ModelsBuilder model:

```csharp
Article article = new(publishedContent, publishedValueFallback);
string title = article.Title;
```

## Sample models

The sample site (`src/Shorthand.UmbracoCodeFirst.Umbraco/Models`) contains:

- Document Types: `Article`, `LandingPage`, `ProductPage` (50+ properties across product, pricing,
  inventory, marketing, SEO, support and lifecycle tabs; composes `Article`).
- Data Types: text, long text, email, integer, decimal, date and boolean.
- `ProductPageGroupAttributes.cs` – the derived tab/group attributes used above.
- `MediaTypes/ImageAsset.cs` and `MemberTypes/MemberProfile.cs` – empty placeholders; they have no
  attribute and produce nothing yet.
