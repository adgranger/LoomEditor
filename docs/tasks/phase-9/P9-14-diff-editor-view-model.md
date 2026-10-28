# P9-14 — `DiffEditorViewModel`: computing and publishing the diff

- **Status:** done
- **Depends on:** P9-13
- **Size:** M

## Goal

A `DiffEditorViewModel` watches two `TextModel`s and an options object, recomputes the diff off the UI thread (debounced 200 ms after an edit, immediately at start), publishes a `DiffState` and the list of unchanged regions with an event, and answers "make this line visible".

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/widget/diffEditor/diffEditorViewModel.ts` | 28–60, 90–124 | state members; the debouncer (`RunOnceScheduler` 200 ms) |
| same | 63–125 | "collapse touching unchanged ranges" (regions that touch after an edit are merged into one) |
| same | 127–205 | `updateUnchangedRegions` (carry the visible counts of the old regions over to the new ones using decorations; `LineRangeMapping.clip` / `inverse`) |
| same | 263–315 | "compute diff": the options it depends on, the cancellation, `normalizeDocumentDiff`, writing the result |
| same | 317–349 | `ensureModifiedLineIsVisible`, `ensureOriginalLineIsVisible`, `waitForDiff` |
| same | 233–261 | how the content change events of the two models mark the diff stale and schedule the debouncer (only that part: **not** `applyModifiedEdits` / `applyOriginalEdits`, ADR 0012 decision 4) |

## Files to create or change

- `src/LoomEditor.View/DiffEditor/DiffEditorViewModel.cs` (+ `DiffEditorViewModel.UnchangedRegions.cs`) — create
- `tests/LoomEditor.View.Tests/DiffEditor/DiffEditorViewModelTests.cs` — create
- `tests/LoomEditor.View.Tests/DiffEditor/TestHelpers/FakeDocumentDiffProvider.cs`, `ManualScheduler.cs` (reuse the phase 8 `ManualScheduler` / fake dispatcher if one exists: `grep -rn "class .*Scheduler\|class .*Dispatcher" tests/LoomEditor.View.Tests`) — create

## Design decisions (already made — do not change)

- `internal sealed class DiffEditorViewModel : IDisposable`; constructor `(TextModel original, TextModel modified, DiffEditorOptions options, IDocumentDiffProvider provider, IUiDispatcher dispatcher, IEditorScheduler scheduler)`. It starts a first computation immediately (not debounced).
- Public surface: `DiffState? Diff`, `bool IsDiffUpToDate`, `IReadOnlyList<UnchangedRegion> UnchangedRegions` (empty when `!options.HideUnchangedRegions.Enabled`; Monaco collapses all regions then), `event EventHandler? DiffChanged` (a new `DiffState` or a changed up-to-date flag), `event EventHandler? UnchangedRegionsChanged`, `Task WaitForDiffAsync(CancellationToken)` (completes when `IsDiffUpToDate` is true; a test helper, also used by the control to reveal the first diff), `void EnsureModifiedLineIsVisible(int line, RevealPreference)`, `void EnsureOriginalLineIsVisible(int line, RevealPreference)`.
- Computation: on a content change of either model: `IsDiffUpToDate = false`, `DiffChanged` (only when it was true), `scheduler.Schedule(200 ms, Recompute)` (a pending schedule is disposed first). `Recompute` cancels the previous `CancellationTokenSource`, calls `provider.ComputeDiffAsync(original, modified, new LinesDiffComputerOptions(options.IgnoreTrimWhitespace, options.MaxComputationTimeMs, options.ShowMovesEffective), token)` and consumes it with `UiContinuation.Then(…, dispatcher, onSuccess, onError, token)`.
  `onSuccess(result)`: drop it if the token is cancelled; `result = result.Normalize(...)` (P9-11); build `DiffState.FromDiffResult`, update the unchanged regions, set `IsDiffUpToDate = true`, raise the events. `onError`: ignore (keep the old state, stay not up to date); never rethrow on the UI thread.
  Options that trigger a recompute: `IgnoreTrimWhitespace`, `MaxComputationTimeMs`, `ShowMoves`, `RenderSideBySide` (through `ShowMovesEffective`) and the `HideUnchangedRegions` counts (immediately, not debounced).
- Between the edit and the new result the old `Diff` is kept (ADR 0012 decision 4); every consumer clamps ranges to the model's line count.
- Unchanged regions across recomputations (port of lines 127–205): the regions are tracked with one decoration per region on each model (`model.DeltaDecorations(ownerId, old ids, new)`; owner id: a private constant `DiffUnchangedOwnerId` (a negative or large unused int: read how `MarkerDecorations` picks its owner id and follow it); options with `Description = "unchanged"` only: no class name) so they follow edits; on a new diff the old regions' ranges are read with `GetDecorationRange` and the visible counts are transferred as in Monaco.
  The merge of touching regions (lines 63–125) runs after a content change.
- `Dispose` cancels the token, disposes the scheduler registration, removes the decorations and unsubscribes.
- `<remarks>Threading: UI thread only; the diff itself runs on a pool thread (DocumentDiffProvider).</remarks>`

## Tests

- [x] New (all with fakes: `FakeDocumentDiffProvider` returns a prepared `DocumentDiff` or a `TaskCompletionSource`; `ManualScheduler`; a synchronous dispatcher): `Start_ComputesDiffImmediately`, `Edit_MarksNotUpToDate_ThenRecomputesAfter200ms`, `Edit_TwiceQuickly_OneRecompute`, `Edit_WhileComputing_CancelsOldResult` (the old result is dropped), `ProviderThrows_KeepsOldState`, `OptionChange_IgnoreTrimWhitespace_Recomputes`, `UnchangedRegions_Disabled_Empty`, `UnchangedRegions_Enabled_FromDiff`, `UnchangedRegions_VisibleCountsSurviveRecompute`, `UnchangedRegions_TouchingRegionsMergedAfterEdit`, `UnchangedRegions_FollowEditsAbove` (insert lines above a region: its start moves), `EnsureModifiedLineIsVisible_InsideRegion_ShowsLine`, `EnsureModifiedLineIsVisible_OutsideRegion_NoChange`, `EnsureOriginalLineIsVisible_*`, `WaitForDiffAsync_CompletesWhenUpToDate`, `Dispose_CancelsAndStopsEvents`.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `DiffEditorViewModel` (subset) and "Behavior differences": no edit mapping (stale diff), no observables
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Line alignment (P9-15), decorations (P9-17), `serializeState`, moved text selection (`movedTextToCompare`, `activeMovedText`).

## Suggested commit message

`feat(view): add DiffEditorViewModel`
