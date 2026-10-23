# P6-05 — `TextModel.GetLineInjectedText` and `GetInjectedTextDecorations`

- **Status:** done
- **Depends on:** P6-04
- **Size:** S

## Goal

The text model answers "which injected text is on line N for editor X" and "every injected-text decoration", from its single
decorations tree.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/model/textModel.ts`.

| Lines | What to port |
|---|---|
| 1839–1841 | `getInjectedTextDecorations` |
| 1855–1861 | `getLineInjectedText` |
| 2184–2190 | `isOptionsInjectedText`, `isNodeInjectedText` |
| 2246–2250 | `DecorationsTrees.getInjectedTextInInterval` (the `showIfCollapsed || !range.isEmpty()` filter) |
| 2258–2262 | `DecorationsTrees.getAllInjectedText` |

## Files to create or change

- `src/LoomEditor.Core/Decorations/DecorationsTrees.cs` — change: add `GetInjectedTextInInterval`, `GetAllInjectedText`
- `src/LoomEditor.Core/Model/TextModel.Decorations.cs` — change: add `internal IReadOnlyList<ModelDecoration> GetInjectedTextDecorations(int ownerId = 0)`
  and `internal IReadOnlyList<LineInjectedText> GetLineInjectedText(int lineNumber, int ownerId = 0)`
- `tests/LoomEditor.Core.Tests/Model/TextModelInjectedTextTests.cs` — create

## Design decisions (already made — do not change)

- LoomEditor has **one** interval tree (PORTING "Behavior differences", P5-03). The injected-text queries search that tree
  and keep only nodes whose options have `Before` or `After` (`IsNodeInjectedText`), then apply Monaco's
  `ShowIfCollapsed || !range.IsEmpty` filter. Document it in the XML doc of both methods ("Monaco searches its injected-text
  tree; LoomEditor filters the single tree").
- `filterOutValidation` and `filterFontDecorations` are `false` and `onlyMarginDecorations` is `false`, exactly the arguments
  Monaco passes.
- `GetLineInjectedText` uses `_buffer.GetOffsetAt(lineNumber, 1)` and `+ _buffer.GetLineLength(lineNumber)` like Monaco, then
  `LineInjectedText.FromDecorations(...)` and keeps the items whose `LineNumber == lineNumber` (a decoration ending on the
  line but starting earlier gives a `before` on another line).
- Validate `lineNumber` like the other line queries of `TextModel` (`ArgumentOutOfRangeException` through the existing
  validation helper used by `GetLineDecorations`).
- `IsOptionsInjectedText(ModelDecorationOptions)` becomes an `internal static` helper on `ModelDecorationOptions`
  (`HasInjectedText` property: `Before is not null || After is not null`); P6-06 uses it.

## Tests

No upstream test calls these directly. New:

- [x] `GetLineInjectedText_NoDecorations_IsEmpty`
- [x] `GetLineInjectedText_AfterDecoration_IsAtTheEndColumn`
- [x] `GetLineInjectedText_EmptyRangeWithoutShowIfCollapsed_IsIgnored`; `…WithShowIfCollapsed_IsKept`
- [x] `GetLineInjectedText_FiltersByOwner` (owner 0 sees all, owner 1 sees its own)
- [x] `GetLineInjectedText_MultiLineDecoration_ReportsBeforeAndAfterOnTheirLines`
- [x] `GetLineInjectedText_IgnoresDecorationsWithoutInjectedText`
- [x] `GetInjectedTextDecorations_ReturnsOnlyInjectedText`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: the `TextModel` decorations rows mention the injected-text queries on the single tree
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The injected text changed event (P6-06), view model use (P6-17).

## Suggested commit message

`feat(core): query the injected text of a line`
