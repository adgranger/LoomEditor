# P6-49 — The minimap model: `IMinimapModel` over the view model

- **Status:** done
- **Depends on:** P6-47, P6-48
- **Size:** M

## Goal

The minimap reads the document through one object (Monaco's `Minimap` view part, model side): line count and lines with
sampling applied, selections and minimap decorations mapped to minimap lines, reveal / scroll requests, and the options
re-created when the layout, the theme or the font change.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/browser/viewParts/minimap/minimap.ts`.

| Lines | What to port |
|---|---|
| 561–577 | `interface IMinimapModel` (without the section header members) |
| 812–862 | `class Minimap`: fields, constructor, `_onOptionsMaybeChanged` |
| 866–953 | the event handlers: what each one does to the sampling state and the inner minimap (see decisions) |
| 992–1015 | `_recreateLineSampling` |
| 1017–1070 | `getLineCount`, `getRealLineCount`, `getLineContent`, `getLineMaxColumn`, `getMinimapLinesRenderingData`, `getSelections` |
| 1072–1110 | `getMinimapDecorationsInViewport`, `_getMinimapDecorationsInViewport` |
| 1126–1150 | `getOptions`, `revealLineNumber`, `setScrollTop` |

## Files to create or change

- `src/LoomEditor.View/Minimap/IMinimapModel.cs` — create (header `minimap.ts (IMinimapModel)`)
- `src/LoomEditor.View/Minimap/MinimapModel.cs` — create (header `minimap.ts (Minimap)`): `internal sealed class MinimapModel : IMinimapModel, IDisposable`
- `src/LoomEditor.View/Minimap/IMinimapRenderTarget.cs` — create: the calls the model makes on Monaco's `InnerMinimap`
  (`OnDidChangeOptions`, `OnFlushed`, `OnLinesChanged`, `OnLinesDeleted`, `OnLinesInserted`, `OnTokensChanged`, `OnSelectionChanged`,
  `OnDecorationsChanged`, `OnThemeChanged`) so P6-50 … P6-52 implement it
- `tests/LoomEditor.View.Tests/Minimap/MinimapModelTests.cs` — create

## Design decisions (already made — do not change)

- **View events:** LoomEditor has no `ViewLinesChanged/Inserted/Deleted/Flushed` view events (only cursor, reveal, decorations, tokens).
  `MinimapModel` subscribes to `TextModel.RawContentChanged` and `InjectedTextChanged` and to the view model's line mapping changes, and
  treats any of them as Monaco's `onFlushed` (recompute the sampling from scratch, `IMinimapRenderTarget.OnFlushed()`); tokens use the
  existing `ViewTokensChangedEvent` (ranges → `OnTokensChanged`); cursor and decorations events map one to one. Behavior difference
  (PORTING): edits re-render all minimap lines instead of only the changed ones. Do **not** add new view events.
- `revealLineNumber` → `ViewModel.RevealRange` with the same reveal type Monaco uses (read line 1130–1141); `setScrollTop` →
  `ViewLayout.SetScrollPosition(… , ScrollType.Immediate)`.
- The options are rebuilt by `UpdateOptions(EditorTheme theme)` called by the renderer each frame; it returns `true` when
  `MinimapOptions.EqualsOptions` says they changed (Monaco's `_onOptionsMaybeChanged`), and then recreates the sampling.
- Selections: `GetSelections()` maps view selections to minimap lines when sampling, cached until the next cursor event, like
  `_minimapSelections`.

## Tests

- [x] New: `GetLineCount_Sampling_IsTheSampleCount`; `…_NotSampling_IsTheViewLineCount`
- [x] New: `GetMinimapLinesRenderingData_Sampling_ReadsTheSampledLines`
- [x] New: `GetMinimapDecorationsInViewport_Sampling_MapsToMinimapLines`
- [x] New: `ContentChange_FlushesTheRenderTarget` (a fake `IMinimapRenderTarget` records the calls)
- [x] New: `UpdateOptions_SameOptions_ReturnsFalse`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `minimap.ts` row (+ the model part); behavior difference (flush on edits)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Rendering lines (P6-50), decorations (P6-51), Win2D (P6-52).

## Suggested commit message

`feat(view): add the minimap model`
