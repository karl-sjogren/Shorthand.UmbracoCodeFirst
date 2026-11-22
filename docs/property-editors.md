# Property editor metadata

The built-in Umbraco property-editor catalog is maintained in
[`umbraco-property-editors.json`](../src/Shorthand.UmbracoCodeFirst.Core/PropertyEditors/umbraco-property-editors.json).
It is the single metadata source for:

- generated `UmbracoEditors` aliases, UI aliases, descriptions, return types, and documentation links;
- generated typed `UmbracoEditors.<Editor>.Configuration` base classes for editors whose settings
  are described in the catalog;
- analyzer validation of Data Type editor aliases;
- analyzer checks that a code-first property's CLR type is compatible with the editor's documented
  return type.

The source generator consumes the file as an `AdditionalFiles` input and emits
`UmbracoEditors.g.cs`. Do not edit the generated file. Add or update an entry in the JSON catalog,
then rebuild.

Each entry has a stable `id`, display `name`, `description`, schema `alias`, `uiAlias`,
documented `returnType`, `documentationUrl`, and a `configuration.properties` array. The array is
empty for editors with no documented Data Type options. Supported configuration property types are
`string`, `bool`, `int`, `decimal`, `string[]`, `stringCollection`, `int[]`, `decimal[]`, and `json`. The `json` type is
intentional for editor-owned arrays and objects such as picker roots, crop presets, block definitions,
and rich-text toolbar settings; it preserves the editor's native shape while keeping the generated
configuration API discoverable. Return types are descriptors rather than
assembly-qualified CLR names so the netstandard source generator and analyzer can consume them
without referencing Umbraco runtime assemblies.

Each configuration property also requires a non-empty `comment`, emitted as the generated
property's XML documentation summary. Comments are plain text; XML-sensitive characters are escaped.

Every catalog entry generates a `Configuration` base class with nullable setters, including entries
whose configuration array is empty. A consuming project derives from that class, sets the desired
values in a constructor, and passes the derived type through `UCFDataType.ConfigurationType`. This
keeps editor options discoverable while allowing the catalog to remain the single source of truth.

Compatibility diagnostics are conservative. Scalar and string collection editors are checked
directly; nullable values are accepted. Media Picker checks the supported media shapes and, when
statically known, selection multiplicity. Other Umbraco return types are treated as compatible when
the catalog cannot safely resolve the consuming type.

