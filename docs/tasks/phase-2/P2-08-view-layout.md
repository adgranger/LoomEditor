# P2-08 — `ViewLayout`: scroll dimensions of the editor

- **Status:** done
- **Depends on:** P2-07
- **Size:** M

## Goal

Port Monaco's `ViewLayout`. It joins `LinesLayout` (vertical offsets) and `Scrollable` (scroll position) for an editor:
content width and height, "scroll beyond last line", the current viewport, `setScrollPosition` (immediate or
smooth), and the visible lines for the current scroll position.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewLayout/viewLayout.ts` | 17 | `SMOOTH_SCROLLING_TIME = 125` |
| same | 19–71 | `EditorScrollDimensions` |
| same | 73–154 | `EditorScrollable` |
| same | 156–490 | `ViewLayout` (all members; see Design decisions for the configuration) |
| `src/vs/editor/common/editorCommon.ts` | 216–219 | `ScrollType` |
| `src/vs/editor/common/viewModelEventDispatcher.ts` | 214–245 | `ContentSizeChangedEvent` (raised when content width/height change) |
| `src/vs/editor/common/config/editorOptions.ts` | 6690–6696 | `scrollBeyondLastColumn` (4), `scrollBeyondLastLine` (true) defaults |
| same | 6779–6781 | `smoothScrolling` default (false) |

## Files to create or change

- `src/LoomEditor.View/Layout/ViewLayout.cs` — create (`EditorScrollDimensions` and `EditorScrollable` as private nested classes, or in `ViewLayout.Scrollable.cs`)
- `src/LoomEditor.View/Layout/ViewLayoutOptions.cs` — create
- `src/LoomEditor.View/Layout/ScrollType.cs`, `ContentSizeChangedEventArgs.cs` — create
- `tests/LoomEditor.View.Tests/Layout/ViewLayoutTests.cs` — create

## Design decisions (already made — do not change)

- `internal sealed class ViewLayout : IDisposable`. Events: `event EventHandler<ScrollEvent>? Scroll` (Monaco
  `onDidScroll`) and `event EventHandler<ContentSizeChangedEventArgs>? ContentSizeChanged`.
- Monaco reads `IEditorConfiguration`. There is no options system yet, so `ViewLayout` gets what it reads:
  - `internal sealed record ViewLayoutOptions(int LineHeight, int PaddingTop, int PaddingBottom, bool ScrollBeyondLastLine, int ScrollBeyondLastColumn, bool SmoothScrolling, ScrollbarVisibility HorizontalScrollbarVisibility, int HorizontalScrollbarSize, bool IgnoreHorizontalScrollbarInContentHeight)`
    with Monaco's defaults (`ScrollBeyondLastLine = true`, `ScrollBeyondLastColumn = 4`, `SmoothScrolling = false`,
    padding 0, `Auto`, 12, `false`);
  - the parts of `EditorLayoutInfo` (P2-07) it reads: `ContentWidth`, `Height`, `VerticalScrollbarWidth`;
  - `FontInfo.TypicalHalfwidthCharacterWidth` (P2-06).
- `onConfigurationChanged(e)` becomes three methods: `OnOptionsChanged(ViewLayoutOptions)`,
  `OnLayoutInfoChanged(EditorLayoutInfo)`, `OnFontInfoChanged(FontInfo)`. Each does what the matching
  `e.hasChanged(...)` branch of Monaco does (lines 209–236).
- Word wrap is never on in phase 2: in `_computeContentWidth`, port only the `else` branch (lines 330–332) and leave
  a comment pointing to the wrapping branch for phase 6.
- The constructor takes `Func<Action, IDisposable> scheduleAtNextAnimationFrame` and a `TimeProvider`, which it passes
  to `Scrollable` (P2-02).
- `saveState`, `changeWhitespace`, `changeSpecialLineHeights`, `setOverlayWidgetsMinWidth` are ported (they are small and
  delegate to `LinesLayout`).

## Tests

Monaco has no `viewLayout.test.ts`. New tests:

- [x] New: 100 lines × 20 DIPs, viewport 500 high, `ScrollBeyondLastLine = true` → `GetContentHeight() == 2000 + 500 - 20`; with `false` → 2000
- [x] New: `SetScrollPosition(new(ScrollTop: 1e9), ScrollType.Immediate)` clamps to `ScrollHeight - Height`
- [x] New: `SetMaxLineWidth(800)` with a 600-wide content area → content width `800 + 4 × typicalHalfwidth + verticalScrollbarWidth`,
      and `ContentSizeChanged` is raised once
- [x] New: `OnLinesInserted` / `OnLinesDeleted` update the content height
- [x] New: `GetLinesViewportData()` after scrolling to line 1,000 starts at line 1,000
- [x] New: with `SmoothScrolling = true`, `SetScrollPosition(..., ScrollType.Smooth)` animates with a fake scheduler and clock (P2-02 helpers)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `editor/common/viewLayout/viewLayout.ts` ✅ ported (no word-wrap branch, explicit options instead of `IEditorConfiguration`)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Listening to the text model (P2-10); mouse wheel (P2-09); any UI.

## Suggested commit message

`feat(view): port ViewLayout`
