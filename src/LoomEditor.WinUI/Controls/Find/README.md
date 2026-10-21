# Find widget

**Role:** the XAML find widget at the top right of the editor (Monaco: `findWidget.ts`, behavior only). Find box with the match case,
whole word and regex toggles, previous / next / close buttons; typing searches as you type. The replace row exists in the XAML but
stays collapsed until P5-74; keyboard and focus routing is P5-73.

**Files:** `FindWidget.xaml` (layout; brushes in `UserControl.Resources` re-colored by the theme), `FindWidget.xaml.cs` (state ↔ XAML, `IFindWidget`),
`FindWidget.Theme.cs` (`ApplyTheme(EditorTheme)`). The editor template has an optional `PART_FindWidget`; `LoomEditor.Find.cs` connects it.

**Rules:** no `{Binding}` (event attributes + code-behind, AOT-safe); the type is public only for the XAML compiler.

**Keyboard and focus (P5-73):** `FindWidget.Keyboard.cs`. `PreviewKeyDown` on the widget handles Ctrl+Enter (line break) and Ctrl+Down (focus the text), then resolves the key
with the editor's keybindings and runs it only if the id is in `RoutedCommandIds` (the find commands); everything else stays with the `TextBox`. The control ignores
keys, characters, IME focus and pointer events that come from the widget (`!HasFocus`, `IsFromFindWidget`).

**Replace and count (P5-74):** `FindWidget.Replace.cs`: the replace row (shown when `IsReplaceRevealed` and the editor is writable), preserve case, replace / replace all, find in selection,
`UpdateButtons` (Monaco `_updateButtons`) and the "n of m" label (`FindMatchesLabel` in `LoomEditor.View`, red `errorForeground` when nothing matches).
