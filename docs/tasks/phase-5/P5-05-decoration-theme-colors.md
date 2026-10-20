# P5-05 — Theme colors of the phase 5 decorations

- **Status:** done
- **Depends on:** P5-04
- **Size:** M

## Goal

Every theme color that a phase 5 decoration draws with (find, word highlight, selection highlight, bracket match,
bracket pair colorization, folding) resolves with VS Code's default when the theme does not set it, including the
defaults that are computed from other colors (`transparent`, `lessProminent`). `EditorTheme` can resolve any color id,
not only its typed properties.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/base/common/color.ts` | 8–11, 13–46, 48–159 | `roundFloat`, `RGBA`, `HSLA` (`fromRGBA`, `_hue2rgb`, `toRGBA`) |
| `src/vs/base/common/color.ts` | 256–310, 314–321, 367–370, 400–410, 453–464, 570–590 | `Color`: constructor (RGBA or HSLA), `rgba`, `hsla`, `getRelativeLuminance`, `_relativeLuminanceForComponent`, `isLighterThan`, `isDarkerThan`, `lighten`, `darken`, `transparent`, `getLighterColor`, `getDarkerColor` |
| `src/vs/platform/theme/common/colorUtils.ts` | 262–271, 299–313, 327–329, 343–345, 352–366 | `executeTransform` (only `Transparent` and `LessProminent`), `transparent`, `lessProminent`, `resolveColorValue` |
| `src/vs/platform/theme/common/colors/editorColors.ts` | 133–139, 148–178 | `editor.selectionHighlightBackground`, `editor.selectionHighlightBorder`, `editor.findMatchBackground`, `editor.findMatchForeground`, `editor.findMatchHighlightBackground`, `editor.findMatchHighlightForeground`, `editor.findRangeHighlightBackground`, `editor.findMatchBorder`, `editor.findMatchHighlightBorder`, `editor.findRangeHighlightBorder` |
| `src/vs/editor/common/core/editorColorRegistry.ts` | 17–18, 57–59, 79–86 | `editor.rangeHighlightBackground`, `editor.rangeHighlightBorder`, `editorBracketMatch.background`, `.border`, `.foreground`, `editorBracketHighlight.foreground1` … `foreground6`, `editorBracketHighlight.unexpectedBracket.foreground` |
| `src/vs/editor/contrib/wordHighlighter/browser/highlightDecorations.ts` | 14–19 | `editor.wordHighlightBackground`, `…StrongBackground`, `…TextBackground`, `…Border`, `…StrongBorder`, `…TextBorder` |
| `src/vs/editor/contrib/folding/browser/foldingDecorations.ts` | 17–19 | `editor.foldBackground`, `editor.foldPlaceholderForeground`, `editorGutter.foldingControlForeground` |
| `src/vs/platform/theme/common/colors/baseColors.ts` | 33–35 | `icon.foreground` |
| `src/vs/base/test/common/color.test.ts` | 14–76, 158–207 | the tests listed below |

## Files to create or change

- `src/LoomEditor.Themes/Color.cs` — create (internal `Color`, `Rgba`, `Hsla`: the port of `color.ts` above)
- `src/LoomEditor.Themes/ColorValue.cs` — create (internal: a default color value, see below)
- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: the table holds `ColorValue?`; the new rows; the two transforms
- `src/LoomEditor.Themes/EditorTheme.cs` — change: internal `Colors` and `GetColor(string colorId)`
- `src/LoomEditor.Themes/LoomEditor.Themes.csproj` — change: `InternalsVisibleTo` `LoomEditor.Rendering` and `LoomEditor.Rendering.Tests`
- `tests/LoomEditor.TextMate.Tests/Themes/ColorTests.cs` — create (the Themes tests live there, see the csproj comment)
- `tests/LoomEditor.TextMate.Tests/Themes/EditorColorDefaultsTests.cs` — change (new tests below)

## Design decisions (already made — do not change)

- **`Color`** keeps Monaco's model: `Rgba` (`int R, G, B` clamped to 0–255 and truncated like `| 0`, `double A`
  rounded to 3 decimals) and `Hsla` (`int H` truncated, `double S, L, A` rounded to 3 decimals) are
  `readonly record struct`s; `Color` is a `sealed class` built from an `Rgba` or an `Hsla` (it keeps the `Hsla` it was
  built from, like Monaco's `_hsla`). `roundFloat` and every `Math.round` use `Math.Floor(x + 0.5)` (phase-3 README,
  trap 1). Conversions: `Color.FromThemeColor(ThemeColor)` (alpha / 255) and `ToThemeColor()` (alpha × 255 with the
  same rounding).
- **`ColorValue`** (Monaco: `ColorValue`): `internal abstract record ColorValue` with three cases — a hex color or a
  color id (`implicit operator ColorValue(string)`, so the existing rows such as `"#1E1E1E"` and `"foreground"` keep
  compiling), `ColorValue.Transparent(ColorValue value, double factor)` and
  `ColorValue.LessProminent(ColorValue value, ColorValue background, double factor, double transparency)`. The table
  type becomes `(ColorValue? Dark, ColorValue? Light, ColorValue? HcDark, ColorValue? HcLight)`.
- **Resolution** (`resolveColorValue` + `executeTransform`): a transform resolves its operands with the same theme, and
  returns `null` when its value resolves to `null`. Results go through `Color` and back to `ThemeColor`.
- **New rows** (one line each, with the upstream file and line in a comment, like the existing rows). A
  `registerColor(id, '#hex')` or `registerColor(id, otherId)` with a single value means the same value for the four types.
  `Color.white` / `Color.black` are `#FFFFFF` / `#000000`; `new Color(new RGBA(255, 18, 18, 0.8))` is `#FF1212CC`,
  `new Color(new RGBA(255, 50, 50, 1))` is `#FF3232`; `activeContrastBorder` is the id `contrastActiveBorder`;
  `editorSelectionBackground` / `editorBackground` / `iconForeground` / `wordHighlightBackground` / `wordHighlightBorder`
  are the ids `editor.selectionBackground`, `editor.background`, `icon.foreground`, `editor.wordHighlightBackground`,
  `editor.wordHighlightBorder`. All the ids of the sources table, plus `icon.foreground`.
