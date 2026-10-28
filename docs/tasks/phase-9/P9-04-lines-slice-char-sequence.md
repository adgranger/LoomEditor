# P9-04 — `LinesSliceCharSequence`

- **Status:** done
- **Depends on:** P9-03
- **Size:** M

## Goal

The character-level sequence of the second diff pass exists: a slice of several lines seen as one sequence of characters, with word and sub-word search, boundary scores that prefer to cut at word boundaries, and the translation of an offset back to a `Position`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/diff/defaultLinesDiffComputer/linesSliceCharSequence.ts` | 14–186 | `LinesSliceCharSequence` (constructor with the `considerWhitespaceChanges` trimming, `getElement`, `length`, `getText`, `getBoundaryScore`, `countLinesIn`, `isStronglyEqual`, `extendToFullLines`, `translateOffset`, `translateRange`, `findWordContaining`, `findSubWordContaining`, `getBoundaryScore`) |
| same | 187–246 | `isWordChar`, `isUpperCase`, `CharBoundaryCategory`, the `score` table, `getCategoryBoundaryScore`, `getCategory` |
| `src/vs/editor/test/node/diffing/defaultLinesDiffComputer.test.ts` | 18–29 | `myers` › `1`, now with the real sequences (the card P9-03 left it with `ArraySequence`) |

## Files to create or change

- `src/LoomEditor.Core/Diff/LinesSliceCharSequence.cs` — create
- `tests/LoomEditor.Core.Tests/Diff/LinesSliceCharSequenceTests.cs` — create
- `tests/LoomEditor.Core.Tests/Diff/Algorithms/MyersDiffAlgorithmTests.cs` — change (the `1` test with `LinesSliceCharSequence`)

## Design decisions (already made — do not change)

- Constructor `LinesSliceCharSequence(string[] lines, Range range, bool considerWhitespaceChanges)`; `Range` is `LoomEditor.Core.Text.Range` (the `1 → Number.MAX_SAFE_INTEGER` end columns of the Monaco test become `int.MaxValue`; the constructor clamps the column to the line length like Monaco).
- Elements are UTF-16 code units (`char` → `int`), as in Monaco (`charCodeAt`). `translateOffset` returns a `Position`. `translateRange(OffsetRange)` returns a `Range`.
- The `CharBoundaryCategory` enum and the `score` table (`Record<CharBoundaryCategory, number>`) become a `private enum` and a `private static readonly int[]` indexed by the enum; the numbers are copied from lines 197–220.
- Word and sub-word search (`findWordContaining`, `findSubWordContaining`) return `OffsetRange?`. A "word char" is `[a-zA-Z0-9]` (`isWordChar`, lines 187–192): ASCII only, as in Monaco. Do **not** use `char.IsLetterOrDigit`.
- `<remarks>Threading: immutable after construction.</remarks>`.

## Tests

- [x] Port: `myers` › `1` (with `LinesSliceCharSequence(["hello world"], Range(1,1,1,int.MaxValue), true)` against `"hallo welt"`; assert no exception).
- [x] New: `Length_TrimsLeadingWhitespace_WhenNotConsideringWhitespace` (and keeps it when considering), `TranslateOffset_MultipleLines` (offset at a line end → `Position(line, len+1)`), `TranslateRange_*`, `FindWordContaining_InsideAndOutsideWord`, `FindSubWordContaining_CamelCase` (`fooBarBaz`, offset in `Bar` → `Bar`), `FindSubWordContaining_SnakeCase`, `GetBoundaryScore_PrefersLineBreakOverWordBoundaryOverInsideWord`, `ExtendToFullLines_*`, `CountLinesIn_*`, `IsStronglyEqual_*`, `GetText_Range`.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated: `linesSliceCharSequence.ts`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Heuristics that use it (P9-05, P9-06), the driver (P9-08).

## Suggested commit message

`feat(core): port LinesSliceCharSequence`
