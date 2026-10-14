# P2-20 — Control: font, tab size and DPI changes

- **Status:** todo
- **Depends on:** P2-19
- **Size:** S

## Goal

The editor reacts at run time to a change of font family, font size or tab size, and to a DPI change: the window
moves to a monitor with another scale, or the user changes the scale. The font is measured again, the layout cache
is cleared, the layout is recomputed, and the scroll position is kept on the same line.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewLayout/viewLayout.ts` | 209–236 | `onConfigurationChanged`: line height, layout info (already ported in P2-08 as `OnFontInfoChanged` / `OnLayoutInfoChanged`) |
| same | 361–371 | `saveState`: `scrollTopWithoutViewZones` (used to keep the first visible line) |
| `src/vs/editor/browser/viewParts/viewLines/viewLines.ts` | 188–237 | `onConfigurationChanged` / `_onOptionsMaybeChanged`: font change → every rendered line is invalid, `_maxLineWidth = 0` |

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.Font.cs` — create (partial)
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — `TabSize` dependency property
- `src/LoomEditor.WinUI/Themes/Generic.xaml` — `FontFamily` and `FontSize` setters on the default style
- `src/LoomEditor.Rendering/TextViewRenderer.cs` — `OnFontChanged(ITextMeasurer)`: replaces the measurer, clears the cache, resets `_maxLineWidth`
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — `TabSize` / `TabSizeProperty`
- `samples/LoomEditor.Gallery/MainWindow.xaml(.cs)` — font size `NumberBox` (8–40), a font family `ComboBox`
  (`Cascadia Mono`, `Consolas`, `Segoe UI` — a proportional font on purpose), tab size `NumberBox` (1–16)

## Design decisions (already made — do not change)

- Font family and size are the **inherited** `Control.FontFamily` and `Control.FontSize`. No new properties.
  The default style sets `FontFamily="Cascadia Mono, Consolas, Courier New"` and `FontSize="14"`, which replaces the
  phase 0 constants. Listen with `RegisterPropertyChangedCallback(FontFamilyProperty / FontSizeProperty, …)`.
  `FontFamily.Source` is the comma-separated list given to `Win2DTextMeasurer`.
- Public API: `public int TabSize` (dependency property, default 4, Monaco's `EDITOR_MODEL_DEFAULTS.tabSize` in `src/vs/editor/common/core/misc/textModelDefaults.ts` line 7;
  values < 1 throw `ArgumentOutOfRangeException` from the property-changed callback, and the old value is kept).
- DPI: subscribe to `XamlRoot.Changed` after `Loaded`. When `XamlRoot.RasterizationScale` changed, recreate the
  measurer with the new `pixelRatio`. The canvas redraws at the new DPI by itself (Win2D).
- One method does every case: `RecreateTextMeasurer()`. It disposes the old measurer, creates a new one, calls
  `ViewModel.SetFontInfo(newFontInfo)` and `renderer.OnFontChanged(measurer)`, keeps the first visible line (the line
  at the old `ScrollTop` is scrolled to the top again with the new line height), and invalidates the canvas.
  Device loss (`CreateResources`) also goes through it.
- Proportional fonts are supported (`Segoe UI` in the Gallery): nothing assumes a fixed character width since P2-12.
  `FontInfo.IsMonospace` is `false` for them. Nothing uses it yet.

## Tests

- [ ] New (Rendering.Tests): `OnFontChanged` clears the cache (a layout created before is disposed) and the next
      frame calls `SetMaxLineWidth` again
- [ ] Manual: font size 14 → 28: the text and line height double, the first visible line stays the same
- [ ] Manual: `Segoe UI`: text is proportional, selection and caret land between the right characters
- [ ] Manual: tab size 4 → 8 on a tab-indented line moves the text
- [ ] Manual: move the Gallery between a 100 % and a 150 % monitor (or change the scale in Settings): text stays sharp,
      caret width stays 2 DIPs

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, manual checks done
- [ ] `PublicAPI.Unshipped.txt` of `LoomEditor.WinUI`: only `TabSize` / `TabSizeProperty` added
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Font ligatures and weights, letter spacing, editor zoom (Ctrl+wheel), `insertSpaces` / `detectIndentation` (phase 3–5).

## Suggested commit message

`feat(winui): follow font, tab size and DPI changes`
