# P9-30 — Accessible diff viewer: the UI

- **Status:** todo
- **Depends on:** P9-29, P9-26
- **Size:** M

## Goal

`F7` / `Shift+F7` in the diff editor open an accessible diff viewer: a list that covers the editors and shows one group of changes as rows (`-` deleted, `+` added, context), readable by Narrator with the exact labels of the model, navigable with the arrow keys, `Enter` / `Space` jump to the line in the editor, `Esc` closes it. The viewer is only opened by the user (`F7`, or `OpenAccessibleDiffViewer()`); the `onlyShowAccessibleDiffViewer` option is not ported.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/browser/widget/diffEditor/components/accessibleDiffViewer.ts` | 340–440 | `View`: the container (role list, label), the close action, keyboard (`Down`, `Up`, `Esc`, `Space`, `Enter`, with `Ctrl` / `Alt` variants), focus the selected row (`tabIndex 0`, others `-1`), `focus` → `goToLine` |
| same | 540–600 | `_createRow`: two line number columns (original, modified), the spacer with the `+` / `−` sign, the line text (modified text for added / unchanged rows, original text for deleted rows), the row classes `line-insert` / `line-delete` and `char-insert` / `char-delete` on the number cells |
| same | 600–692 | `_getLineHtml`: the line is rendered **without tokens** (`LineTokens.createEmpty`), with the editor's font and tab size |
| `src/vs/editor/browser/widget/diffEditor/commands.ts` | 205–250 | `AccessibleDiffViewerNext` (`F7`), `AccessibleDiffViewerPrev` (`Shift+F7`), precondition `isInDiffEditor` |
| `src/vs/editor/browser/widget/diffEditor/diffEditorWidget.ts` | the `_accessibleDiffViewer` members, `accessibleDiffViewerNext` / `Prev` | opening and layering over the editors |

## Files to create or change

- `src/LoomEditor.WinUI/Controls/DiffEditor/AccessibleDiffViewer.cs` — create (`internal sealed class AccessibleDiffViewer : Control`, template-less: builds a `Grid` with a `ListView` and a close `Button` in code; **or** a `Generic.xaml` template like the other controls: follow whichever the repo's other overlay controls use, `grep -rn "Overlay" src/LoomEditor.WinUI/Controls/Overlay`)
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffViewerRowItem.cs` — create (the view model of one row: texts, brushes, the label)
- `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.AccessibleViewer.cs` — create: the host (`IAccessibleDiffViewerHost` over the two editors), `OpenAccessibleDiffViewer()`, `CloseAccessibleDiffViewer()`, `bool IsAccessibleDiffViewerOpen` (public), commands `F7` / `Shift+F7` through `DiffNavigationFeature`'s mechanism (P9-26: add two commands to that feature: `editor.action.accessibleDiffViewer.next` / `.prev`, same `IsInDiffEditor` condition)
- `src/LoomEditor.View/Features/DiffNavigation/DiffNavigationCommands.cs`, `DiffNavigationFeature.cs`, `DefaultKeybindings.cs` — change: the two commands and bindings (`F7`, `Shift+F7`, `EditorContrib`)
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — regenerate

## Design decisions (already made — do not change)

- The viewer is an overlay over the whole diff control (both editors and the overview), shown with `Visibility`; opening it creates an `AccessibleDiffViewerModel` (P9-29) from the current `DiffState` (diffs changes while open → `SetDiffs`), closing it destroys it and returns focus to the modified editor (`ModifiedFocus`).
- `next`: if closed → open on the group chosen by the model; if open → `NextGroup()`. `prev`: open (if closed) and `PreviousGroup()` (Monaco calls `previousGroup` even on opening: port as is).
- Rows: a `ListView` (`SelectionMode = Single`, `IsItemClickEnabled = true`), one `ListViewItem` per `ViewElement` of the current group; each item shows the two line number cells (the editor's line number color; a `char-insert` / `char-delete` background on the cells of added / deleted rows via the theme colors `diffEditor.insertedTextBackground` / `removedTextBackground`), a `+` / `−` spacer for added / deleted rows, and the **plain** line text (`TextBlock`, `FontFamily`, `FontSize` and tab size of the modified editor; tabs expanded by the model's tab size) over a `line-insert` / `line-delete` background (`diffEditor.insertedLineBackground` / `removedLineBackground`); no syntax colors (Monaco renders without tokens). The header row shows the `"{i}/{n}: @@ … @@"` text.
- **UIA**: every item has `AutomationProperties.Name` = the row label of P9-29 (`AccessibilityMessages`), the list has `AutomationProperties.Name` = the list label, the header item its header label; the selected row is focused (`Focus(FocusState.Keyboard)`), moving selection announces through the item focus change (no extra live region). The viewer's close button has `AutomationProperties.Name = "Close"`.
- Keys on the viewer (`KeyDown`, the same set as Monaco): `Down` / `Ctrl+Down` / `Alt+Down` → `GoToNextLine`; `Up` variants → `GoToPreviousLine`; `Esc` (with `Ctrl` / `Alt` / `Shift`) → close; `Space` / `Enter` → `RevealCurrentElementInEditor` (closes the viewer and reveals: deleted rows reveal in the **original** editor — in inline mode the original is hidden: reveal the deleted block's position in the modified editor instead, i.e. `Modified.RevealLine(diff.Modified.StartLineNumber)`; Monaco has the same limitation: its original editor is also hidden then).
  Because WinUI's `ListView` also handles arrow keys, set `e.Handled = true` after calling the model and update the list selection from the model's `Changed` event (not the other way round), with a guard against the selection-changed echo.
- With a screen reader active the layout does not change automatically: `DiffEditorOptions.IsScreenReaderOptimized` (P9-12, set from the editor's accessibility support, P7-04) only keeps the side by side view from switching to inline when the window narrows (Monaco `renderSideBySide`: `!(… && !screenReaderMode)`). Nothing to do here besides making sure the control sets that property.
- Native AOT: no `x:Bind` with reflection-only paths; use code-behind event handlers (the Gallery's rule: no `{Binding}`).
- `<remarks>Threading: UI thread only.</remarks>`

## Tests

- [ ] The model is tested in P9-29; here: `tests/LoomEditor.View.Tests/Features/DiffNavigation/AccessibleViewerCommandsTests.cs` — `Command_F7_ResolvesInDiffEditor`, `Command_ShiftF7_ResolvesInDiffEditor`, `Command_F7_NotResolvedOutsideDiffEditor`.
- [ ] Manual with Narrator (P9-31's plan): `F7` opens the viewer on the group nearest to the cursor; Narrator reads "Difference 1 of 2: original line 3, 1 line changed, modified line 3, 2 lines changed"; the arrows read the rows ("- old text original line 3", "+ new text modified line 3"); `Enter` closes and the caret is on that line; `Esc` closes and focus returns to the modified editor.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI` run, diff checked
- [ ] `docs/PORTING.md`: `accessibleDiffViewer.ts` (view part), `commands.ts` (F7 / Shift+F7)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Audio cues, the `onlyShowAccessibleDiffViewer` option, syntax coloring in the viewer, localization, the `accessibilityVerbose` hint, the UIA tree of the two editors while the viewer is open (they are hidden from automation with `AutomationProperties.AccessibilityView = Raw` while it is open: do that, it is one line each).

## Suggested commit message

`feat: add the accessible diff viewer`
