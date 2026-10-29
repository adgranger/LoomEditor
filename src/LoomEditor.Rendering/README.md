# LoomEditor.Rendering

## Install

```powershell
dotnet add package LoomEditor.Rendering --prerelease
```

`--prerelease` is needed while the version has a suffix (for example `0.1.0-alpha.1`). Most applications want
`LoomEditor.WinUI`, which brings this package and the others.

## What it is

Draws the viewport of a `ViewModel` on an `IRenderSurface`. It knows Win2D (through `Win2D/`) but nothing
about XAML controls: the `LoomEditor` control in `LoomEditor.WinUI` owns the canvas, the timer and the focus.

## What is here (phase 2)

- `TextViewRenderer` (+ `.Gutter.cs`): draws only the visible lines at the scroll position (offsets are computed in
  `int` relative to the viewport, see ADR 0005), then the selection, the current line highlight, the caret, and the
  gutter with the line numbers. It reports the widest line it drew to `ViewLayout.SetMaxLineWidth`.
  `OnFontChanged` swaps the measurer and clears the cache after a font, tab size or DPI change.
- `LineLayoutCache`: least-recently-used cache of line layouts, keyed by the text of the line.
- `ITextMeasurer`, `ILineLayout`, `IRenderSurface`: what the renderer needs from a backend. `Win2D/` implements them
  with DirectWrite (`Win2DTextMeasurer`, `Win2DLineLayout`, `Win2DRenderSurface`); the tests use fakes.
- `CursorBlinking`, `TextEditorCursorBlinkingStyle`: the caret blinking state machine. It has no timer; the control
  ticks it.
- `OverflowLabel`: the "Show more" label of lines longer than 10,000 characters.
- `RenderLineHighlight`: which parts of the current line are highlighted.

## Rules

- Lines are laid out by DirectWrite; nothing assumes a fixed character width.
- Everything here runs on the UI thread. The classes say so in their `<remarks>`.

Monaco sources: `src/vs/editor/browser/viewParts/` (`viewLines`, `lineNumbers`, `currentLineHighlight`,
`viewCursors`). The Monaco → LoomEditor map is in [docs/PORTING.md](https://github.com/adgranger/LoomEditor/blob/main/docs/PORTING.md).
