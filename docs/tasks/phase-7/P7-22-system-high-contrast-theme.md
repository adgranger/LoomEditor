# P7-22 — The Windows system high contrast theme (`SystemHighContrastThemeFactory`)

- **Status:** todo
- **Depends on:** P7-21
- **Size:** M

## Goal

An `EditorTheme` can be built from the eight colors of the user's Windows high contrast scheme (Window, WindowText, Highlight, HighlightText, GrayText, Hotlight, ButtonFace, ButtonText), so that the editor follows the colors the user chose
(P7-23 wires it to the control). Pure logic, tested with sample palettes.

## Sources

None to port (ADR 0010 decision 10). The shape of a theme comes from the bundled `src/LoomEditor.Themes/BuiltIn/hc_black.json` / `hc_light.json` and `EditorTheme.Load(json, uiTheme, name)`.
Windows' system color meanings: Window (background), WindowText (text), Highlight / HighlightText (selected items), GrayText (disabled text), Hotlight (hyperlinks), ButtonFace / ButtonText (button surfaces).

## Files to create or change

- `src/LoomEditor.Themes/HighContrast/SystemHighContrastColors.cs` — create
- `src/LoomEditor.Themes/HighContrast/SystemHighContrastThemeFactory.cs` — create
- `tests/LoomEditor.TextMate.Tests/Themes/SystemHighContrastThemeFactoryTests.cs` — create

## Design decisions (already made — do not change)

