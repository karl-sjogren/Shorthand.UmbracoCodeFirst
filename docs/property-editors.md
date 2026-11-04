# Property editor metadata

The built-in Umbraco property-editor catalog is maintained in
[`umbraco-property-editors.json`](../src/Shorthand.UmbracoCodeFirst/PropertyEditors/umbraco-property-editors.json).
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