- **`EditorTheme`**: `internal IReadOnlyDictionary<string, ThemeColor> Colors { get; init; }` (empty by default;
  `Load` sets it to the colors of the theme files) and
  `internal ThemeColor? GetColor(string colorId) => EditorColorDefaults.Resolve(colorId, Type, …Colors…)`. The typed
  public properties do not change.
- No new public API.

## Tests

- [x] Port (`color.test.ts` › `Color`): `isLighterColor`, `getLighterColor`, `isDarkerColor`, `getDarkerColor`,
  `luminance`; › `HSLA`: `HSLA.toRGBA`, `HSLA.fromRGBA`. `Color.fromHex` in these tests → `Color.FromThemeColor(ThemeColor.Parse(…))`.
  Not ported: `blending`, `toString`, `toNumber32Bit`, `HSVA`, `Format`, `ensureContrastRatio` (not needed).
- [x] New: `Resolve_Transparent_MultipliesAlpha` (`editor.foldBackground` of a dark theme without colors =
  `#264F78` with alpha 0.3 → `#264F784D`)
- [x] New: `Resolve_Transparent_UsesTheThemesValueOfTheOperand` (a theme that sets `editor.selectionBackground`)
- [x] New: `Resolve_LessProminent_DarkTheme_IsTheDarkerColorMadeTransparent` (expected value computed in the test with
  `Color.GetDarkerColor(#264F78, #1E1E1E, 0.3).Transparent(0.6)`; and a light-theme twin with `GetLighterColor` /
  `GetDarkerColor` as Monaco chooses)
- [x] New: `Resolve_NullDefault_IsNull` (`editor.findMatchForeground`, `editorBracketMatch.foreground`)
- [x] New: `GetColor_DarkModern_ReturnsTheThemeValueElseTheDefault` (`EditorTheme.Dark`; an id the theme sets and one it does not)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green (the golden color tests of phase 4 untouched)
- [x] `docs/PORTING.md` map updated (`color.ts` subset, `colorUtils.ts` transforms)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Which class name draws with which color (P5-08). The other `colorUtils.ts` transforms (`darken`, `lighten`, `mix`,
`opaque`, `oneOf`, `ifDefinedThenElse`) and `HSVA`.

## Suggested commit message

`feat(themes): add the default colors of find, highlights, brackets and folding`
