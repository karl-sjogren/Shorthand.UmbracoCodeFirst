# Generator diagnostics

These are errors reported by the consumer source generator. The examples are independent;
assume the relevant `Shorthand.UmbracoCodeFirst.Attributes`, `Metadata` and
`PropertyEditors` namespaces are imported. Model properties shown on document types are
partial declarations implemented by the generator.

## UCF001

A published-model property cannot have a pointer, function-pointer or `void` type. With unsafe
compilation enabled, this is invalid:

```csharp
[UCFDocument("Article")]
public unsafe partial class Article {
    [UCFProperty("Count", "number")]
    public partial int* Count { get; }
}
```

Use a supported CLR type, for example `public partial int Count { get; }`. UCF104 is the
corresponding analyzer warning; editor compatibility is checked separately by UCF109.

## UCF111

An untyped Data Type configuration must provide a public constant `Json` string or at least one
supported public constant setting.

```csharp
public class Settings { } // UCF111
[UCFDataType("Title", typeof(UmbracoEditors.TextBox), typeof(Settings))]
public partial class TitleDataType;
```

For example, replace the empty class with:

```csharp
public class Settings { public const string Json = "{}"; }
```

## UCF112

Only strings, booleans and numeric constants are serialized as individual settings. An enum
constant is not supported:

```csharp
public enum Mode { Normal }
public class Settings { public const Mode ModeSetting = Mode.Normal; } // UCF112
```

Use a supported representation, such as `public const string ModeSetting = "normal";`,
or supply a `Json` string. Use `Settings` as the configuration type in the UCF111 example.

## UCF113

Generated `ModelTypeAlias` references are resolved in allowed-child arrays, but arbitrary
expressions involving them are not:

```csharp
[UCFDocument("Article")]
public partial class Article;
[UCFDocument("Landing", AllowedChildAliases = new[] { Article.ModelTypeAlias + "child" })] // UCF113
public partial class Landing;
```

Use a string constant or an unmodified generated model alias:

```csharp
[UCFDocument("Landing", AllowedChildAliases = new[] { Article.ModelTypeAlias })]
public partial class Landing;
```

## UCF114

A collection is supported on a document or media type and requires a concrete configuration
with a public parameterless constructor. UCF115 checks the configuration type as well.

```csharp
public class Settings { } // Not a collection configuration
[UCFDocument("Index"), UCFCollection(typeof(Settings))] // UCF114
public partial class Index;
```

Replace `Settings` with:

```csharp
public class Settings : CodeFirstCollectionConfiguration { }
```

## UCF116

The generator owns the `PropertyAliases` member when a model has properties:

```csharp
[UCFMedia("Image")]
public partial class Image {
    public static class PropertyAliases { } // UCF116
    [UCFProperty("Caption", "textstring")] public string? Caption { get; set; }
}
```

Remove or rename the hand-written `PropertyAliases` class so the generator can provide it.

## UCF117

Generated property aliases cannot be consumed by UCF attributes in the same compilation:

```csharp
[UCFMedia("Image")]
public partial class Image {
    [UCFProperty("Caption", "textstring")] public string? Caption { get; set; }
}
[UCFMedia(Image.PropertyAliases.Caption)] // UCF117
public partial class Other;
```

Use `[UCFMedia("caption")]`, a handwritten constant, or a generated constant from an already
compiled assembly. Normal C# references outside UCF attributes can use the generated constant.

## UCF118

Media and member models with generated property aliases cannot be file-local, and all their
containing types must be partial:

```csharp
public class Container { // UCF118
    [UCFMedia("Image")]
    public partial class Image {
        [UCFProperty("Caption", "textstring")] public string? Caption { get; set; }
    }
}
```

Change the container to `public partial class Container` and do not use `file` on these types.

## UCF120

A marked root-level view needs one non-empty string literal and a valid filename:

```razor
@* Views/article.cshtml: UCF120 *@
@attribute [UCFTemplate("")]
```

```razor
@* Views/article.cshtml *@
@attribute [UCFTemplate("Article")]
```

Filenames must start with an ASCII letter, contain only letters, digits or underscores and be at
most 100 characters long.

## UCF121

Marked root-level views cannot have aliases differing only by case. On a case-sensitive filesystem:

```text
Views/article.cshtml   @attribute [UCFTemplate("Article")]
Views/Article.cshtml   @attribute [UCFTemplate("Other")]
```

Rename the second file to `Views/other.cshtml` so the filename-derived aliases are distinct.

## UCF122

Distinct template aliases can still produce the same C# constant:

```text
Views/umbracoTemplates.cshtml          @attribute [UCFTemplate("One")]
Views/templateUmbracoTemplates.cshtml  @attribute [UCFTemplate("Two")]
```

Rename the second file to `Views/other.cshtml`. Both filenames above otherwise generate
`UmbracoTemplates.TemplateUmbracoTemplates`.

## UCF123

Specify exactly one non-empty Data Type alias or a valid non-empty GUID key:

```csharp
[UCFMedia("Image")]
public partial class Image {
    [UCFProperty("Caption")] // UCF123: neither an alias nor a key
    public string? Caption { get; set; }
}
```

Use `[UCFProperty("Caption", "textstring")]`, or pass a known key with
`[UCFProperty("Caption", DataTypeKey = "ad9f0cf2-bda2-45d5-9ea1-a63cfc873fd3")]`.
Do not specify both.

## UCF124

The current project would generate a code-first registry while a referenced project already
generates a template registry. The runtime does not merge them. Move all code-first definitions
to the referenced models project and set `UmbracoCodeFirstGenerateTemplates` to `false` in the
site project. See [UCFTemplate](../attributes.md#ucftemplate--razor-view).

## UCF125

Generated composite properties, static getters, and interfaces must not conflict with existing
members or types. For example, two different composites cannot both contribute a CLR property
named `Title`, even when their Umbraco aliases differ. Rename one property or remove the
conflicting composition. A property reached through multiple paths from the same original
composite is not a conflict.

## Internal catalog diagnostics

UCF200 and UCF201 are for repository maintainers editing generator input catalogs, not normal
consumer declarations.

### UCF200

Editor metadata needs an `editors` array with complete entries and `configuration.properties`.
For example, `{"editors":[{"id":"textBox"}]}` is invalid. A minimal corrected entry is:

```json
{"editors":[{"id":"textBox","name":"Textbox","description":"Text","alias":"Umbraco.TextBox","uiAlias":"Umb.PropertyEditorUi.TextBox","returnType":"string","documentationUrl":"https://docs.umbraco.com/","configuration":{"properties":[]}}]}
```

### UCF201

Icon catalog entries must be non-empty, unique and produce distinct C# identifiers.
`{"icons":["icon-home","icon-home"]}` is invalid; use:

```json
{"icons":["icon-home","icon-user"]}
```
