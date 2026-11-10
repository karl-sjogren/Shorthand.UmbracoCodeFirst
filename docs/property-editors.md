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
`string`, `bool`, `int`, `decimal`, `string[]`, `int[]`, `decimal[]`, and `json`. The `json` type is
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
directly; nullable values are accepted; picker/block/media return types are treated as compatible
when the catalog cannot safely resolve the consuming Umbraco type. Configuration-dependent editors
should use a broad documented return descriptor rather than causing false positives.

The catalog currently follows the [Umbraco built-in property editor
documentation](https://docs.umbraco.com/umbraco-cms/model-your-content/property-editors/built-in-umbraco-property-editors).
When an editor has confusing historical aliases, its stable catalog ID and display name preserve
the documented distinction.

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
        ToolbarRows = [[["Umb.Tiptap.Toolbar.Bold", "Umb.Tiptap.Toolbar.Italic"]]];
        StatusbarGroups = [["Umb.Tiptap.Statusbar.WordCount"]];
        EditorDimensions = new() { Height = 400 };
        StylesheetPaths = ["/editor.css"];
        Extensions = ["Umb.Tiptap.RichTextEssentials"];
        MaxImageSize = 1000;
        OverlaySize = "medium";
    }
}
```

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
