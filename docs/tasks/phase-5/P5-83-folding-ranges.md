# P5-83 — Folding ranges (`FoldingRegions`, `sanitizeAndMerge`)

- **Status:** todo
- **Depends on:** P5-82
- **Size:** M

## Goal

The `Folding` feature folder exists with Monaco's compact list of folding ranges: `FoldingRegions` (start / end lines,
parent index, collapsed / user-defined / recovered flags packed in `uint[]` and bit fields), `FoldingRegion`, `FoldRange`,
and `sanitizeAndMerge`, which merges the ranges of the provider with the folded and manual ones.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/folding/browser/foldingRanges.ts` | 8–36 | `ILineRange`, `FoldSource`, `foldSourceAbbr`, `FoldRange`, `MAX_FOLDING_REGIONS`, `MAX_LINE_NUMBER`, `MASK_INDENT` |
| same | 38–61 | `BitField` |
| same | 63–393 | `FoldingRegions` (all members, `toString`, `toFoldRange`, `fromFoldRanges`, `sanitizeAndMerge`) |
| same | 395–429 | `FoldingRegion` |
| `contrib/folding/browser/folding.ts` | 587–608 | `SelectedLines`, `toSelectedLines` |
| `contrib/folding/test/browser/foldingRanges.test.ts` | 1–34, 120–206 | the helpers and the four `sanitizeAndMerge` tests |

## Files to create or change

- `src/LoomEditor.View/Features/Folding/FoldingRanges.cs` — create: `FoldSource`, `FoldRange`, `ILineRange`, `BitField` (private nested or file-local), `FoldingRegions`
- `src/LoomEditor.View/Features/Folding/FoldingRegions.SanitizeAndMerge.cs` — create (partial: `FromFoldRanges`, `SanitizeAndMerge`)
- `src/LoomEditor.View/Features/Folding/FoldingRegion.cs` — create
- `src/LoomEditor.View/Features/Folding/SelectedLines.cs` — create: `ISelectedLines` + `SelectedLines.From(IReadOnlyList<Selection>?)` (Monaco: `toSelectedLines`)
- `tests/LoomEditor.View.Tests/Features/Folding/FoldingRangesTests.cs` — create

## Design decisions (already made — do not change)

- Namespace `LoomEditor.View.Features.Folding` (ADR 0009, decisions 2 and 13). Everything `internal`.
- **Packed arrays** (phase-5 README, trap 6): `_startIndexes` and `_endIndexes` are `uint[]`. `ensureParentIndices`
  stores the parent index in the high byte of both arrays with **unchecked `uint` arithmetic**:
  `_startIndexes[i] = (uint)startLineNumber + (((uint)parentIndex & 0xFF) << 24)` and
  `_endIndexes[i] = (uint)endLineNumber + (((uint)parentIndex & 0xFF00) << 16)`; with `parentIndex == -1` this stores
  `0xFF…`, and `GetParentIndex` (`>>> 24`, `>>> 16` → `>>` on `uint`) reads `MAX_FOLDING_REGIONS` (0xFFFF) back,
  which means -1. `GetStartLineNumber` masks with `MAX_LINE_NUMBER` (0xFFFFFF). Keep these constants as `uint`.
- `BitField` keeps a `uint[]` of `ceil(size / 32)` words.
- `FoldRange` is a mutable class (Monaco mutates `useRange.isCollapsed` in `sanitizeAndMerge`):
  `internal sealed class FoldRange { int StartLineNumber; int EndLineNumber; string? Type; bool IsCollapsed; FoldSource Source; }`
  with settable properties.
- `SanitizeAndMerge` has two overloads for Monaco's `FoldingRegions | FoldRange[]` inputs:
  `(FoldingRegions rangesA, IReadOnlyList<FoldRange> rangesB, int? maxLineNumber, ISelectedLines? selection = null)` and
  `(IReadOnlyList<FoldRange> rangesA, IReadOnlyList<FoldRange> rangesB, int? maxLineNumber, ISelectedLines? selection = null)`;
  both use one private core with Monaco's `getIndexedFunction` as a `Func<int, FoldRange?>`. `maxLineNumber ?? int.MaxValue`
  replaces `Number.MAX_VALUE`.
- The constructor throws `ArgumentException("invalid startIndexes or endIndexes size")` where Monaco throws.
- `ToString()` keeps Monaco's format (used by test messages).

## Tests

- [ ] Port: `FoldingRanges` › `sanitizeAndMerge1`, `sanitizeAndMerge2`, `sanitizeAndMerge3`, `sanitizeAndMerge4`
- [ ] New: `GetParentIndex_NestedRegions_ReturnsTheEnclosingRegion` (and -1 for a top-level region)
- [ ] New: `SetSource_UserDefinedThenProvider_ClearsBothFlags`
- [ ] New: `Constructor_MoreThanMaxFoldingRegions_Throws`
- [ ] New: `FromFoldRanges_NoTypes_HasTypesIsFalse`

(`test max folding regions`, `findRange` and `setCollapsed` need `computeRanges`: P5-84.)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: new row `contrib/folding/browser/foldingRanges.ts` (✅ P5-83) and `folding.ts`
      (`toSelectedLines`, P5-83)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The indentation provider (P5-84), the folding model (P5-85). The feature class and its `README.md` (P5-88).

## Suggested commit message

`feat(folding): port the folding ranges`
