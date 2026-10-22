# P6-52 — Drawing the minimap with Win2D: canvas, slider, shadow, auto-hide

- **Status:** todo
- **Depends on:** P6-51, P6-45
- **Size:** M

## Goal

The renderer draws the minimap at its layout position: the lines buffer and the decorations buffer as bitmaps (90 %
opacity), the slider (visible always or on mouse-over, with hover / active colors), the shadow when the content is
scrolled horizontally, and Monaco's auto-hide modes.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/viewParts/minimap/minimap.ts` | 1152–1228 | `InnerMinimap` fields and DOM structure (lines canvas, decorations canvas above it, slider, horizontal slider part, shadow) |
| same | 1309–1320 | `_hideSoon`, `_hideImmediatelyIfMouseIsOutside` (500 ms) |
| same | 1380–1418 | `_getMinimapDomNodeClassName`, `_applyLayout` |
| same | 1436–1441, 1468–1480 | `onDidChangeOptions`, `onScrollChanged` (auto-hide on scroll), `onThemeChanged` |
| same | 1499–1538 | `render` (shadow visibility, `MinimapLayout.create`, slider position and height, then decorations and lines) |
| `src/vs/editor/browser/viewParts/minimap/minimap.css` | 6–67 | slider opacity (mouse-over), slider colors (`minimapSlider.background` / `.hoverBackground` / `.activeBackground`), shadow (6 px on the left), auto-hide opacity (0.5 s), canvas opacity 0.9 |

## Files to create or change

- `src/LoomEditor.Rendering/IRenderSurface.cs` — change: `void DrawRgbaImage(byte[] rgba, int pixelWidth, int pixelHeight, float x, float y, float width, float height, float opacity)`
  (implemented by `Win2DRenderSurface` and by the test recording surface)
- `src/LoomEditor.Rendering/Win2D/Win2DRenderSurface.cs` — change: implement it with a cached `CanvasBitmap` (see decisions)
- `src/LoomEditor.Rendering/TextViewRenderer.Minimap.cs` — create (header `minimap.ts (InnerMinimap)`): owns a `MinimapModel`, the
  two renderers of P6-50/P6-51, and draws them; called after the text and before the overview ruler
- `src/LoomEditor.Rendering/MinimapPresentationState.cs` — create: slider hover / active, mouse-over-minimap, auto-hide visibility and its
  timer (the state Monaco keeps in CSS classes)
- `tests/LoomEditor.Rendering.Tests/TextViewRendererMinimapTests.cs` — create

## Design decisions (already made — do not change)

- Bitmaps: Win2D draws premultiplied BGRA/RGBA. Upload with `CanvasBitmap.CreateFromBytes(device, bytes, w, h, DirectXPixelFormat.R8G8B8A8UIntNormalized, 96, CanvasAlphaMode.Premultiplied)`
  after converting the straight-alpha RGBA buffer to premultiplied (a pure helper in `LoomEditor.View/Minimap/`, tested). Re-create the
  bitmap only when the buffer object or its content version changed (add an `int Version` to `MinimapImageData`, incremented by the
  renderers when they paint).
- The canvas is drawn at `options.CanvasOuterWidth × CanvasOuterHeight` DIPs from `MinimapLeft`, with opacity 0.9 (CSS).
- The slider: a rectangle `MinimapWidth × SliderHeight` at `SliderTop`, color `minimapSlider.background`, `.hoverBackground` when the
  pointer is over it, `.activeBackground` while dragging (P6-53 sets those flags). `showSlider: MouseOver` draws it only while the pointer
  is over the minimap or dragging (Monaco's 100 ms opacity transition is not animated: instant). Document it.
- Shadow: when `scrollLeft + viewportWidth < scrollWidth`, a 6-DIP band left of the minimap filled with the theme's `scrollbar.shadow`
  color (Monaco's `--vscode-shadow-md` box shadow; a gradient is not required: a solid band at the shadow color's alpha is enough —
  document the difference).
- Auto-hide: `MouseOver` → visible only while hovered; `Scroll` → visible for 500 ms after a scroll (use `IEditorScheduler` /
  `RunOnceScheduler`, never a timer of your own). The 0.5 s fade is not animated (instant): document it.
- Wire the char renderer factory to `Win2DMinimapFontSampler` (P6-45) for scales above 2.

## Tests

- [ ] New: `Minimap_Disabled_DrawsNothing`
- [ ] New: `Minimap_Enabled_DrawsTwoImagesAtTheLayoutPosition`
- [ ] New: `Slider_AlwaysMode_IsDrawn`; `Slider_MouseOverMode_HiddenUntilHovered`
- [ ] New: `Shadow_DrawnOnlyWhenHorizontallyScrollable`
- [ ] New (View): `Premultiply_Converts`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `minimap.ts` row (drawing), `minimap.css` behavior differences (no animations, solid shadow)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Pointer input (P6-53); the control property (P6-54).

## Suggested commit message

`feat(rendering): draw the minimap`
