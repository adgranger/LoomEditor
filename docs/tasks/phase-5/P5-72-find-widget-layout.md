# P5-72 — The find widget (XAML): layout, theme colors and the find row

- **Status:** todo
- **Depends on:** P5-71
- **Size:** M

## Goal

Ctrl+F shows a VS Code-like find widget at the top right of the text: a find box with the match case / whole word /
regex toggles, previous / next / close buttons. Typing searches as you type; the toggles and buttons drive the
`FindReplaceState` and the `FindController`, and every state change is shown. The replace row, "n of m" and find in
selection exist in the XAML but are finished in P5-74; keyboard and focus routing is P5-73.

## Sources

`contrib/find/browser/findWidget.ts` (behavior only, not its DOM — ADR 0009 decision 11), pinned commit
`0c9c0a4295a9222e12a605537738719455ae3056`:

| Lines | What to reproduce |
|---|---|
| 64–87 | labels (`Find`, `Previous Match`, `Next Match`, `Close`, `Find in Selection`, `Replace`, `Replace All`, `Toggle Replace`), `FIND_WIDGET_INITIAL_WIDTH` 419, `PART_WIDTH` 275, `FIND_INPUT_AREA_WIDTH` 221, `FIND_INPUT_AREA_HEIGHT` 33 |
| 365–442 | `_onStateChanged`: this card handles `searchString`, `isRevealed`, `isRegex`, `wholeWord`, `matchCase` |
| 551–647 | `_reveal`, `_hide(focusTheEditor)` — visibility only (no animation, no view zone) |
| 984–1150 | `_buildDomNode`: find input `onInput` (1020–1032), `onDidOptionChange` (1033–1040), previous / next / close buttons (1070–1150) |
| `findWidget.css` 1–80 | sizes and paddings, as a reference for the XAML |

