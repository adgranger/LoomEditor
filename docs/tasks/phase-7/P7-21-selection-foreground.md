# P7-21 — High contrast: the selected text color (`editor.selectionForeground`)

- **Status:** todo
- **Depends on:** P7-20
- **Size:** M

## Goal

In a high contrast theme the selected text is drawn in `editor.selectionForeground` (black on the white / yellow selection of `hc_black`, white on the blue selection of `hc_light`), so that selected text stays readable.
Today it keeps its token color on the selection color: white on white in `hc_black`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/platform/theme/common/colors/editorColors.ts` | 125–127 | `editorSelectionForeground`: `editor.selectionForeground`, `hcDark: '#000000'`, `hcLight: Color.white`, no value for `dark` / `light` |
| `src/vs/editor/browser/viewParts/viewLines/viewLine.ts` | 123–141 | `selectionsOnLine` and the `inline-selected-text` decoration, **only when `isHighContrast(themeType)`** |
| `src/vs/editor/browser/viewParts/selections/selections.ts` | 399–405 | the CSS rule that colors `inline-selected-text` with `editorSelectionForeground` when it is not transparent |

## Files to create or change

- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: the row `["editor.selectionForeground"] = (null, null, "#000000", "#FFFFFF")` right after `editor.selectionBackground`, with the upstream line in a comment (`// editorColors.ts:125`)
- `src/LoomEditor.Themes/EditorTheme.cs` — change: property `ThemeColor? SelectionForeground { get; init; }` (XML doc, `editor.selectionForeground`) set in `Load` next to `SelectionBackground` with `theme.GetColor("editor.selectionForeground")`
- `src/LoomEditor.Themes/PublicAPI.Unshipped.txt` — change (`./eng/Update-PublicApi.ps1 src/LoomEditor.Themes`): `EditorTheme` is public
- `src/LoomEditor.Rendering/TextViewRenderer.Selection.cs` — create (`partial`: `GetSelectionStyleOverrides`)
- `src/LoomEditor.Rendering/TextViewRenderer.cs` — change: line ~143, the overrides passed to `LineStyle.Create` also contain the selection overrides
- `tests/LoomEditor.TextMate.Tests/Themes/EditorColorDefaultsTests.cs`, `BuiltInThemesTests.cs` — change: the theme tests below
- `tests/LoomEditor.Rendering.Tests/TextViewRendererSelectionForegroundTests.cs` — create (follow `TextViewRendererDecorationsTests.cs` for the recording surface and the view model setup)

## Design decisions (already made — do not change)

- `GetSelectionStyleOverrides(ViewModel viewModel, IReadOnlyList<Selection> selections, int lineNumber, int maxColumn, int length)` returns `List<StyleOverride>?` (`null` when nothing applies). It returns `null` unless
  `Theme.Type is ThemeType.HighContrastDark or ThemeType.HighContrastLight` **and** `Theme.SelectionForeground is { A: > 0 } color` (Monaco: `isHighContrast(themeType)`, `!isTransparent()`).
  For each selection that intersects the **view** line: `startColumn = selection.StartLineNumber == lineNumber ? selection.StartColumn : viewModel.GetLineMinColumn(lineNumber)`,
  `endColumn = selection.EndLineNumber == lineNumber ? selection.EndColumn : maxColumn`; if `startColumn < endColumn` add `new StyleOverride(Math.Min(startColumn - 1, length), Math.Min(endColumn - 1, length), LineStyle.ToArgb(color))`.
  (`viewLine.ts` 127–141, where `lineData.minColumn` / `maxColumn` are the view line's.)
- In `Render`, the list given to `LineStyle.Create` is `[.. existing ?? [], .. selectionOverrides ?? []]` so that the selection color is applied **last** and wins over token colors and decoration colors (Monaco pushes `inline-selected-text` after the other decorations).
  The selections used are the ones `Render` already passes to the selection drawing (the same variable): view selections.
- A collapsed selection (the caret) adds nothing (`startColumn == endColumn`). Sticky scroll lines (`TextViewRenderer.StickyScroll.cs`) are not selected text: unchanged.
- `Dark` / `Light` themes are unaffected: their default is `null` and the type check fails first. A theme file that sets `editor.selectionForeground` in a non-HC theme has no effect (Monaco: same).
- The selected line break is drawn as an extra space by `DrawSelection` (no text there): nothing to color.

## Tests

- [ ] New (`EditorColorDefaultsTests`): `SelectionForeground_Dark_HasNoDefault`, `SelectionForeground_Light_HasNoDefault`, `SelectionForeground_HcDark_IsBlack` (`#000000FF`), `SelectionForeground_HcLight_IsWhite` (`#FFFFFFFF`)
- [ ] New (`BuiltInThemesTests`): `SelectionForeground_DarkHighContrast_IsBlack`, `SelectionForeground_LightHighContrast_IsWhite`, `SelectionForeground_DarkModern_IsNull`,
      `HighContrastSelection_ForegroundOnBackground_MeetsAaa` (uses `ContrastRatio.Between(theme.SelectionForeground!.Value, theme.SelectionBackground) >= 7` for both HC themes)
- [ ] New (`TextViewRendererSelectionForegroundTests`): `HighContrastDark_SelectedText_UsesTheSelectionForeground` (`"hello world"`, selection `(1,1)-(1,6)`, theme "Dark High Contrast": the drawn run covering `hello` has the black foreground, the rest `world` the theme foreground),
      `HighContrastLight_SelectedText_UsesWhite`, `DarkTheme_SelectedText_KeepsTokenColors`, `HighContrastSelection_MultiLine_ColorsTheSelectedPartOfEachLine` (`"abc\ndef"`, selection `(1,2)-(2,3)`),
      `HighContrastSelection_Empty_ChangesNothing`, `HighContrastSelection_WinsOverTokenColors` (give the line a token color and check the selected run still has the selection foreground),
      `HighContrastSelection_MultipleCursors_ColorsEachSelection`, `HighContrastSelection_WrappedLine_UsesViewColumns` (word wrap on: a selection across the break colors both view lines' parts)
- [ ] Existing renderer tests still pass

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: the three Monaco locations; "Behavior differences": sticky lines do not color selections (there are none)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The Windows system colors theme (P7-22), contrast of token colors, `selectionHighlight` colors.

## Suggested commit message

`feat(themes): draw the selected text with editor.selectionForeground in high contrast`
