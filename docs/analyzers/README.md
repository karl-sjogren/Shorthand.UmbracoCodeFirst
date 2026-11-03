# Analyzers

`CodeFirstAttributeAnalyzer` (project `Shorthand.UmbracoCodeFirst.Analyzers`) validates code-first
declarations while you type and during builds. Every rule has its own page with an example.

| Id | Title | Severity |
| -- | ----- | -------- |
| [UCF100](UCF100.md) | Invalid code-first alias | Error |
| [UCF101](UCF101.md) | Duplicate code-first alias | Warning |
| [UCF102](UCF102.md) | Repeated layout configuration should use a shared attribute | Warning |
| [UCF103](UCF103.md) | Repeated validation configuration should use a shared attribute | Info |
| [UCF104](UCF104.md) | Unsupported code-first property type | Warning |
| [UCF105](UCF105.md) | UCFProperty used outside a UCFContentType | Warning |
| [UCF106](UCF106.md) | Invalid content composition target | Warning |
| [UCF107](UCF107.md) | Invalid content composition graph (duplicate, self, cycle) | Warning |

The source generator additionally reports `UCF001` (unsupported published model property type) as
an error; see [Source generator](../source-generator.md#generator-diagnostics).

Notes:

- Diagnostic help links currently point at the `feature/initial-implementation` branch and should
  be switched to `main` when merged.
- No code fixes are provided yet.
- Alias validity follows Umbraco: letters, combining marks, digits, whitespace, `.` and `-`.