- `internal readonly record struct SystemHighContrastColors(ThemeColor Window, ThemeColor WindowText, ThemeColor Highlight, ThemeColor HighlightText, ThemeColor GrayText, ThemeColor Hotlight, ThemeColor ButtonFace, ThemeColor ButtonText)`.
- `internal static class SystemHighContrastThemeFactory` with `static EditorTheme Create(SystemHighContrastColors colors, string name = "Windows High Contrast")`:
  1. every color is made **opaque** (`A = 255`: Win32 colors have no alpha);
  2. `uiTheme` = `"hc-black"` when `Color.FromThemeColor(Window).GetRelativeLuminance() < Color.FromThemeColor(WindowText).GetRelativeLuminance()`, else `"hc-light"` (the scheme's background is darker than its text → a dark high contrast theme);
  3. the theme JSON is **built with a `StringBuilder`** (no reflection JSON) as below, with `#RRGGBB` strings (`$"#{c.R:X2}{c.G:X2}{c.B:X2}"`), then `EditorTheme.Load(json, uiTheme, name)`;
  4. the result is returned as is (its `Type` follows the `uiTheme`).
- The JSON (`W` = Window, `T` = WindowText, `H` = Highlight, `HT` = HighlightText, `G` = GrayText, `L` = Hotlight, `BF` = ButtonFace, `BT` = ButtonText; `0` = `#00000000`):
  ```json
  {
    "name": "<name>",
    "colors": {
      "editor.background": "W", "editor.foreground": "T",
      "editor.selectionBackground": "H", "editor.selectionForeground": "HT",
      "editor.inactiveSelectionBackground": "H",
      "editor.lineHighlightBorder": "L",
      "editorCursor.foreground": "T",
      "editorLineNumber.foreground": "T", "editorLineNumber.activeForeground": "L",
      "editorWhitespace.foreground": "G",
      "editorIndentGuide.background1": "G", "editorIndentGuide.activeBackground1": "T",
      "editorBracketMatch.background": "0", "editorBracketMatch.border": "L",
      "editor.findMatchBackground": "0", "editor.findMatchBorder": "L",
      "editor.findMatchHighlightBackground": "0", "editor.findMatchHighlightBorder": "H",
      "editor.wordHighlightBackground": "0", "editor.wordHighlightBorder": "H",
      "editorGutter.foldingControlForeground": "T",
      "editorWidget.background": "BF", "editorWidget.foreground": "BT", "editorWidget.border": "BT",
      "input.background": "W", "input.foreground": "T", "input.border": "T",
      "inputOption.activeBackground": "H", "inputOption.activeForeground": "HT", "inputOption.activeBorder": "L",
      "focusBorder": "L", "errorForeground": "L"
    },
    "tokenColors": [
      { "scope": ["comment", "punctuation.definition.comment"], "settings": { "foreground": "G", "fontStyle": "italic" } },
      { "scope": ["keyword", "storage", "storage.type"], "settings": { "foreground": "T", "fontStyle": "bold" } },
      { "scope": ["markup.underline.link", "string.other.link"], "settings": { "foreground": "L", "fontStyle": "underline" } }
    ]
  }
  ```
  Every other token (strings, numbers, types…) takes the default foreground `T`: in high contrast, meaning is carried by bold / italic / underline, not by hue. A color id that `EditorColorDefaults` does not know is harmless; do not add ids to the file beyond this list.
- Names are JSON-escaped for `"` and `\` only (the default name has neither).
- `internal`, XML doc + `Threading: stateless` remark; the factory is used by the control (P7-23); nothing is public.

## Tests

(sample palettes, written as constants in the test: **Black** — Window `#000000`, WindowText `#FFFFFF`, Highlight `#1AEBFF`, HighlightText `#000000`, GrayText `#3FF23F`, Hotlight `#FFFF00`, ButtonFace `#000000`, ButtonText `#FFFFFF`;
**White** — Window `#FFFFFF`, WindowText `#000000`, Highlight `#37006E`, HighlightText `#FFFFFF`, GrayText `#600000`, Hotlight `#00009F`, ButtonFace `#FFFFFF`, ButtonText `#000000`;
**Custom** — Window `#000080`, WindowText `#FFFF00`, Highlight `#FFFFFF`, HighlightText `#000080`, GrayText `#C0C0C0`, Hotlight `#00FFFF`, ButtonFace `#000080`, ButtonText `#FFFF00`. They are samples, not a claim about Microsoft's exact scheme values.)

- [ ] New: `Create_DarkPalette_IsHighContrastDark` (Black and Custom), `Create_LightPalette_IsHighContrastLight` (White), `Create_UsesTheGivenName`, `Create_DefaultName_IsWindowsHighContrast`
- [ ] New: `Create_MapsTheEditorColors` (theory over the 3 palettes: `Background == Window`, `Foreground == WindowText`, `SelectionBackground == Highlight`, `SelectionForeground == HighlightText`, `CursorForeground == WindowText`,
      `LineNumberForeground == WindowText`, `LineHighlightBorder == Hotlight`)
- [ ] New: `Create_Palette_MeetsWcagAaa_ForTextAndSelection` (theory: `ContrastRatio.Between(Foreground, Background) >= 7` and `ContrastRatio.Between(SelectionForeground!.Value, SelectionBackground) >= 4.5` for the 3 palettes; if a sample fails, fix the sample, not the factory)
- [ ] New: `Create_CommentTokens_UseGrayText_Italic`, `Create_KeywordTokens_AreBold`, `Create_LinkTokens_UseHotlightUnderlined` (find the rule by scope in `theme.TokenColors`; `FontStyle` strings `italic`, `bold`, `underline`), `Create_DefaultRule_HasWindowTextOnWindow` (`TokenColors[0]`: `Foreground == WindowText`, `Background == Window`)
- [ ] New: `Create_IgnoresTheAlphaOfTheInput` (colors with `A = 0x20` give the same theme colors as opaque ones)
- [ ] New: `Create_WidgetColors_AreResolved` (`theme.GetColor("editorWidget.background") == ButtonFace`, `GetColor("focusBorder") == Hotlight`, `GetColor("input.border") == WindowText`)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Reading the real system colors and following the Windows setting (P7-23), token colors beyond the three rules, Windows 10's "High Contrast #1/#2" names.

## Suggested commit message

`feat(themes): build a high contrast theme from the Windows system colors`
