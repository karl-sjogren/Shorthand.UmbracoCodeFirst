# Property editor metadata

The built-in Umbraco property-editor catalog is maintained in
[`umbraco-property-editors.json`](../src/Shorthand.UmbracoCodeFirst/PropertyEditors/umbraco-property-editors.json).
It is the single metadata source for:

- generated `UmbracoEditors` aliases, UI aliases, descriptions, return types, and documentation links;
- analyzer validation of Data Type editor aliases;
- analyzer checks that a code-first property's CLR type is compatible with the editor's documented
  return type.

The source generator consumes the file as an `AdditionalFiles` input and emits
`UmbracoEditors.g.cs`. Do not edit the generated file. Add or update an entry in the JSON catalog,
then rebuild.

Each entry has a stable `id`, display `name`, `description`, schema `alias`, `uiAlias`,
documented `returnType`, and `documentationUrl`. Return types are descriptors rather than
assembly-qualified CLR names so the netstandard source generator and analyzer can consume them
without referencing Umbraco runtime assemblies.

Compatibility diagnostics are conservative. Scalar and string collection editors are checked
directly; nullable values are accepted; picker/block/media return types are treated as compatible
when the catalog cannot safely resolve the consuming Umbraco type. Configuration-dependent editors
should use a broad documented return descriptor rather than causing false positives.

The catalog currently follows the [Umbraco built-in property editor
documentation](https://docs.umbraco.com/umbraco-cms/model-your-content/property-editors/built-in-umbraco-property-editors).
When an editor has confusing historical aliases, its stable catalog ID and display name preserve
the documented distinction.
