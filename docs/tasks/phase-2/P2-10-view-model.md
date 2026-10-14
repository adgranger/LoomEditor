# P2-10 — `ViewModel` (phase 2 subset) and `ViewportData`

- **Status:** done
- **Depends on:** P2-09
- **Size:** M

## Goal

The object the renderer and the control talk to. The `ViewModel` follows the `TextModel` (lines inserted, deleted,
flushed) and keeps `ViewLayout` and the `EditorLayoutInfo` up to date. When the control is resized or the font
changes, it recomputes the layout. For each frame, it builds a `ViewportData`: the visible lines, their offsets, the
selection and the visible range. In phase 2, view lines are model lines (ADR 0005, decision 6).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewModel/viewModelImpl.ts` | 322–430 | `onDidChangeContentOrInjectedText`: **only** the calls to `viewLayout.onFlushed` / `onLinesDeleted` / `onLinesInserted`, with an identity line mapping (no line breaks, no injected text, no decorations, no view events) |
| `src/vs/editor/common/viewLayout/viewLinesViewportData.ts` | 14–74 | `ViewportData` (fields and constructor; **not** `getViewLineRenderingData`, `getViewLineContinuesWithWrappedLine`, `getDecorationsInViewport`) |
| `src/vs/editor/common/config/editorOptions.ts` | 2710–2724 | how `EditorLayoutInfoComputer.compute` builds its input from the environment (outer size, font, digit count, pixel ratio) |

## Files to create or change

- `src/LoomEditor.View/Layout/ViewModel.cs` — create
- `src/LoomEditor.View/Layout/ViewportData.cs` — create
- `src/LoomEditor.View/Layout/README.md` — update: what is in the folder now, and that view lines = model lines until phase 6
- `tests/LoomEditor.View.Tests/Layout/ViewModelTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class ViewModel : IDisposable` in `LoomEditor.View.Layout` (ADR 0003 keeps Monaco's name).
  Constructor: `ViewModel(TextModel model, FontInfo fontInfo, EditorLayoutOptions layoutOptions, ViewLayoutOptions viewLayoutOptions, Func<Action, IDisposable> scheduleAtNextAnimationFrame, TimeProvider timeProvider)`.
- Members:
  - `TextModel Model`, `ViewLayout ViewLayout`, `EditorLayoutInfo LayoutInfo`, `FontInfo FontInfo`.
  - `void SetViewportSize(int outerWidth, int outerHeight)`, `void SetFontInfo(FontInfo fontInfo)`,
    `void SetOptions(EditorLayoutOptions layoutOptions, ViewLayoutOptions viewLayoutOptions)`. Each recomputes
    `LayoutInfo` with `EditorLayoutInfoComputer.ComputeLayout` and forwards the changes to `ViewLayout`
    (`OnLayoutInfoChanged`, `OnFontInfoChanged`, `OnOptionsChanged`, and `LinesLayout.SetDefaultLineHeight` through
    `ViewLayout` when the line height changes).
  - `event EventHandler? LayoutInfoChanged`.
  - `ViewportData GetViewportData(Selection selection)`: uses `ViewLayout.GetLinesViewportData()` and
    `GetWhitespaceViewportData()`.
- Subscribes to `TextModel.RawContentChanged` (P1-23):
  `ModelRawFlush` → `ViewLayout.OnFlushed(model.LineCount, [])`;
  `ModelRawLinesDeleted` → `OnLinesDeleted(FromLineNumber, ToLineNumber)`;
  `ModelRawLinesInserted` → `OnLinesInserted(FromLineNumber, FromLineNumber + Count - 1)`;
  `ModelRawLineChanged`, `ModelRawEolChanged` → nothing (no line mapping yet).
  Then `ViewLayout.OnHeightMaybeChanged()`. If the number of digits of `LineCount` changed, recompute `LayoutInfo`
  (Monaco recomputes the layout when `lineNumbersDigitCount` changes).
- `ViewportData` is an `internal sealed class` with `Selections` (`IReadOnlyList<Selection>`, one item for now),
  `StartLineNumber`, `EndLineNumber`, `RelativeVerticalOffset`, `VisibleRange`, `BigNumbersDelta`, `LineHeight`,
  `WhitespaceViewportData`. `VisibleRange` uses `model.GetLineMinColumn` / `GetLineMaxColumn` like Monaco.
- The `ViewModel` does **not** go into `IEditorContext` yet (only add a member when a feature needs it). The control
  owns it (P2-17).
- `Dispose` unsubscribes from the model and disposes `ViewLayout`.

## Tests

- [x] New: inserting 10 lines through `Model.PushEditOperations` grows `ViewLayout.GetLinesTotalHeight()` by `10 × lineHeight`
- [x] New: deleting lines shrinks it; `SetValue` (flush) resets it to the new line count
- [x] New: going from 99,999 to 100,000 lines raises `LayoutInfoChanged` and widens `LineNumbersWidth` by one `MaxDigitWidth` (rounded)
- [x] New: `SetViewportSize(800, 600)` then scrolling to line 500 → `GetViewportData(...)` starts at line 500, `VisibleRange.StartLineNumber == 500`
- [x] New: after `Dispose`, edits to the model no longer reach the layout

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `viewModel/viewModelImpl.ts` (layout part only, identity mapping) and `viewLinesViewportData.ts` (in progress, P2-10)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Projections, word wrap, injected text, view events, cursors in the view model (phase 3), decorations. Drawing.

## Suggested commit message

`feat(view): add the phase 2 ViewModel and ViewportData`
