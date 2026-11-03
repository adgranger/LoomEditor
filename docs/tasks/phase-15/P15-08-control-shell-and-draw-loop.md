# P15-08 — The `LoomEditor` control: template, draw loop, font, zoom, schedulers

- **Status:** done
- **Depends on:** P15-07
- **Size:** M

**Recommended model:** Sonnet

## Goal

`<loom:LoomEditor Text="…" />` in an Avalonia window shows the text with highlighting and the theme, read-only for now (no input yet). The font, the zoom and DPI
changes re-measure and redraw.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/LoomEditor.cs`, `LoomEditor.Font.cs`, `LoomEditor.FontOptions.cs`, `LoomEditor.Zoom.cs`, `Controls/AnimationFrameScheduler.cs`,
`Threading/DispatcherQueueEditorScheduler.cs`, `Threading/DispatcherQueueUiDispatcher.cs`, `Themes/Generic.xaml` (the `LoomEditor` style). Also read
[ADR 0005](../../adr/0005-viewport-rendering.md) (viewport-sized canvas) and [phase-15/README.md](README.md) decisions 2, 3, 5, 8, 9.

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/LoomEditor.cs`, `LoomEditor.Font.cs`, `LoomEditor.FontOptions.cs`, `LoomEditor.Zoom.cs`, `AnimationFrameScheduler.cs` — create (twins)
- `src/LoomEditor.Avalonia/Controls/EditorCanvas.cs` — create: `internal sealed class EditorCanvas : Control`. `Render(DrawingContext)` calls an `Action<DrawingContext, Size>`
  set by the editor; `ClipToBounds = true`. It replaces `CanvasControl` (`PART_Canvas`)
- `src/LoomEditor.Avalonia/Threading/DispatcherEditorScheduler.cs`, `DispatcherUiDispatcher.cs` — create (twins of the `DispatcherQueue…` files, on `Dispatcher.UIThread`)
- `src/LoomEditor.Avalonia/Themes/Generic.axaml` — the `ControlTheme` of `LoomEditor`: `Border` > `Panel` with `PART_Canvas` (`EditorCanvas`), `PART_ZoneWidgetCanvas` (`Canvas`),
  `PART_OverlayCanvas` (`Canvas`), `PART_VerticalScrollBar`, `PART_HorizontalScrollBar` (`ScrollBar`, `IsVisible=False`). `PART_FindWidget` is added in P15-17
- `docs/testing/phase-15-manual-tests.md` — create (decision in [README](README.md) "Tests"), section "P15-08"
- `tests/LoomEditor.Avalonia.Tests/Controls/LoomEditorRenderTests.cs` — create
- `PublicAPI.Unshipped.txt`, `AvaloniaParityPending.txt` — the members of these partials

## Design decisions (already made — do not change)

- `public sealed partial class LoomEditor : TemplatedControl`; template parts are found in `OnApplyTemplate(TemplateAppliedEventArgs e)` with `e.NameScope.Find<T>`. The
  exception messages are the WinUI ones with the Avalonia type names.
- Every WinUI `DependencyProperty` of these partials becomes a `StyledProperty<T>` with the same name, default and change callback (Avalonia: `OnPropertyChanged` override that
  switches on the property, or `Changed.AddClassHandler` in the static constructor; use `AddClassHandler`, one per property, as close to the WinUI callback as possible).
- `FontFamily` and `FontSize` are Avalonia's `TemplatedControl` properties, as WinUI's `Control` ones. The default `FontFamily` comes from
  `BareFontInfo.DefaultFontFamilyForCurrentOS` (set in the control theme: Windows keeps `"Cascadia Mono, Consolas, Courier New"`).
- Redraw = `EditorCanvas.InvalidateVisual()`. Frame pacing = `AnimationFrameScheduler` on `TopLevel.RequestAnimationFrame`. Never redraw from a timer.
- `AvaloniaTextMeasurer` is recreated when the font, font options, zoom or `TopLevel.RenderScaling` change (`TopLevel.ScalingChanged`). This is the same trigger list as
  `CreateResources` in WinUI.
- The `Features`, `Model`, `Selections`, `SetSelections`, `SelectionChanged`, `Commands`, `Text` members are translated as they are in `LoomEditor.cs`.

## Tests

- [x] New: `LoomEditor_InWindow_DrawsText` (headless window 400×200, `Text = "hello"`: dark-on-light pixels in the first line box); `Text_Changed_Redraws` (frame differs);
  `FontSize_Changed_RemeasuresLineHeight` (`LineHeight` grows); `ZoomIn_GrowsLineHeight`; `Theme_Dark_BackgroundIsThemeColor` (pixel at (390,190) = theme background)
- [x] Manual "P15-08" (written, not run): on Windows, macOS and Ubuntu, the Gallery (from P15-31) shows a C# file highlighted; the font is the OS default monospace; moving the
  window to a 200 % display re-renders crisp text

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated

## Out of scope

Scroll bars and scrolling (P15-09), the other options (P15-10), any input (P15-11 on).

## Suggested commit message

`feat(avalonia): the LoomEditor control draws text with its theme, font and zoom`

## Result

- Done as written. `Theme` and `LanguageId` (the `Highlighting` partial) are P15-09's, as its card says: here the renderer uses its default theme (`EditorTheme.Dark`) and no
  highlighter runs yet, so `Theme_Dark_BackgroundIsThemeColor` checks that default.
- Not in this card (WinUI members the draw loop calls): `LayoutChanged`, scroll bars, `HasFocus`/caret, `ResetText` (the `Text` callback does its two lines inline).
- `Invalidate()` posts to the dispatcher during a render pass (Avalonia rule), see `docs/PORTING.md`.
- Manual checks "P15-08" written in `docs/testing/phase-15-manual-tests.md`, not run.
