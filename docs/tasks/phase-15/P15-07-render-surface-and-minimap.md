# P15-07 — `AvaloniaRenderSurface`, bitmap cache, minimap font sampler

- **Status:** todo
- **Depends on:** P15-06
- **Size:** M

**Recommended model:** Sonnet

## Goal

`IRenderSurface` on an Avalonia `DrawingContext`. `TextViewRenderer` can draw a whole frame (text, decorations, minimap, overview ruler) into a headless bitmap.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Rendering/Win2D/Win2DRenderSurface.cs`, `Win2DBitmapCache.cs`, `Win2DMinimapFontSampler.cs`. Also read: `IRenderSurface.cs`,
`src/LoomEditor.View/Minimap/MinimapCharSheet.cs`, `MinimapSampleConversion.cs`, `TextViewRenderer.cs` (how a frame is driven), the existing `tests/LoomEditor.Rendering.Tests`
(a fake surface records calls: reuse its frame setup).

## Files to create or change

- `src/LoomEditor.Avalonia/Rendering/AvaloniaRenderSurface.cs`, `AvaloniaBitmapCache.cs`, `AvaloniaMinimapFontSampler.cs` — create (twins)
- `src/LoomEditor.View/Minimap/MinimapSampleConversion.cs` — add the conversion for the pixel format Avalonia returns, if it differs from BGRA premultiplied (check
  `RenderTargetBitmap` / `WriteableBitmap.Lock()` format; most likely `Bgra8888` premultiplied, the same, so nothing to add)
- `tests/LoomEditor.Avalonia.Tests/Rendering/AvaloniaRenderSurfaceTests.cs`, `FrameRenderTests.cs` — create

## Design decisions (already made — do not change)

- **Shapes:** `FillRectangle` → `DrawingContext.FillRectangle` with an `ImmutableSolidColorBrush`; `DrawLine` / `DrawRectangle` → an `ImmutablePen`. Brushes and pens are cached per
  `(color, width)` in a small dictionary owned by the surface's creator (one per control), never allocated per call.
- **Clear:** fill the whole bounds (Avalonia has no clear); the surface is created with its bounds.
- **Text:** `DrawLineLayout` → `AvaloniaLineLayout.TextLayout.Draw(context, new Point(x, lineTop + GlyphTopOffset))`. The `color` argument is the default foreground of a layout
  without runs (as Win2D).
- **Bitmaps:** `AvaloniaBitmapCache.Get(rgba, w, h, version)` keeps one `WriteableBitmap` (`PixelFormat.Bgra8888`, `AlphaFormat.Premul`) per array instance and re-uploads
  only when `version` changes. Converting straight RGBA → premultiplied BGRA happens at upload. `DrawImage` with `BitmapInterpolationMode.LowQuality` (linear), and opacity
  through `PushOpacity`.
- **Minimap sampler:** a `RenderTargetBitmap` of the char sheet size at 96 DPI, glyphs drawn with weight 700 and vertically centered, as the Win2D twin. Pixels are read
  with `CopyPixels`, then `MinimapSampleConversion` as today.
- Thread: UI thread only, inside `Render` (threading note in each class).

## Tests

- [ ] New: `FillRectangle_PaintsExactPixels` (red 10×10 at (5,5) on a 40×40 frame: the pixels inside are red, the ones outside are not); `DrawLine_OneDip_Visible`;
  `DrawRgbaImage_SameVersion_UploadsOnce` (counts uploads); `DrawRgbaImage_NewVersion_Reuploads`; `MinimapSampler_ProducesNonEmptySheet` (some non-zero alpha per glyph);
  `Frame_TwoLines_DrawsTextPixelsInEachLineBox` (a `TextViewRenderer` frame of 2 lines: dark pixels within the boxes of line 1 and 2, none below)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green; Avalonia tests green on the three CI jobs
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The control and its draw loop (P15-08).

## Suggested commit message

`feat(avalonia): draw editor frames on an Avalonia DrawingContext`
