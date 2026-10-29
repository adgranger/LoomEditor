# How to set the font, zoom and whitespace options

All of these are `DependencyProperty` values of `LoomEditor` (design: [ADR 0013](../adr/0013-text-options-and-snippets.md)). Each editor has its own values.

| Property | Default | What it does |
|---|---|---|
| `FontFamily`, `FontSize` | the control's | the font (as before) |
| `FontWeight` | normal | the inherited property; only `wght` of a variable font is applied |
| `LetterSpacing` | `0` | extra space after each character, in DIPs (included in the width table) |
| `FontLigatures` | `false` | `true` turns `liga` and `calt` on, `false` turns them off explicitly |
| `FontFeatureSettings` | `""` | CSS syntax, e.g. `'ss01' 1, "calt" 0`; when set it replaces `FontLigatures` |
| `FontVariationSettings` | `""` | parsed and stored; only `wght` is applied (Win2D has no variation axes API) |
| `ZoomLevel` | `0` | -5 to 20, each step is 10 % of the font size; `Ctrl` + wheel changes it when `MouseWheelZoom` is `true` (default `false`) |
| `RenderWhitespace` | `None` | `None`, `Boundary`, `Selection`, `Trailing`, `All`: dots for spaces, arrows for tabs |
| `RenderControlCharacters` | `true` | control characters drawn as control pictures (U+2400…) instead of nothing |
| `CursorStyle`, `OvertypeCursorStyle` | `Line`, `Block` | the six Monaco styles; the `Insert` key (or `InputMode`) toggles overtype |
| `DragAndDrop` | `true` | drag the selected text with the mouse (`Ctrl` copies, `Escape` cancels) |

```xml
<loom:LoomEditor FontFamily="Cascadia Code" FontLigatures="True" RenderWhitespace="Boundary" MouseWheelZoom="True" />
```

**Fonts with ligatures.** Cascadia Code (installed with Windows Terminal) has them; `=>`, `!=`, `->`, `<=`, `>=`, `==` and `|>` fuse into one glyph. The caret crosses a ligature in as many
steps as it has characters (DirectWrite shares the advance equally; Monaco does the same). With ligatures on, word wrap is an approximation. Ligature rendering was checked with Cascadia Code only.

Try everything in the Gallery: the samples "Fonts and ligatures", "Zoom", "Whitespace and control characters" and "Overtype and cursor styles".
