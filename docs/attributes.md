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

## `UCFDocument` – class

Declares a Document Type. Media and Member Types use `UCFMedia` and `UCFMember`.

| Argument | Kind | Description |
| -------- | ---- | ----------- |
| `name` | ctor | Display name. |
| `alias` | ctor (optional) | Umbraco alias. |
| `Icon` | named | Icon alias, e.g. `icon-document`. |
| `AllowAsRoot` | named | Allowed at content root. |
| `DefaultTemplateAlias` | named | Default template alias; must be included in `AllowedTemplateAliases` ([UCF119](analyzers/UCF119.md)). |
| `AllowedTemplateAliases` | named | Aliases of templates allowed for this document type. |
| `AllowedChildAliases` | named | Metadata only – *not yet synchronized*. |
| `Variations` | named | Metadata only – *not yet synchronized*. |

Not inherited; one per class.

## `UCFTemplate` – Razor view

Mark a view such as `Views/landingPage.cshtml` or `Views/Features/Blog/Default.cshtml`:

```cshtml
@attribute [Shorthand.UmbracoCodeFirst.Attributes.UCFTemplate("Landing Page")]
@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage
<h1>Landing page</h1>
```

The required display name must be a non-empty C# string literal, and the directive must be on
one line. The filename stem is the template alias (`landingPage` here); use the exact same
case when referencing it. Every relative path segment must start with an ASCII letter and contain only ASCII
letters, digits or underscores, and the complete alias is limited to 100 characters.
Nested views are registered using a flattened underscore alias such as
`Features_Blog_Default`, while retaining `Features/Blog/Default.cshtml` as the physical view path.

Template registration does not automatically associate a document type. Configure both settings:

```csharp
[UCFDocument("landingPage", "Landing Page",
    DefaultTemplateAlias = UmbracoTemplates.LandingPage,
    AllowedTemplateAliases = new[] { UmbracoTemplates.LandingPage })]
public partial class LandingPage;
```

`UmbracoTemplates` is generated in `Shorthand.UmbracoCodeFirst` from marked views in the
configured views directory by default. Root view constants capitalize the first character of
the filename stem and preserve the remaining characters. Nested views use hierarchical static
classes: `Features/Blog/Default.cshtml` exposes
`UmbracoTemplates.Features.Blog.Default`. Constant values are the exact slash-separated aliases.
An root alias named `umbracoTemplates` uses the constant `TemplateUmbracoTemplates`.
Identifier and hierarchy collisions report UCF122.

To keep code-first models in a project referenced by the Umbraco site, install/reference both
`Shorthand.UmbracoCodeFirst` and `Shorthand.UmbracoCodeFirst.SourceGenerators` in the models
project, and set its Views directory:

```xml
<PropertyGroup>
  <UmbracoCodeFirstViewsDirectory>..\MySite\Views</UmbracoCodeFirstViewsDirectory>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Shorthand.UmbracoCodeFirst" />
  <PackageReference Include="Shorthand.UmbracoCodeFirst.SourceGenerators" />
</ItemGroup>
```

Specify package versions through your normal NuGet configuration.
The path is relative to the models project, or may be absolute. It must exist; an empty directory
is allowed. The default local `Views` directory may be absent. All `.cshtml` files below the
directory are considered recursively.

Reference the models project from the site and disable template generation in the site:

```xml
<PropertyGroup>
  <UmbracoCodeFirstGenerateTemplates>false</UmbracoCodeFirstGenerateTemplates>
</PropertyGroup>
<ItemGroup>
  <ProjectReference Include="..\MyModels\MyModels.csproj" />
</ItemGroup>
```

Keep all code-first definitions in the models project. The runtime selects one registry, rather
than merging registries across projects; UCF124 reports a competing registry in this arrangement.
The package supplies the build targets automatically. When using project references to the
package source rather than NuGet, replace the package references with project references and
import the build targets in each consuming project, adjusting paths for your solution:

```xml
<Import Project="..\Shorthand.UmbracoCodeFirst\buildTransitive\Shorthand.UmbracoCodeFirst.targets" />
<ItemGroup>
  <ProjectReference Include="..\Shorthand.UmbracoCodeFirst\Shorthand.UmbracoCodeFirst.csproj" />
  <ProjectReference Include="..\Shorthand.UmbracoCodeFirst.SourceGenerators\Shorthand.UmbracoCodeFirst.SourceGenerators.csproj"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false" PrivateAssets="all" />
</ItemGroup>
```

