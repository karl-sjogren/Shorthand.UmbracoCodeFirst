# Attribute reference

All attributes live in the `Shorthand.UmbracoCodeFirst.Attributes` namespace. Constructor
arguments that appear in two forms exist with and without an explicit `alias`; when omitted the
alias is [derived from the name](source-generator.md#alias-derivation).

Attribute arguments must be compile-time constants, which is why editor and Data Type references are
strings (use the provided `Alias` constants).

## `UCFFolderPath` – class

Places the generated definition in a type-specific Umbraco folder. The path uses `/` separators;
missing, empty, or `/` paths keep the definition in the root. Missing folders are created during
synchronization.

| Argument | Kind | Description |
| -------- | ---- | ----------- |
| `path` | ctor | Folder path relative to the type root, e.g. `Elements/SEO`. |

Not inherited; one per class.

## `UCFDocumentType` – class

Declares a Document Type. Only Document Types are supported; there is no media/member variant yet.

| Argument | Kind | Description |
| -------- | ---- | ----------- |
| `name` | ctor | Display name. |
| `alias` | ctor (optional) | Umbraco alias. |
| `Icon` | named | Icon alias, e.g. `icon-document`. |
| `AllowAsRoot` | named | Allowed at content root. |
| `DefaultTemplateAlias` | named | Metadata only – *not yet synchronized*. |
| `AllowedTemplateAliases` | named | Metadata only – *not yet synchronized*. |
| `AllowedChildAliases` | named | Metadata only – *not yet synchronized*. |
| `Variations` | named | Metadata only – *not yet synchronized*. |
| `IsListView` | named | Metadata only – *not yet synchronized*. |

Not inherited; one per class.

## `UCFElementType` – class

Declares an Element Type. Element-only settings are kept separate from Document Types so
templates, root permissions, allowed child types, and collection/list-view settings cannot be
configured accidentally.

| Argument | Kind | Description |
| -------- | ---- | ----------- |
| `name` | ctor | Display name. |
| `alias` | ctor (optional) | Umbraco alias. |
| `Icon` | named | Icon alias, e.g. `icon-document`. |
| `AllowInLibrary` | named | Allowed in the library. |
| `Variations` | named | Metadata only – *not yet synchronized*. |

Not inherited; one per class.

## `UCFDataType` – class

Declares a Data Type.

| Argument | Kind | Description |
| -------- | ---- | ----------- |
| `name` | ctor | Display name. |
| `editorAlias` | ctor | Property editor schema alias, e.g. `Umbraco.TextBox`. |
| `editorUiAlias` | ctor | Editor UI alias, e.g. `Umb.PropertyEditorUi.TextBox`. Written independently of the schema alias. |
| `editor` | ctor overload | A type from `UmbracoEditors`; its `Alias` and `UiAlias` constants are read automatically. |
| `configurationType` | ctor overload or named | A static configuration type exposing typed public constants; their names become camel-case JSON settings. A raw `Json` constant remains supported. |
| `Alias` | named (optional) | Internal code-first alias used by `UCFProperty`; derived from `name` when omitted. |
| `ConfigurationJson` | named | A JSON **object** merged into the Data Type's configuration (see below). This remains the raw escape hatch. |

The generator also emits `public const string Alias` on the class. This is a code-first identity;
Umbraco's `IDataType` does not expose a persisted alias property.

When using the generated editor registry, prefer the type overload:

```csharp
[UCFDataType("Code First Text", typeof(UmbracoEditors.TextBox))]
public partial class CodeFirstTextDataType;
```

### Editor registry

`UmbracoEditors` is generated from the repository's
[property-editor metadata catalog](property-editors.md). It provides `Alias`/`UiAlias` constants
and an `UmbracoEditor` record `Definition` containing the documented name, description, return
type, and documentation URL:

| Editor | Schema alias | UI alias |
| ------ | ------------ | -------- |
| `TextBox` | `Umbraco.TextBox` | `Umb.PropertyEditorUi.TextBox` |
| `TextArea` | `Umbraco.TextArea` | `Umb.PropertyEditorUi.TextArea` |
| `Integer` | `Umbraco.Integer` | `Umb.PropertyEditorUi.Integer` |
| `Decimal` | `Umbraco.Decimal` | `Umb.PropertyEditorUi.Decimal` |
| `DateTime` | `Umbraco.DateTime` | `Umb.PropertyEditorUi.DatePicker` |
| `Boolean` | `Umbraco.TrueFalse` | `Umb.PropertyEditorUi.Toggle` |
| `EmailAddress` | `Umbraco.EmailAddress` | `Umb.PropertyEditorUi.EmailAddress` |

The analyzer also validates that a referenced built-in editor is in the catalog and that the CLR
property type is compatible with its documented return type. Any other installed editor can be
used by passing its alias string, but it will require a catalog entry to participate in validation.

### `ConfigurationJson`

Top-level keys of the JSON object are merged into the Data Type configuration data; nested objects
and arrays are converted to dictionaries/arrays. It must be a JSON object.

For reusable typed declarations, inherit the generated configuration base for the editor. The base
exposes the known editor options through IntelliSense:

```csharp
public sealed class TextBoxConfiguration :
    UmbracoEditors.TextBox.Configuration {
    public TextBoxConfiguration() {
        MaxChars = 512;
    }
}

[UCFDataType("Configured Text", typeof(UmbracoEditors.TextBox), typeof(TextBoxConfiguration))]
public partial class ConfiguredTextDataType;
```

`ConfigurationType` takes precedence over `ConfigurationJson` when both are supplied. Generated
editor configuration bases are instantiated and serialized using camel-case names
(`MaxChars` becomes `maxChars`). A public constant string `Json` remains supported for legacy or
complex configurations. `EditorId` defaults to the Data Type editor alias and `SchemaVersion`
defaults to `1`. Unsupported typed fields report `UCF112`.

## `UCFProperty` – property

Declares a Document Type property. Must be on a `partial` property of a class that has
`UCFDocumentType`.

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

`UCFComposite(typeof(OtherDocumentType))` composes another code-first Document Type. Repeatable.
See [Compositions](compositions.md).

## Derived attributes

```csharp
public sealed class SeoTabAttribute() : UCFTabAttribute("seo", "SEO", 4);
public sealed class SkuValidationAttribute()
    : UCFValidationAttribute(@"^[A-Z0-9-]{4,32}$", "Use 4-32 uppercase letters, numbers, or hyphens.");
```
