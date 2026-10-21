# Find widget

**Role:** the XAML find widget at the top right of the editor (Monaco: `findWidget.ts`, behavior only). Find box with the match case,
whole word and regex toggles, previous / next / close buttons; typing searches as you type. The replace row exists in the XAML but
stays collapsed until P5-74; keyboard and focus routing is P5-73.

**Files:** `FindWidget.xaml` (layout; brushes in `UserControl.Resources` re-colored by the theme), `FindWidget.xaml.cs` (state ↔ XAML, `IFindWidget`),
`FindWidget.Theme.cs` (`ApplyTheme(EditorTheme)`). The editor template has an optional `PART_FindWidget`; `LoomEditor.Find.cs` connects it.

**Rules:** no `{Binding}` (event attributes + code-behind, AOT-safe); the type is public only for the XAML compiler.