The package build targets automatically include all configured `.cshtml` files as content and
preserve their relative paths below `Views` for build and publish output. A compiled view alone
is not sufficient for Umbraco template registration. See [Synchronization](synchronization.md#templates)
for ownership and file behavior.

## `UCFCollection` – class

Attach a collection to a code-first Document or Media Type using a concrete
`CodeFirstCollectionConfiguration` with a public parameterless constructor:

```csharp
using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Metadata;

[UCFDocument("Articles")]
[UCFCollection(typeof(ArticleCollection))]
public partial class Articles;

[UCFDocument("Article")]
public partial class Article {
    [UCFProperty("articleSummary", "Summary", "text")]
    public partial string Summary { get; }
}

public sealed class ArticleCollection : CodeFirstCollectionConfiguration<Article> {
    public ArticleCollection() {
        List = true;
        Grid = false;
        PageSize = 25;
        OrderBy = Alias(article => article.Summary);
        OrderDirection = "desc";
        Columns = [
            new(CodeFirstCollectionSystemFields.UpdateDate, "Last edited"),
            new(Article.PropertyAliases.Summary, "Summary", NameTemplate: "{=value}"),
        ];
    }
}
```

`CodeFirstCollectionConfiguration<T>` is an optional subclass of the non-generic base. Its
`Alias` method delegates to `CodeFirstHelper<T>.Alias`, so both forms can be used in a
configuration. Both typed references above resolve to `articleSummary`, the explicit Umbraco alias;
`nameof(Article.Summary)` would instead produce `Summary`. The expression helper supports direct
`UCFProperty` selections, including CLR-inherited properties, and property rename refactoring.
Generated `PropertyAliases` constants can be used where a compile-time constant is required; a
property rename makes stale constant references fail to compile. Use the declaring model for a
composed property if the composition is not CLR inheritance.

```csharp
[UCFMedia("Media folders")]
[UCFCollection(typeof(MediaCollection))]
public partial class MediaFolders;

[UCFMedia("Image assets")]
public partial class ImageAsset {
    [UCFProperty("Caption", "text")]
    public string? Caption { get; set; }
}

public sealed class MediaCollection : CodeFirstCollectionConfiguration<ImageAsset> {
    public MediaCollection() {
        OrderBy = ImageAsset.PropertyAliases.Caption;
        Columns = [new(Alias(image => image.Caption), "Caption")];
    }
}
```

The same configuration may be reused across types; Umbraco's matching document or media layout
aliases are selected automatically. Defaults enable both layouts, display 10 items per page, sort
by `sortOrder` descending, and include Sort, Last edited and Created by columns.
`ListViewAlias` optionally selects a registered custom collection view instead of the built-in
table view. The view must be available for the relevant document or media collection.
`OrderDirection` is `asc` or `desc`; the order field must be `name` or a displayed column.
At least one layout and one column must be configured.

`CodeFirstCollectionSystemFields` provides constants for the built-in fields offered by Umbraco;
`Published` is document-only. A column's `IsSystem` is determined automatically by an exact,
case-sensitive match against those aliases. Custom properties using the same alias are treated as
system fields. A field being available as a column does not guarantee it can be used for sorting.
Strings remain supported for manual properties or other fields.

The expression helper does not execute the selector. It rejects nested properties, methods, fields,
static properties, and properties without generated code-first aliases. Configuration errors are
reported in synchronization status. It uses reflection; trimming support is not guaranteed. A
generated alias constant used as an argument to a UCF attribute in the same compilation is not
available to the generator in its initial pass: UCF117 reports this case. Use a string constant or
reference a previously compiled model instead.

Each parent gets its own owned Data Type, even if configurations are shared. Existing manual
collections are not replaced; synchronization reports a conflict. Removing the attribute detaches
only the matching code-first collection and leaves the Data Type intact. The old `IsListView`
attribute property has been removed; replace it with `UCFCollection` to enable a collection.
Custom layouts and workspace icon/name/content-first options are not supported.

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
`UCFDocument`.

| Argument | Kind | Description |
| -------- | ---- | ----------- |
| `name` | ctor | Display name. |
| `alias` | ctor (optional) | Property alias. |
| `dataTypeAlias` | ctor (optional) | Alias of a code-first Data Type (use `MyDataType.Alias`). |
| `DataTypeKey` | named (optional) | GUID string identifying an existing Umbraco Data Type. |
| `Alias` | named (optional) | Explicit property alias, useful with `DataTypeKey`. |
| `SortOrder` | named | Property sort order. |
| `Mandatory` | named | Whether a value is required. |
| `LabelOnTop` | named | Show the label above the editor. |

Supply exactly one code-first alias or existing Data Type key. For example:

```csharp
[UCFProperty("Image", DataTypeKey = global::Umbraco.Cms.Core.Constants.DataTypes.Guids.MediaPicker3SingleImage)]
public partial MediaWithCrops? Image { get; }
```

The key can reference a built-in or manually created Data Type. Code First does not own, configure,
or delete it. A missing key fails synchronization; changing the externally managed configuration
can change the property's runtime value shape.

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

If a property has both attributes, the group is placed inside the tab:

```csharp
[UCFTab("Content")]
[UCFGroup("Details")]
[UCFProperty("Heading", "text")]
public partial string Heading { get; }
```

Group aliases are scoped to their parent tab. The same group alias on another tab, or on a
property without a tab, identifies a separate group. Declarations for the same group within
one parent must agree on its name and sort order. Synchronization reports a conflict rather
than changing the type of an existing group containing manual properties or nested groups.

## `UCFValidation` – property

`UCFValidation(expression, message)` sets the regular-expression validation and its message.
Unsealed, so a derived attribute can fix both values.

## `UCFComposite` – class

`UCFComposite(typeof(OtherType))` composes another code-first Document Type or Element Type of the
same kind. Repeatable.
See [Compositions](compositions.md).

## Derived attributes

```csharp
public sealed class SeoTabAttribute() : UCFTabAttribute("seo", "SEO", 4);
public sealed class SkuValidationAttribute()
    : UCFValidationAttribute(@"^[A-Z0-9-]{4,32}$", "Use 4-32 uppercase letters, numbers, or hyphens.");
```
