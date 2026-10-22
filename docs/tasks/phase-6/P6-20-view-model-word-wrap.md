# P6-20 — The view model wraps lines: settings, model changes, content width

- **Status:** done
- **Depends on:** P6-19, P6-02
- **Size:** M

## Goal

`ViewModel` turns word wrap on: it builds its view lines with the wrapping settings, rebuilds them when the layout's
wrapping column, the font or the wrapping options change (keeping the viewport stable), computes line breaks for edited
lines in two passes like Monaco, and `ViewLayout` computes the content width of a wrapped editor.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewModel/viewModelImpl.ts` | 95–121 | how the constructor builds `ViewModelLinesFromProjectedModel` from the options |
| same | 276–296 | `_onConfigurationChanged`: the `setWrappingSettings` block (stable viewport, flush, line mapping changed, cursor, decorations) |
| same | 322–355 | `onDidChangeContentOrInjectedText`: the first pass (one `createLineBreaksComputer`, `addRequest` for every inserted / changed line, `finalize`, `ArrayQueue`) |
| same | 377–405 | the second pass: `takeCount` for inserted lines, `dequeue` for a changed line |
| `src/vs/editor/common/viewLayout/viewLayout.ts` | 313–334 | `_computeContentWidth` (the `isViewportWrapping` branch) |

## Files to create or change

- `src/LoomEditor.View/Layout/ViewModel.cs` — change: the constructor builds the lines with the wrapping settings;
  `OnModelRawContentChanged` does the two passes; `SetOptions` / `SetViewportSize` / `SetFontInfo` call a new private
  `UpdateWrappingSettings()` after `RecomputeLayoutInfo()`
- `src/LoomEditor.View/Layout/ViewModel.Wrapping.cs` — create (partial, header `viewModelImpl.ts`): `UpdateWrappingSettings`
- `src/LoomEditor.View/Layout/ViewLayout.cs` — change: `ComputeContentWidth` gets the wrapping branch (it reads
  `_layoutInfo.IsViewportWrapping`); remove the "phase 2/phase 6" comment
- `tests/LoomEditor.View.Tests/Layout/ViewModelWordWrapTests.cs` — create

## Design decisions (already made — do not change)

- The wrapping options come from `EditorLayoutOptions.Wrapping` (P6-02); the wrapping column from `LayoutInfo.WrappingColumn`.
  The view model owns one `MonospaceLineBreaksComputerFactory`, created from the options and re-created when the two break
  character strings change.
- `UpdateWrappingSettings()` is Monaco's `setWrappingSettings` block: `CaptureStableViewport()` (exists, `ViewModel.HiddenAreas.cs`),
  then inside `EmitViewEvent`: if `_lines.SetWrappingSettings(...)` returns true → `_cursor.OnLineMappingChanged`,
  `_decorations.OnLineMappingChanged()`, `ViewLayout.OnFlushed(GetLineCount(), [])`, `ViewLayout.OnHeightMaybeChanged()`,
  then `stableViewport.RecoverViewportStart(...)`; afterwards `HandleVisibleLinesChanged()`. Monaco's
  `_updateConfigurationViewLineCount` (the view line count option) has no LoomEditor counterpart: skip it.
- `wrapOnEscapedLineFeeds` changes need a full rebuild: Monaco reads it only in the constructor (no `setWrappingSettings`
  parameter); LoomEditor does the same — changing it at run time has no effect until the model is flushed. Document it in
  the XML doc of `EditorWrappingOptions.WrapOnEscapedLineFeeds` (behavior identical to Monaco).
- Two passes: a private helper `ComputeLineBreaks(IReadOnlyList<ModelRawChange> changes)` returns a `Queue<ModelLineProjectionData?>`
  (Monaco's `ArrayQueue`: `takeCount(n)` → dequeue `n` items into an array).
- `ViewLayout.ComputeContentWidth` wrapping branch: the minimap clause (`minimap.enabled && side === 'right'`) is written
  against a `bool MinimapOnTheRight` that is always `false` until P6-41 computes it from the minimap layout — add a
  `// Minimap: P6-41` comment.
- `Tab size` changes keep going through `_lines.SetTabSize` (exists).

## Tests

- [x] New: `WordWrapOn_LongLine_HasSeveralViewLines` (`SetViewportSize` small enough, `GetLineCount()` > model line count)
- [x] New: `Resize_ChangesTheWrappingColumn_RebuildsTheViewLines`
- [x] New: `WordWrapOff_ToOn_KeepsTheFirstVisibleModelLine` (scroll to a model line, turn wrap on, the viewport start model line
      is the same)
- [x] New: `TypingInAWrappedLine_UpdatesItsViewLines` (insert text so the line gains a view line: the view line count grows)
- [x] New: `PastingSeveralLines_EachGetsItsLineBreaks` (multi-line insert with long lines)
- [x] New: `ComputeContentWidth_ViewportWrapping_IsTheMaxLineWidth` (on `ViewLayout` directly)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `viewModelImpl.ts` row (wrapping), `viewLayout.ts` row (wrapped content width); update the remark of
      `ViewModel` ("no word wrap … before phase 6")
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Injected-text model events (P6-21); drawing wrapped lines (P6-24); the control's options (P6-26).

## Suggested commit message

`feat(view): wrap lines in the view model`
