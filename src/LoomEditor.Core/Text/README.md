# Text

Value types used everywhere in the editor. All are immutable `readonly record struct`s.

| Type | Meaning | Monaco source |
|---|---|---|
| `Position` | A 1-based (line, column) pair | `core/position.ts` |
| `Range` | A start and an end position, always normalized (start <= end) | `core/range.ts` |
| `Selection` | A range plus a direction (anchor and caret) | `core/selection.ts` |
| `TextEdit` | "Replace this range with this text" | `core/editOperation.ts` |

`Range` clashes with `System.Range`: add `global using Range = LoomEditor.Core.Text.Range;` (see docs/adr/0003-naming-and-namespaces.md).
