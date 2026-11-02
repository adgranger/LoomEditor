# P14-01 — `DiffChangeBlock` and `LoomDiffEditor.Changes`

- **Status:** done
- **Depends on:** P13-11
- **Size:** S

**Recommended model:** Sonnet

## Goal

A host reads the computed diff as a read-only list of change blocks (original and modified line ranges, inner character changes), refreshed when `DiffChanged` is
raised.

## Sources

Sources: none (specified in this card; [phase-14/README.md](README.md) decisions 5–7). Read first:
`src/LoomEditor.View/DiffEditor/DiffState.cs` (`DiffMapping`), `src/LoomEditor.Core/Diff/DetailedLineRangeMapping.cs`, `LineRange` (`StartLineNumber`,
`EndLineNumberExclusive`, `Length`), `src/LoomEditor.Core/Diff/RangeMapping.cs`, `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.cs`
(`OnViewModelDiffChanged`, `ChangeCount`), `LoomDiffEditor.Navigation.cs`, `tests/LoomEditor.View.Tests/DiffEditor/` (how a diff is computed in tests).

## Files to create or change

- `src/LoomEditor.View/DiffEditor/DiffChangeBlock.cs` — create: `public sealed class DiffChangeBlock` and `public readonly record struct DiffInnerChange`
- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.cs` — change: `Changes`
- `src/LoomEditor.View/PublicAPI.Unshipped.txt`, `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — `./eng/Update-PublicApi.ps1`
- `tests/LoomEditor.View.Tests/DiffEditor/DiffChangeBlockTests.cs` — create

## Design decisions (already made — do not change)

- `DiffChangeBlock`: constructor `internal`; properties as in the phase README. `internal static IReadOnlyList<DiffChangeBlock> FromMappings(IReadOnlyList<DiffMapping>)`
  converts in order: `OriginalStartLineNumber = Original.StartLineNumber`, `OriginalLineCount = Original.Length`, same for modified; `InnerChanges` maps each
  `RangeMapping` to `DiffInnerChange(OriginalRange, ModifiedRange)`, empty array (`[]`) when `InnerChanges` is null.
- `ToString()` returns `"{original start},{count} -> {modified start},{count}"` (debugger and test messages only).
- XML doc of the class: decision 6 (start line of an empty side = the line **before which**; the git hunk start is `start - 1` for an empty side) and decision 7.
- `LoomDiffEditor.Changes`: a field `IReadOnlyList<DiffChangeBlock> _changes = []`, rebuilt in `OnViewModelDiffChanged` **before** `DiffChanged` is raised, only when
  `_viewModel.Diff` is a different instance than the one it was built from (`IsDiffUpToDate` changes raise `DiffChanged` too). Reset to `[]` in `OnUnloaded`. XML doc:
  "The last published diff; stale while `IsDiffUpToDate` is false. Raised with `DiffChanged`. `GoToNextChange` walks the same blocks."
- `ChangeCount` is unchanged (it equals `Changes.Count`).

## Tests

Compute each diff with the same diff computer and options the view model uses (`DocumentDiffProvider` defaults; see the existing View diff tests), then
`DiffChangeBlock.FromMappings`. Assert exact `ToString()` values.

- [x] New: `FromMappings_SeveralBlocks_InOrderWithExactRanges` (3 separated changes in a 20-line file)
- [x] New: `FromMappings_PureInsertion_OriginalCountIsZero` (lines inserted between lines 4 and 5: original start 5, count 0)
- [x] New: `FromMappings_PureDeletion_ModifiedCountIsZero`
- [x] New: `FromMappings_ModifiedLine_PairWithInnerChanges` (one word changed: counts 1/1, one inner change with the word's columns)
- [x] New: `FromMappings_CrlfAndLf_SameBlocks` (the same edit with `\r\n` and with `\n` texts gives equal blocks; CRLF does not turn the file into one block)
- [x] New: `FromMappings_EmptyOriginal_OneBlock` and `FromMappings_IdenticalTexts_NoBlock` (pin the computer's result for `""` vs `"a\nb"`; comment why)
- [x] New: `FromMappings_NoInnerChanges_EmptyList`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

A `ChangesChanged` event (decision 5), moved-line blocks, a block lookup by line (P14-02 has the internal one).

## Suggested commit message

`feat(winui): expose the diff change blocks of LoomDiffEditor`

## Result

Build (0 warnings) and tests green; 8 new tests in `DiffChangeBlockTests`. `DiffInnerChange` lives in its own file (`DiffInnerChange.cs`). `Update-PublicApi.ps1` also re-sorted the existing `SetViewZoneHeight` line of the WinUI API file.
