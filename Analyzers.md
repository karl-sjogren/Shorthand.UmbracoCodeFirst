# Ideas for analyzers

## Suggestions/information

1. Check if there are multiple UCFGroup/UCFTab attributes that share the same alias. If so suggest to create a derived attribute. Codefix might be possible but not required.
2. Check if there are multiple UCFValidation attributes that share the same alias. If so suggest to create a derived attribute. Codefix might be possible but not required.

## Warnings

1. Aliases should be unique across all UCF attributes. A codefix could be provided to rename the alias to a unique name, e.g. by appending a numeric suffix.
2. Aliases should not contain any chars in this regex: [^\p{L}\p{M}\p{Nd}\s.-]+ A codefix could be provided to remove the invalid chars and create a proper alias.
3. Using UCFProperty with a type that is not supported by the code-first contract. A codefix could be provided to change the property type to a supported type.
4. Using UCFProperty in a class that isn't a UCFDocumentType. A codefix could be provided to add the UCFDocumentType attribute to the class.

## Implemented analyzers

- `UCF100` reports aliases containing characters outside the supported alias character set.
- `UCF101` reports duplicate aliases within the relevant code-first scope.
- `UCF102` suggests shared derived group/tab attributes when layout configuration is repeated.
- `UCF103` suggests shared derived validation attributes when validation configuration is repeated.
- `UCF104` reports UCF properties whose CLR type is unsupported by the code-first contract.
- `UCF105` reports UCF properties declared outside a UCF Document Type.

Detailed documentation for each diagnostic is available under `docs/analyzers`.
### UCF106 - Invalid content composition target

Reports `UCFComposite` declarations that reference a class without `UCFDocumentTypeAttribute`.

### UCF107 - Invalid content composition graph

Reports duplicate, self-referencing, or cyclic Document Type compositions.
