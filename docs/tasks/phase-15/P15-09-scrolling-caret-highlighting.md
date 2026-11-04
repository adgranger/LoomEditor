# P15-09 — Scrolling, caret, highlighting, read-only, reveal

- **Status:** done
- **Depends on:** P15-08
- **Size:** M

**Recommended model:** Sonnet

## Goal

The Avalonia `LoomEditor` scrolls (scroll bars, mouse wheel, smooth scrolling), blinks its caret, switches languages and themes, honours `IsReadOnly` and reveals positions,
exactly as the WinUI control.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/LoomEditor.Scrolling.cs`, `LoomEditor.Caret.cs`, `LoomEditor.Highlighting.cs`, `LoomEditor.ReadOnly.cs`, `LoomEditor.Reveal.cs`.
Also read [ADR 0005](../../adr/0005-viewport-rendering.md) (Monaco's `Scrollable`, XAML scroll bars).

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/LoomEditor.Scrolling.cs`, `LoomEditor.Caret.cs`, `LoomEditor.Highlighting.cs`, `LoomEditor.ReadOnly.cs`, `LoomEditor.Reveal.cs` — create (twins)
- `tests/LoomEditor.Avalonia.Tests/Controls/LoomEditorScrollingTests.cs`, `LoomEditorCaretTests.cs` — create
- `docs/testing/phase-15-manual-tests.md` — section "P15-09"
- `PublicAPI.Unshipped.txt`, `AvaloniaParityPending.txt`

## Design decisions (already made — do not change)

- Scroll bars: Avalonia `ScrollBar` (`Minimum`, `Maximum`, `ViewportSize`, `Value`, `Scroll` event). `AllowAutoHide = true` stands for WinUI's `IndicatorMode="MouseIndicator"`.
- Wheel: `PointerWheelChanged`. `e.Delta` is in lines (Avalonia gives ±1 per notch). Convert it to Monaco's wheel delta as the WinUI twin converts `MouseWheelDelta / 120`. On macOS
  trackpads `e.Delta` is fractional and continuous: pass it through unchanged (no notch rounding).
- Caret blink timer: `DispatcherTimer` on the UI thread, same periods as the WinUI twin (`CursorBlinking.cs` in Rendering).
- Highlighting switches through the same View calls; only the theme-change trigger changes: `ActualThemeVariantChanged` stands for `ActualThemeChanged`.

## Tests

- [x] New: `MouseWheel_Down_ScrollsByLines` (100-line text, one wheel notch: `ScrollTop` grows by Monaco's amount); `VerticalScrollBar_Value_FollowsScrollTop`;
  `RevealLine_Far_ScrollsIntoView`; `Caret_Blinks_FramesAlternate` (two frames 600 ms apart through `AvaloniaHeadlessPlatform.ForceRenderTimerTick` differ at the caret
  pixel); `ReadOnly_Typing_DoesNotEdit` (once P15-11 exists, mark with `// completed in P15-11` and assert on `Model` after a command call here)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated

## Out of scope

Pointer selection (P15-12), keyboard (P15-11).

## Suggested commit message

`feat(avalonia): scrolling, caret, highlighting and read-only mode`

## Result

- Done as written, plus two members the WinUI twins have in the partials of this card's neighbours: `TryZoomFromWheel` (in `LoomEditor.Zoom.cs`, called by the wheel code) and the
  `MouseWheelClassifier` and `MouseWheelOptions` fields. The wheel delta is passed through unchanged (Avalonia gives one unit per notch, as `MouseWheelDelta / 120`).
- `Theme` is declared `new`: `StyledElement.Theme` (a `ControlTheme`) already exists in Avalonia. XAML `Theme=` therefore still sets the control theme, never the `EditorTheme`.
- `UpdateScrollBars` and `Invalidate` post to the dispatcher during a render pass (Avalonia refuses layout and visual invalidation there).
- `HasFocus` is `IsFocused` (the control itself, not the find widget's children: P15-17 revisits it). The Windows high contrast theme and `LayoutChanged`/`ScrollChanged` come with P15-14 and P15-16.
- Tests need `HeadlessSession.RunAsync(Func<Task>)` so that `DispatcherTimer` ticks while a test awaits (the caret blink test).
- Manual checks "P15-09" written in `docs/testing/phase-15-manual-tests.md`, not run.
