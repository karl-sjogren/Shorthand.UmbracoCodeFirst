# Attribute reference

All attributes live in the `Shorthand.UmbracoCodeFirst.Attributes` namespace. Constructor
arguments that appear in two forms exist with and without an explicit `alias`; when omitted the
alias is [derived from the name](source-generator.md#alias-derivation).

Attribute arguments must be compile-time constants, which is why editor and Data Type references are
strings (use the provided `Alias` constants).

## `UCFContentType` – class

Declares a Document Type. Only Document Types are supported; there is no media/member variant yet.

| Argument | Kind | Description |
| -------- | ---- | ----------- |
| `name` | ctor | Display name. |
| `alias` | ctor (optional) | Umbraco alias. |
| `Icon` | named | Icon alias, e.g. `icon-document`. |
| `IsElement` | named | Marks an Element Type (also changes the generated model's `ModelItemType`). |
| `AllowAsRoot` | named | Allowed at content root. |
| `AllowInLibrary` | named | Allowed in the library. |
| `DefaultTemplateAlias` | named | Metadata only – *not yet synchronized*. |
| `AllowedTemplateAliases` | named | Metadata only – *not yet synchronized*. |
| `AllowedChildAliases` | named | Metadata only – *not yet synchronized*. |
| `Variations` | named | Metadata only – *not yet synchronized*. |
| `IsListView` | named | Metadata only – *not yet synchronized*. |

Not inherited; one per class.

## `UCFDataType` – class

Declares a Data Type.

| Argument | Kind | Description |
| -------- | ---- | ----------- |
| `name` | ctor | Display name. |
| `alias` | ctor (optional) | Alias used by `UCFProperty`. |
| `editorAlias` | ctor | Property editor schema alias, e.g. `Umbraco.TextBox`. |
| `EditorUiAlias` | named | Editor UI alias, e.g. `Umb.PropertyEditorUi.TextBox`. Written independently of the schema alias. |
| `ConfigurationJson` | named | A JSON **object** merged into the Data Type's configuration (see below). |

The generator also emits `public const string Alias` on the class.

### Editor registry

`UmbracoEditors` provides `Alias`/`UiAlias` constants and an `UmbracoEditor` record `Definition`:

| Editor | Schema alias | UI alias |
| ------ | ------------ | -------- |
| `TextBox` | `Umbraco.TextBox` | `Umb.PropertyEditorUi.TextBox` |
| `TextArea` | `Umbraco.TextArea` | `Umb.PropertyEditorUi.TextArea` |
| `Integer` | `Umbraco.Integer` | `Umb.PropertyEditorUi.Integer` |
| `Decimal` | `Umbraco.Decimal` | `Umb.PropertyEditorUi.Decimal` |
| `DateTime` | `Umbraco.DateTime` | `Umb.PropertyEditorUi.DatePicker` |
| `Boolean` | `Umbraco.TrueFalse` | `Umb.PropertyEditorUi.Toggle` |
| `EmailAddress` | `Umbraco.EmailAddress` | `Umb.PropertyEditorUi.EmailAddress` |

Any other installed editor can be used by passing its alias string. The editor must be registered
in Umbraco or the Data Type fails with *unknown editor*.

### `ConfigurationJson`

Top-level keys of the JSON object are merged into the Data Type configuration data; nested objects
and arrays are converted to dictionaries/arrays. It must be a JSON object. There is currently no
typed per-editor settings model – this raw JSON is the only configuration path.

## `UCFProperty` – property

Declares a Document Type property. Must be on a `partial` property of a class that has
`UCFContentType`.

| Argument | Kind | Description |
| -------- | ---- | ----------- |
| `name` | ctor | Display name. |
| `alias` | ctor (optional) | Property alias. |
| `dataTypeAlias` | ctor | Alias of a code-first Data Type (use `MyDataType.Alias`). |
| `SortOrder` | named | Property sort order. |
| `Mandatory` | named | Whether a value is required. |
| `LabelOnTop` | named | Show the label above the editor. |

The referenced Data Type must be a code-first Data Type (see
[Synchronization](synchronization.md#data-types)); referencing an Umbraco built-in Data Type is not
currently supported.

## `UCFDescription` – class or property

`UCFDescription("text")` sets the Document Type or property description.

## `UCFTab` and `UCFGroup` – property

Place a property on a tab and/or group.

| Constructor | Meaning |
| ----------- | ------- |
| `(name)` | Alias derived from the name. |
| `(alias, name)` | Explicit alias. |
| `(alias, name, sortOrder)` | Explicit alias and sort order. |

`SortOrder` is also a named argument. Both attributes are inheritable and unsealed so they can be
subclassed (the generator resolves the base-constructor arguments of derived attributes).

If a property has both a tab and a group, the **group is the assignment target** in Umbraco (a
property type belongs to a single property group); the tab is still created.

## `UCFValidation` – property

`UCFValidation(expression, message)` sets the regular-expression validation and its message.
Unsealed, so a derived attribute can fix both values.

## `UCFComposite` – class

`UCFComposite(typeof(OtherContentType))` composes another code-first Document Type. Repeatable.
See [Compositions](compositions.md).

## Derived attributes

```csharp
public sealed class SeoTabAttribute() : UCFTabAttribute("seo", "SEO", 4);
public sealed class SkuValidationAttribute()
    : UCFValidationAttribute(@"^[A-Z0-9-]{4,32}$", "Use 4-32 uppercase letters, numbers, or hyphens.");
```
