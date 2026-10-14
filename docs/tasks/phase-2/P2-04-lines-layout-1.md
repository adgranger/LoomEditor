# P2-04 — `LinesLayout` (1/2): vertical offsets and whitespaces

- **Status:** todo
- **Depends on:** P2-03
- **Size:** M

## Goal

The first half of Monaco's `LinesLayout`: the vertical offset of every line, the total height, and "whitespaces"
(the gaps that view zones will use later). Offsets are integers and are computed in O(log n), so the layout of a
2.5-million-line document costs nothing per frame.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewLayout/linesLayout.ts` | 10–81 | `IPendingChange`, `IPendingRemove`, `PendingChanges`, `EditorWhitespace` |
| same | 83–604 | `LinesLayout`: fields, constructor, `findInsertionIndex`, `setDefaultLineHeight`, `setPadding`, `onFlushed`, `changeLineHeights`, `changeWhitespace`, `_commitPendingChanges` and its helpers, `onLinesDeleted`, `onLinesInserted`, the whitespace accumulated heights, `getLinesTotalHeight`, `getVerticalOffsetForLineNumber`, `getLineHeightForLineNumber`, `getVerticalOffsetAfterLineNumber`, `hasWhitespace`, `getWhitespaceMinWidth`, `isAfterLines`, `isInTopPadding`, `isInBottomPadding`, `getLineNumberAtOrAfterVerticalOffset` |
| same | 731–794 | `getVerticalOffsetForWhitespaceIndex`, `getWhitespaceIndexAtOrAfterVerticallOffset` |
| same | 866–912 | `getWhitespaces`, `getWhitespacesCount`, `getIdForWhitespaceIndex`, `getAfterLineNumberForWhitespaceIndex`, `getHeightForWhitespaceIndex` |
| `src/vs/editor/common/viewModel.ts` | 159–177 | `IEditorWhitespace`, `IWhitespaceChangeAccessor`, `ILineHeightChangeAccessor` |
| `src/vs/base/common/strings.ts` | 988 (`singleLetterHash`) | used for whitespace ids |
| `src/vs/editor/test/common/viewLayout/linesLayout.test.ts` | see Tests | 8 of the 12 tests |

## Files to create or change

- `src/LoomEditor.View/Layout/LinesLayout.cs` — create (`partial`; whitespaces in `LinesLayout.Whitespaces.cs`)
- `src/LoomEditor.View/Layout/EditorWhitespace.cs`, `IWhitespaceChangeAccessor.cs`, `ILineHeightChangeAccessor.cs` — create
- `tests/LoomEditor.View.Tests/Layout/LinesLayoutTests.cs` — create

## Design decisions (already made — do not change)

- All types `internal`. Offsets, heights and line numbers are `int`. Every `x | 0` of Monaco disappears (the value is
  already an `int`). Where Monaco truncates a division (`(a / 2) | 0`), C# integer division does the same for
  non-negative values; `(low + high) >>> 1` stays `(low + high) >>> 1` (C# 11 has `>>>`).
- `INSTANCE_COUNT` becomes a `static int` updated with `Interlocked.Increment` (tests run in parallel).
  `singleLetterHash` is ported as a private static helper.
- `changeWhitespace(callback)` / `changeLineHeights(callback)` take an `Action<IWhitespaceChangeAccessor>` /
  `Action<ILineHeightChangeAccessor>`. The accessor is a private nested class that writes into the pending changes,
  and the `try/finally` that commits is kept.
- `_commitPendingChanges` stays `internal` (Monaco calls it from `PendingChanges`).
- `EditorWhitespace` is a mutable `sealed class` (Monaco mutates `afterLineNumber`, `height`, `prefixSum`).
- The constructor takes `IReadOnlyList<CustomLineHeightData>` (P2-03).

## Tests

- [ ] Port: `LinesLayout 1`, `LinesLayout 2`, `LinesLayout Padding`, `LinesLayout getLineNumberAtOrAfterVerticalOffset`,
      `LinesLayout` (the one named only `LinesLayout`), `LinesLayout findInsertionIndex`,
      `LinesLayout changeWhitespaceAfterLineNumber & getFirstWhitespaceIndexAfterLineNumber`, `LinesLayout Bug`
- [ ] Port the helpers `insertWhitespace`, `changeOneWhitespace`, `removeWhitespace` of the suite (lines 13–31) as private methods
- [ ] New: a 2,500,000-line layout with line height 19 gives `GetLinesTotalHeight() == 47_500_000` and
      `GetLineNumberAtOrAfterVerticalOffset(47_499_999) == 2_500_000` (no overflow, no float)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `editor/common/viewLayout/linesLayout.ts` in progress (P2-04)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`getLinesViewportData`, `getWhitespaceAtVerticalOffset`, `getWhitespaceViewportData` and the 4 tests that use them (P2-05).

## Suggested commit message

`feat(view): port LinesLayout offsets and whitespaces`
