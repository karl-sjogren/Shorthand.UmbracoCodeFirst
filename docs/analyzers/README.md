# Analyzers

The `Shorthand.UmbracoCodeFirst.Analyzers` project validates code-first declarations while you
type and during builds. Each diagnostic is implemented by a focused analyzer, and every rule has
its own page with an example.

| Id | Title | Severity |
| -- | ----- | -------- |
| [UCF100](UCF100.md) | Invalid code-first alias | Error |
| [UCF101](UCF101.md) | Duplicate code-first alias | Warning |
| [UCF102](UCF102.md) | Repeated layout configuration should use a shared attribute | Warning |
| [UCF103](UCF103.md) | Repeated validation configuration should use a shared attribute | Info |
| [UCF104](UCF104.md) | Unsupported code-first property type | Warning |
| [UCF105](UCF105.md) | UCFProperty used outside a UCFDocumentType | Warning |
| [UCF106](UCF106.md) | Invalid content composition target | Warning |
| [UCF107](UCF107.md) | Invalid content composition graph (duplicate, self, cycle) | Warning |
| [UCF108](UCF108.md) | Unknown Umbraco property editor | Error |
| [UCF109](UCF109.md) | Incompatible Umbraco property editor type | Warning |
| [UCF110](UCF110.md) | Incompatible Document Type and Element Type composition | Warning |

The source generator additionally reports `UCF001` (unsupported published model property type) as
an error; see [Source generator](../source-generator.md#generator-diagnostics).

Notes:

- Diagnostic help links currently point at the `feature/initial-implementation` branch and should
  be switched to `main` when merged.
- UCF102 provides a code fix that creates a reusable derived layout attribute in the current file.
- Alias validity follows Umbraco: letters, combining marks, digits, whitespace, `.` and `-`.
