# P6-07 — `ModelLineProjectionData`: break offsets and offset translation

- **Status:** done
- **Depends on:** P6-04
- **Size:** S

## Goal

The data that describes how one model line is wrapped and which text is injected into it exists, with the translation
between input offsets (model line) and output positions (view line index + offset).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/modelLineProjectionData.ts`.

| Lines | What to port |
|---|---|
| 13–37 | the class comment (input → input with injections → breaks → output with wrapped indent): copy it into the XML doc |
| 38–56 | constructor and fields `injectionOffsets`, `injectionOptions`, `breakOffsets`, `breakOffsetsVisibleColumn`, `wrappedTextIndentLength` |
| 58–83 | `getOutputLineCount`, `getMinOutputOffset`, `getLineLength`, `getMaxOutputOffset` |
| 85–109 | `translateToInputOffset` |
| 111–128 | `translateToOutputPosition` |
| 130–167 | `offsetInInputWithInjectionsToOutputPosition` (binary search, both affinity branches) |
| 313–329 | `class OutputPosition` (`toString`, `toPosition`) |

Tests: `src/vs/editor/test/common/viewModel/lineBreakData.test.ts` (suite `Editor ViewModel - LineBreakData`): 16–21
`Basic`, 23–48 the helpers `sequence`, `testInverse`, `getInputOffsets`, `getOutputOffsets`, `mapTextToInjectedTextOptions`,
and in the suites `Injected Text 1` (50–149), `Injected Text 2` (189–209), `Injected Text 3` (211–232) every test **except**
`normalization` (151–187, P6-08).

## Files to create or change

- `src/LoomEditor.View/Layout/ModelLineProjectionData.cs` — create (header `modelLineProjectionData.ts`; `partial` class: this card's file)
- `src/LoomEditor.View/Layout/OutputPosition.cs` — create
- `tests/LoomEditor.View.Tests/Layout/ModelLineProjectionDataTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed partial class ModelLineProjectionData` with a constructor taking
  `int[]? injectionOffsets, ModelDecorationInjectedTextOptions[]? injectionOptions, int[] breakOffsets, double[] breakOffsetsVisibleColumn, int wrappedTextIndentLength`
  and get-only properties of the same names (`InjectionOffsets`, …). `BreakOffsetsVisibleColumn` is `double[]` on purpose:
  Monaco stores JavaScript numbers, and they are fractional when the full-width/half-width ratio of the font is not an
  integer (`issue #110392` uses 1.66). Arrays are kept as given (no copy); the class is treated
  as immutable (say so in the threading remark). `Guard.Assert` that both injection arrays are null together and have the same
  length.
- `OutputPosition` is `internal readonly record struct OutputPosition(int OutputLineIndex, int OutputOffset)` with
  `ToPosition(int baseLineNumber)` → `new Position(baseLineNumber + OutputLineIndex, OutputOffset + 1)` and
  `ToString()` → `"{index}:{offset}"` (invariant culture).
- `TranslateToOutputPosition(int inputOffset, PositionAffinity affinity = PositionAffinity.None)` — note Monaco's default
  `None` here (the identity projection in LoomEditor uses `Left`; keep each default as upstream).
- `mid = low + ((high - low) / 2) | 0` → `low + (high - low) / 2` (integer division; operands are non-negative).
- The `injectionOptions![i].content.length` reads become `InjectionOptions![i].Content.Length`.
- File stays under ~200 lines; P6-08 adds `ModelLineProjectionData.Injections.cs` (second partial).

## Tests

- [x] Port (`tests/LoomEditor.View.Tests/Layout/LineBreakDataTests.cs`, create): `Basic`;
      `Injected Text 1` › `getInputOffsetOfOutputPosition`, `getOutputPositionOfInputOffset`,
      `getInputOffsetOfOutputPosition is inverse of getOutputPositionOfInputOffset`; `Injected Text 2` › the 2 tests;
      `Injected Text 3` › the 2 tests (nested suites become nested classes or a name prefix; `// Monaco:` comments name the
      suite path). The constant `data` of each suite becomes a `static readonly` field.

New, with hand-built data:

- [x] `GetOutputLineCount_IsTheNumberOfBreakOffsets`
- [x] `GetLineLength_WrappedLine_AddsTheIndent` (breaks `[5, 9]`, indent 2 → lengths 5 and 6)
- [x] `GetMinOutputOffset_FirstLineIsZero_OthersAreTheIndent`
- [x] `TranslateToInputOffset_InsideTheIndent_ClampsToTheLineStart`
- [x] `TranslateToInputOffset_InsideInjectedText_ReturnsTheInjectionOffset`; `…AfterInjectedText_SubtractsItsLength`
- [x] `TranslateToOutputPosition_AtABreak_LeftStaysRightMoves` (affinity `Left` vs `None`/`Right` at offset 5 with breaks `[5, 9]`)
- [x] `TranslateToOutputPosition_AtAnInjection_RightSkipsTheInjectedText`
- [x] `OutputPosition_ToPositionAndToString`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: new row `editor/common/modelLineProjectionData.ts` (partial: P6-07)
- [x] `docs/PORTING.md` "Tests ported in phase 6" (created by P6-03): row `lineBreakData.test.ts` (7 / 8)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`normalizeOutputPosition`, `getInjectedText` and the cursor-stop helpers (P6-08); the line-break computer interfaces (P6-09).

## Suggested commit message

`feat(view): add ModelLineProjectionData`