Read `src/LoomEditor.WinUI/Controls/LoomEditor.cs`, `LoomEditor.Highlighting.cs` (how `Theme` is applied) and
`Themes/Generic.xaml`.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Find/FindWidget.xaml`, `FindWidget.xaml.cs` — create: **public** `sealed partial class FindWidget : UserControl, IFindWidget`
  (XAML types of a library must be public for the XAML compiler; every member except the constructor is `internal`)
- `src/LoomEditor.WinUI/Controls/Find/FindWidget.Theme.cs` — create (`ApplyTheme(EditorTheme)`)
- `src/LoomEditor.WinUI/Controls/Find/README.md` — create
- `src/LoomEditor.WinUI/Themes/Generic.xaml` — change: `PART_FindWidget` in the template's `Grid`, after `PART_Canvas`
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — change: `[TemplatePart(Name = "PART_FindWidget", Type = typeof(FindWidget))]`
  (optional part: without it, find works without UI); `OnLoaded` connects the widget after the feature host exists,
  `OnUnloaded` disconnects it
- `src/LoomEditor.WinUI/Controls/LoomEditor.Highlighting.cs` — change: a theme change calls `_findWidget?.ApplyTheme(Theme)`
- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: the widget color rows (below)
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — via `./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI` (`FindWidget` + its constructor)
- `tests/LoomEditor.TextMate.Tests/Themes/EditorColorDefaultsTests.cs` — change: `Resolve_FindWidgetColors_DarkAndLightDefaults`

## Design decisions (already made — do not change)

- **Template.** `<find:FindWidget x:Name="PART_FindWidget" HorizontalAlignment="Right" VerticalAlignment="Top" Margin="0,0,14,0" Visibility="Collapsed" />`
  (14 = the vertical scroll bar). It sits over the canvas; there is no view zone (`addExtraSpaceOnTop` is not ported), so it
  may cover the end of the first lines, as in Monaco with that option off.
- **Layout** (`Border` 419 wide, padding 4, border 1 px, then a `Grid` with 2 columns × 2 rows):
  column 0 (18 px, both rows): the toggle-replace button (chevron `` closed / `` open, Segoe Fluent Icons);
  row 0: a `Grid` with `FindTextBox` (width 221) and, overlaid on its right, `MatchCaseToggle` ("Aa"), `WholeWordToggle`
  ("ab" underlined), `RegexToggle` (".*"); then `MatchesCountText` (min width 69), `PreviousButton` (``),
  `NextButton` (``), `FindInSelectionToggle` (``), `CloseButton` (``);
  row 1 (`ReplaceRow`, collapsed): `ReplaceTextBox` (width 221) with `PreserveCaseToggle` ("AB") overlaid, `ReplaceButton`,
  `ReplaceAllButton` (glyphs free, Segoe Fluent Icons). Every input, toggle and button has `AutomationProperties.Name` and a
  `ToolTip` with Monaco's label and VS Code's Windows key ("Next Match (Enter)", "Match Case (Alt+C)"…).
- **No bindings** (`{Binding}` needs reflection; `x:Bind` buys nothing here): XAML event attributes (`Click="OnNextClick"`,
  compiled by the XAML compiler, AOT-safe) and code-behind that mirrors `_onStateChanged`. `FindWidget` subscribes to
  `FindReplaceState.Changed` in `Connect(FindController controller, ViewModel viewModel, Action focusEditor)` and
  unsubscribes in `Disconnect()`. `Connect` sets `controller.Widget = this`; `Disconnect` sets it back to `null`.
- **Find box ↔ state.** `TextChanged` → `state.Change(new() { SearchString = text }, moveCursor: true)` when
  `EditorFindOptions.FindOnType` and not inside a state update (`_ignoreChangeEvent`, Monaco line 1028). A toggle click →
  `state.Change(new() { IsRegex, WholeWord, MatchCase }, moveCursor: true)` (Monaco 1033–1040).
- **Line breaks.** WinUI's `TextBox` stores line breaks as `\r`. `AcceptsReturn="True"`, `TextWrapping="NoWrap"`,
  `MaxHeight` of about 6 lines; state → box: `\n` → `\r`; box → state: `\r\n` and `\r` → `\n`. (Monaco's find input is
  multi-line too; Enter is routed in P5-73 so it never inserts a line break.)
- **Buttons.** Previous / next → `FindActions.PreviousMatch` / `NextMatch` (Monaco runs the actions); close →
  `state.Change(new() { IsRevealed = false, SearchScope = null }, false)` (Monaco 1143). Revealed → `Visibility.Visible`;
  hidden → `Collapsed` and `focusEditor()` (`_hide(true)`).
- **Colors** from the control's `EditorTheme` through the theme color lookup of step A (ADR 0009 decision 4), applied by
  changing the `Color` of named `SolidColorBrush`es declared once in `UserControl.Resources`; the `TextBox` and
  `ToggleButton` lightweight-styling keys (`TextControlBackground`, `TextControlBackgroundPointerOver`,
  `TextControlBackgroundFocused`, `TextControlForeground*`, `TextControlBorderBrush*`, `ToggleButtonBackgroundChecked*`, …)
  point to those brushes with `{StaticResource}`, so a theme change needs no template refresh. Ids: `editorWidget.background`,
  `editorWidget.foreground`, `editorWidget.border`, `input.background`, `input.foreground`, `input.border`, `focusBorder`,
  `inputOption.activeBackground`, `inputOption.activeBorder`, `inputOption.activeForeground`, `errorForeground`. No
  `widget.shadow` (no drop shadow).
- **Color defaults** (dark, light, hcDark, hcLight; a `transparent(...)` default uses `ColorValue.Transparent` of P5-05, not a computed hex): `editorWidget.background` (`editorColors.ts:49`)
  `#252526`, `#F3F3F3`, `#0C141F`, `#FFFFFF`; `editorWidget.foreground` (53) `foreground` ×4; `editorWidget.border` (57)
  `#CCCCCC33`, `#61616133`, `contrastBorder`, `contrastBorder`; `input.background` (`inputColors.ts:20`) `#3C3C3C`,
  `#FFFFFF`, `#000000`, `#FFFFFF`; `input.foreground` (24) `foreground` ×4; `input.border` (28) null, null,
  `contrastBorder`, `contrastBorder`; `inputOption.activeBorder` (32) `#007ACC`, `#007ACC`, `contrastBorder`,
  `contrastBorder`; `inputOption.activeBackground` (40) `#007FD466`, `#0090F133`, `#00000000`, `#00000000`;
  `inputOption.activeForeground` (44) `#FFFFFF`, `#000000`, `foreground`, `foreground`; `errorForeground`
  (`baseColors.ts:25`) `#F48771`, `#A1260D`, `#F48771`, `#B5200D`.
- IME in the find box works out of the box (a standard `TextBox`).

## Tests

No WinUI test project. Covered: the color defaults test above. **Manual check by the maintainer** (Gallery, Debug and AOT):

- [ ] Ctrl+F shows the widget top right, the find box contains the word under the cursor; typing highlights the matches
      and selects the first one after the cursor
- [ ] The three toggles change the matches at once; the next / previous buttons move through the matches; close hides it
- [ ] Dark and light themes: the widget and the inputs use the theme's widget and input colors
- [ ] Typing Japanese with the Microsoft IME in the find box works

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings (the new XAML is AOT-safe)
- [ ] Public API file updated with the script, diff checked
- [ ] `docs/PORTING.md`: map row `contrib/find/browser/findWidget.ts` 🔶 (XAML, find row, P5-72); the color difference
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Keyboard and focus routing (P5-73); replace row, "n of m", find in selection, button enable states (P5-74). Not ported
(ADR 0009 decision 11): search history, resizing by dragging (sash), overview ruler marks, accessibility help, the
slide-in animation, the find options widget shown when the widget is hidden.

## Suggested commit message

`feat(winui): add the find widget`
