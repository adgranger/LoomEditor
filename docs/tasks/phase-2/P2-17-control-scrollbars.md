# P2-17 — Control: scrollbars and scroll API

- **Status:** done
- **Depends on:** P2-16
- **Size:** M

## Goal

The control can scroll. Two XAML `ScrollBar`s are laid over the canvas and kept in sync with the `ViewLayout`, in
both directions. The control exposes `ScrollTop` and `ScrollLeft` (Monaco's `getScrollTop` / `setScrollTop`) so that
an app, and the Gallery's scroll test (P2-21), can scroll it.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/base/common/scrollable.ts` | 9–13 | `ScrollbarVisibility.Auto`: a scrollbar is shown only when the content is larger than the viewport |
| `src/vs/editor/common/config/editorOptions.ts` | 4218–4224 | scrollbar sizes (vertical 14, horizontal 12): used by the layout computation (P2-07), the XAML scrollbars overlay the canvas in that space |

The scrollbar widgets themselves are not ported (ADR 0005, decision 3).

## Files to create or change

- `src/LoomEditor.WinUI/Themes/Generic.xaml` — template: a `Grid` with `PART_Canvas`, then `PART_VerticalScrollBar`
  (`Orientation="Vertical"`, `HorizontalAlignment="Right"`) and `PART_HorizontalScrollBar` (`Orientation="Horizontal"`,
  `VerticalAlignment="Bottom"`), both `IndicatorMode="MouseIndicator"`, both over the canvas
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — split into partial files; this card adds `LoomEditor.Scrolling.cs`
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — `./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI` (only the two new properties)
- `samples/LoomEditor.Gallery/SampleText.cs` — make the sample long enough to scroll (e.g. append 200 numbered lines and one 300-character line), or add a button that does it

## Design decisions (already made — do not change)

- Public API added in this card, nothing else:
  ```csharp
  /// <summary>The vertical scroll position, in DIPs (Monaco: getScrollTop / setScrollTop).</summary>
  public double ScrollTop { get; set; }
  /// <summary>The horizontal scroll position, in DIPs (Monaco: getScrollLeft / setScrollLeft).</summary>
  public double ScrollLeft { get; set; }
  ```
  Plain CLR properties, not dependency properties (no binding loop during smooth scrolling). The setter calls
  `ViewLayout.SetScrollPosition(..., ScrollType.Immediate)`. Before the `ViewModel` exists (before the first
  `CreateResources`), the value is stored and applied once it is created.
- Scrollbar ↔ layout mapping: `Minimum = 0`, `Maximum = ScrollHeight - Height` (or `ScrollWidth - Width`),
  `ViewportSize = Height` (or `Width`), `LargeChange = ViewportSize`, `SmallChange = LineHeight` (vertical) or
  `TypicalHalfwidthCharacterWidth` (horizontal). `Visibility = Collapsed` when `Maximum <= 0` (`Auto`).
- Updates: `ViewLayout.Scroll` and `ContentSizeChanged` → update the scrollbars and `Invalidate()` the canvas. Scrollbar
  `ValueChanged` → `SetScrollPosition(Immediate)`. A `bool _isUpdatingScrollBars` flag breaks the loop.
- `SetViewportSize` gets the full canvas size. The scrollbars overlay the content, like Monaco, and the layout already
  keeps `VerticalScrollbarWidth` / `HorizontalScrollbarHeight` free (P2-07, P2-08).
- Model edits that change the content height update the scrollbars through `ContentSizeChanged`. No special code.
- The other `ScrollBar` properties keep their WinUI defaults. The control keeps its Windows look.

## Tests

The control has no automated UI tests (no WinUI test project, see P1-32). The scroll math is tested in View
(P2-08). Manual checks, written in the commit body:

- [ ] Manual: Gallery: the vertical scrollbar appears, dragging it scrolls the text, line numbers follow
- [ ] Manual: the horizontal scrollbar appears for the long line; scrolling it moves text under the gutter, not over it
- [ ] Manual: "Delete first line" until the text fits → the vertical scrollbar disappears
- [ ] Manual: resizing the window keeps the scroll position valid (no blank area at the bottom beyond "scroll beyond last line")

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, the app starts (manual checks above still to be done by a human)
- [x] `PublicAPI.Unshipped.txt` of `LoomEditor.WinUI`: only `ScrollTop` and `ScrollLeft` added
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Mouse wheel and smooth scrolling (P2-18), caret blinking (P2-19), font/DPI changes (P2-20), overview ruler (phase 5+).

## Suggested commit message

`feat(winui): add scrollbars and the ScrollTop/ScrollLeft API`
