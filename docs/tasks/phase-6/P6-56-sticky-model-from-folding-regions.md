# P6-56 — The sticky model from indentation folding regions

- **Status:** done
- **Depends on:** P6-55
- **Size:** S

## Goal

LoomEditor builds the sticky scroll model (a tree of `StickyElement`s) from the indentation folding regions of the model,
like Monaco's `StickyModelFromCandidateIndentationFoldingProvider`, and a simplified `StickyModelProvider` picks it.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/contrib/stickyScroll/browser/stickyScrollModelProvider.ts`.

| Lines | What to port |
|---|---|
| 23–33 | `ModelProvider`, `Status` (informative) |
| 45–121 | `StickyModelProvider` (the provider order by `defaultModel` and the fallback chain: outline → folding provider → indentation) |
| 134–209 | `StickyModelCandidateProvider` (`computeStickyModel`, `isModelValid`, `isProviderValid`) |
| 313–367 | `StickyModelFromCandidateFoldingProvider` (`createStickyModel`, `isModelValid`, `_fromFoldingRegions`) |
| 369–384 | `StickyModelFromCandidateIndentationFoldingProvider` |

## Files to create or change

- `src/LoomEditor.View/Features/StickyScroll/StickyModelProvider.cs` — create (header `stickyScrollModelProvider.ts`)
- `tests/LoomEditor.View.Tests/Features/StickyScroll/StickyModelProviderTests.cs` — create

## Design decisions (already made — do not change)

- **Synchronous:** LoomEditor's `IndentRangeProvider.Compute()` (P5-84) is synchronous; the provider exposes
  `StickyModel? Update()` instead of `update(token): Promise`. The cancellation tokens and promises are dropped; the debounce stays in
  the candidate provider (P6-57, 50 ms through `IEditorScheduler`).
- Only the indentation candidate exists: outline (document symbols) and syntax folding providers are phase 8. Whatever
  `DefaultModel` is, the chain ends on the indentation model, which is what Monaco does when the other providers are missing or
  invalid. Keep a `// Phase 8: outline and folding range providers` comment where the chain starts.
- `_fromFoldingRegions` is copied exactly (`FoldingRegions.GetParentIndex`, `GetStartLineNumber`, `GetEndLineNumber` exist), including the
  `endLineNumber + 1` of the range.
- `RangesLimitReporter`: reuse the Folding feature's class (`Features/Folding/RangesLimitReporter.cs`) with the folding maximum regions
  limit (Monaco's `_foldingLimitReporter` is a new reporter on the same option).
- `StickyModel.Version` = the text model's `VersionId` at computation time.

## Tests

No upstream unit tests (the 4 upstream tests use the outline model: phase 8). New:

- [x] `FromFoldingRegions_NestedBlocks_BuildsTheTree` (a function containing an `if`: root → function → if)
- [x] `FromFoldingRegions_RangeEndsOneLineAfterTheRegion`
- [x] `Update_DefaultModelOutline_FallsBackToIndentation`
- [x] `Update_EmptyDocument_HasAnEmptyRoot`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `stickyScrollModelProvider.ts` (🔶 indentation model only, synchronous)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Candidates and the controller (P6-57, P6-58).

## Suggested commit message

`feat(view): build the sticky scroll model from indentation`
