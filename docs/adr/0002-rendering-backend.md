# ADR 0002 — Rendering backend: Win2D, behind `IRenderSurface`

- **Status:** accepted
- **Date:** 2026-10-13

## Context

LoomEditor draws its text itself (like Monaco draws into the DOM). On Windows the text engine is DirectWrite and
the 2D engine is Direct2D. Three ways to reach them from C#:

| Option | What it is | AOT / trimming | Effort | Control |
|---|---|---|---|---|
| **A. Win2D** (`Microsoft.Graphics.Win2D` 1.4.0) | Microsoft's WinRT wrapper over Direct2D/DirectWrite, with XAML controls (`CanvasControl`, `CanvasVirtualControl`, `CanvasSwapChainPanel`) | **Verified clean**: a WinUI app using `CanvasControl`, `CanvasTextFormat`, `CanvasTextLayout` publishes with `PublishAot=true` and 0 warnings, and runs | Low | Good; native interop available for advanced cases |
| B. CsWin32 | Source-generated P/Invoke + COM wrappers for `d2d1.h` / `dwrite.h` | AOT-friendly (source generated) | High: device setup, swap chain, `ISwapChainPanelNative`, device-lost handling, all by hand | Full |
| C. Vortice.Windows (`Vortice.Direct2D1` 3.8.x) | Hand-written/SharpGen .NET bindings for DirectX | Generally AOT-friendly, not verified here | Medium–high: same plumbing as B | Full |

## Decision

**Use Win2D (option A)**, and keep it behind a small interface:

- `LoomEditor.Rendering/IRenderSurface.cs` — the only drawing API used outside the `Win2D/` folder.
- `LoomEditor.Rendering/Win2D/` — the only folder that uses Win2D types (`Win2DRenderSurface`, `Win2DFont`).
- `LoomEditor.WinUI` hosts a `CanvasControl` (`PART_Canvas` in `Themes/Generic.xaml`).

## Why

1. **Maintainability first** (project rule): Win2D removes hundreds of lines of DirectX plumbing that an
   intermediate C# developer would have to understand (device creation and loss, swap chains, DPI, COM lifetimes).
2. **AOT verified**: see ADR 0001.
3. **Same engine underneath**: Win2D *is* Direct2D + DirectWrite, so text quality and performance are the same.
4. **Escape hatch**: if profiling (phase 2, "100 MB file stays fluid") shows Win2D is the bottleneck, a second
   `IRenderSurface` implementation using CsWin32 or Vortice can be added *without touching* `TextViewRenderer`,
   View or Core. Win2D also exposes the underlying native objects for targeted optimizations.

## Consequences

- `LoomEditor.Rendering` and `LoomEditor.WinUI` reference `Microsoft.Graphics.Win2D`.
- Phase 2 will likely switch from `CanvasControl` to `CanvasVirtualControl` (virtualized drawing for very large
  documents) — an internal change of the WinUI project.
- Phase 0 measures a single character to lay out text (monospaced fonts only). Phase 2 replaces it with per-line
  `CanvasTextLayout` / glyph runs (tabs, wide characters, ligatures, proportional fonts).
- Superseded in part by [ADR 0005](0005-viewport-rendering.md): there is no `CanvasVirtualControl`; the control draws
  only the visible lines on a viewport-sized `CanvasControl`.