The catalog currently follows the [Umbraco built-in property editor
documentation](https://docs.umbraco.com/umbraco-cms/model-your-content/property-editors/built-in-umbraco-property-editors).
When an editor has confusing historical aliases, its stable catalog ID and display name preserve
the documented distinction.

For Umbraco 18.3.1, Code Editor uses the `Umbraco.Plain.String` schema with the
`Umb.PropertyEditorUi.CodeEditor` UI. `CodeEditor.Configuration.Height` is an integer number of
pixels; configurations using the previous string property must change, for example, `"600"` to
`600`. References to the previous `Umbraco.CodeEditor` schema alias should use
`Umbraco.Plain.String` instead.

Umbraco 18.3.1 offers these Code Editor languages: `csharp`, `css`, `html`, `javascript`,
`json`, `markdown`, `razor`, and `typescript`. Use the constants in
`Shorthand.UmbracoCodeFirst.PropertyEditors.UmbracoCodeEditorLanguages`:

```csharp
public sealed class CSharpEditorConfiguration : UmbracoEditors.CodeEditor.Configuration {
    public CSharpEditorConfiguration() {
        Language = UmbracoCodeEditorLanguages.CSharp;
    }
}
```

`Language` remains a string so custom values can still be supplied. When it is unspecified,
Umbraco's editor defaults to JavaScript. Other Monaco language IDs are not necessarily exposed
or supported by Umbraco's Code Editor configuration.

## Media Picker

Media Picker supports typed configuration without referencing Umbraco configuration classes:

```csharp
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.PropertyEditors;

public sealed class ImagePickerConfiguration : UmbracoEditors.MediaPicker.Configuration {
    public ImagePickerConfiguration() {
        AcceptedMediaTypeKeys = [global::Umbraco.Cms.Core.Constants.MediaTypes.Guids.ImageGuid];
        Multiple = true;
        ValidationLimit = new(Min: 0, Max: 3);
        StartNodeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        EnableLocalFocalPoint = true;
        Crops = [new("thumbnail", 400, 300)];
        IgnoreUserStartNodes = false;
    }
}
```

Use the configuration type in `UCFDataType`, or reference an existing picker by `DataTypeKey` as
shown in [attributes](attributes.md#ucfproperty--property). `AcceptedMediaTypeKeys` contains media
type keys; it serializes to Umbraco's comma-separated `filter` value. `StartNodeId` is the media node
key. Enabling `IgnoreUserStartNodes` overrides
the user's normal media start-node restrictions for this picker; use it deliberately.

Single selection returns `MediaWithCrops`; multiple selection returns
`IEnumerable<MediaWithCrops>`. Umbraco also documents corresponding `IPublishedContent` shapes.
Even with a maximum of one, multiple selection returns a collection. UCF109 accepts either shape
when multiplicity cannot be established statically, including externally managed built-in pickers.

This replaces the former Media Picker `MinNumber`, `MaxNumber`, raw JSON `Crops` and `StartNode`
members. Use `ValidationLimit`, typed `Crops` and `StartNodeId` instead. Raw
`UCFDataType.ConfigurationJson` remains available.

## Element Picker

`UmbracoEditors.ElementPicker.Configuration` accepts `MinNumber`, `MaxNumber`, and `Filter`.
Set `Filter` to a collection of code-first element aliases or Umbraco element type keys.
Aliases are resolved to synchronized, code-first-owned element type keys. For example:

```csharp
public sealed class SiteSettingsPickerConfiguration : UmbracoEditors.ElementPicker.Configuration {
    public SiteSettingsPickerConfiguration() {
        MaxNumber = 1;
        Filter = [SiteSettings.ModelTypeAlias];
    }
}
```

The picker selects existing elements from Umbraco's element library; the element type must allow
library use, and an element must exist there before it can be selected. The property value is a
collection even when the maximum is one.
Existing `Filter` assignments using a comma-separated string must be changed to a collection;
legacy string values in `CodeFirstEditorConfiguration` remain supported.

## Block editors

The Block List, Block Grid, Single Block and Rich Text/Tiptap configuration classes support typed
block definitions for Umbraco 18.3.1. Reference a code-first element by its CLR model type or its
generated alias. Content and settings references must resolve to owned code-first element types.

```csharp
using Shorthand.UmbracoCodeFirst.Attributes;
using Shorthand.UmbracoCodeFirst.Metadata;
using Shorthand.UmbracoCodeFirst.PropertyEditors;

[UCFElementType("textBlock", "Text block")]
public partial class TextBlock;

[UCFElementType("blockSettings", "Block settings")]
public partial class BlockSettings;

[UCFDataType("Article blocks", typeof(UmbracoEditors.BlockList), typeof(ArticleBlocksConfiguration))]
public partial class ArticleBlocks;

public sealed class ArticleBlocksConfiguration : UmbracoEditors.BlockList.Configuration {
    public ArticleBlocksConfiguration() {
        BlockTypes = [
            new(typeof(TextBlock)) {
                Settings = new("blockSettings"),
                Label = "Text",
                IconColor = "#123456",
                BackgroundColor = "#eeeeee",
                EditorSize = "medium",
            },
        ];
        ValidationRange = new() { Min = 0, Max = 10 };
        UseLiveEditing = true;
        CreateModalSize = "medium";
    }
}
```

Block Grid additionally supports groups, placement, column spans, and named areas with element/group
allowances:

```csharp
public sealed class ArticleGridConfiguration : UmbracoEditors.BlockGrid.Configuration {
    public ArticleGridConfiguration() {
        GridColumns = 12;
        Groups = [new("content", "Content")];
        BlockTypes = [
            new(typeof(TextBlock)) {
                GroupAlias = "content",
                AllowAtRoot = true,
                AllowInAreas = true,
                ColumnSpans = [6, 12],
                Areas = [
                    new("main") {
                        ColumnSpan = 6,
                        SpecifiedAllowance = [
                            new() { Element = new(typeof(TextBlock)), MaxAllowed = 3 },
                            new() { GroupAlias = "content" },
                        ],
                    },
                ],
            },
        ];
    }
}
```

The separate Single Block editor and Tiptap use the same `BlockTypes` model:

```csharp
public sealed class ArticleSingleBlockConfiguration : UmbracoEditors.SingleBlock.Configuration {
    public ArticleSingleBlockConfiguration() {
        BlockTypes = [new(typeof(TextBlock))];
        UseInlineEditingAsDefault = true;
    }
}

public sealed class ArticleRichTextConfiguration : UmbracoEditors.RichTextEditor.Configuration {
    public ArticleRichTextConfiguration() {
        BlockTypes = [new(typeof(TextBlock)) { DisplayInline = true }];
        Extensions?.Add(TiptapExtension.WordCount);
        Extensions?.Add("My.Tiptap.Extension");
        ToolbarRows?[0][1].Remove(TiptapToolbar.Underline);
        ToolbarRows?[0][1].Add("My.Tiptap.Toolbar.Button");
        StatusbarGroups = [[TiptapStatusbar.WordCount, "My.Tiptap.Statusbar.Item"]];
        EditorDimensions = new() { Height = 400 };
        StylesheetPaths = ["/editor.css"];
        MaxImageSize = 1000;
        OverlaySize = "medium";
    }
}
```

Tiptap's configuration class starts with Umbraco's default toolbar, extensions, image size, and
overlay size. `TiptapToolbar`, `TiptapExtension`, and `TiptapStatusbar` in
`Shorthand.UmbracoCodeFirst.PropertyEditors` provide the built-in aliases from Umbraco 18.3.2;
custom or version-specific aliases remain ordinary strings. Some toolbar and statusbar items
require specific extensions: for example, `TiptapStatusbar.WordCount` requires
`TiptapExtension.WordCount`, which is not enabled by default.

Each instance has its own mutable `Extensions` and nested `ToolbarRows` lists. Both the outer
`StatusbarGroups` list and its groups are mutable. Existing assignments of read-only statusbar
collections must be converted to `List<List<string>>`. Add or remove both the toolbar action and
its required extensions as needed. Set a collection to `null` to leave it
unspecified, or clear it to explicitly use an empty list. When using raw `Toolbar` JSON, set
`ToolbarRows = null` to avoid specifying the toolbar twice.

Other supported options are exposed by the generated configuration classes and
`CodeFirstBlockType`, `CodeFirstBlockArea`, and `CodeFirstBlockAreaPermission`. The deprecated
Block List single-block mode is not included; use Single Block instead.

Existing `Blocks`, `BlockGroups`, `ValidationLimit`, `Toolbar`, `Statusbar`, `Dimensions`,
`Stylesheets` and `ConfigurationJson` raw JSON remain supported. Do not set both a raw setting
and its typed counterpart: synchronization reports a failure. `null` leaves a typed collection
unspecified; an empty `BlockTypes` or `Groups` collection explicitly clears it. Unspecified and
unknown settings are preserved.

Grid group keys are derived from the Data Type and group aliases; area keys additionally use the
content element and area aliases. Reordering does not change them, but renaming an alias does.
Set the area's or group's `Key` explicitly to preserve its identity across a rename or to match an
existing manually configured key.
