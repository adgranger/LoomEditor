# P3-38 — Multi-cursor commands (without Find) and scroll commands

- **Status:** todo
- **Depends on:** P3-37
- **Size:** M

## Goal

Add cursors above/below (Ctrl+Alt+↑/↓), at the end of each selected line (Shift+Alt+I), cycle the primary cursor,
and scroll the view without moving the cursor (Ctrl+↑/↓, Alt+PgUp/PgDn). The multi-cursor commands of Monaco's
`multicursor` contribution become a LoomEditor feature; the scroll commands are core commands.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/multicursor/browser/multicursor.ts` | 46–96 | `InsertCursorAbove` (`editor.action.insertCursorAbove`) |
| same | 98–148 | `InsertCursorBelow` (`editor.action.insertCursorBelow`) |
| same | 150–202 | `InsertCursorAtEndOfEachLineSelected` (`editor.action.insertCursorAtEndOfEachLineSelected`) |
| same | 204–265 | `InsertCursorAtEndOfLineSelected` (`editor.action.addCursorsToBottom`), `InsertCursorAtTopOfLineSelected` (`editor.action.addCursorsToTop`) |
| same | 1124–1215 | `FocusNextCursor`, `FocusPreviousCursor` |
| `src/vs/editor/browser/coreCommands.ts` | 51–235 | `namespace EditorScroll_` (arguments: `to`, `by`, `value`, `revealCursor`, `select`; parse) — not the JSON `metadata` |
| same | 1308–1630 | `EditorScrollImpl` (`editorScroll`), `ScrollLineUp`, `ScrollPageUp`, `ScrollEditorTop`, `ScrollLineDown`, `ScrollPageDown`, `ScrollEditorBottom`, `ScrollLeft`, `ScrollRight` |
| `src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts` | 48–87 | suite `Multicursor`: `issue #2205: Multi-cursor pastes in reverse order`, `issue #1336: Insert cursor below on last line adds a cursor to the end of the current line` |

## Files to create or change

- `src/LoomEditor.View/Features/MultiCursor/MultiCursorFeature.cs`, `MultiCursorCommands.cs`, `README.md` — create
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: add `() => new MultiCursorFeature()`
- `src/LoomEditor.View/Commands/EditorScroll.cs` — create (arguments + parse)
- `src/LoomEditor.View/Commands/CoreNavigationCommands.Scroll.cs` — create (partial)
- `src/LoomEditor.View/Commands/CoreCommandRegistration.cs` — change: register the scroll ids
- `src/LoomEditor.View/PublicAPI.Unshipped.txt` — update (the feature and its id constants)
- `tests/LoomEditor.View.Tests/Features/MultiCursor/MultiCursorTests.cs`, `tests/LoomEditor.View.Tests/Commands/EditorScrollTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `MultiCursorFeature` (public, `Id = "loom.multiCursor"`) registers the 7 ids above (Monaco's strings). The actions
  are refused when read-only (`InsertCursorAbove/Below` check `readOnly` in Monaco: port the check). They use
  `CursorMoveCommands.AddCursorUp/Down` (P3-24) and reveal like Monaco.
- **Find-based commands are not part of phase 3**: `addSelectionToNextFindMatch` (Ctrl+D), `moveSelectionToNextFindMatch`,
  `selectHighlights`, `changeAll`, `MultiCursorSession`, `SelectionHighlighter` need the Find feature (phase 5). Say so in
  the feature README.
- `editorScroll` arguments: `internal sealed record EditorScrollArguments(string To, string? By = null, int Value = 1, bool RevealCursor = false, bool Select = false)`
  and `EditorScroll.Parse` → `EditorScrollParsedArguments?` (port lines 51–235 without `metadata`). The command scrolls
  with `ViewModel.ViewLayout.SetScrollPosition` exactly as `EditorScrollImpl._runEditorScroll` / `_computeDesiredScrollTop`
  do, and with `revealCursor` moves the cursors inside the viewport with `CursorMoveCommands.FindPositionInViewportIfOutside` (P3-25).

## Tests

- [ ] Port `multicursor.test.ts` › `issue #2205: Multi-cursor pastes in reverse order` and › `issue #1336: Insert cursor below on last line adds a cursor to the end of the current line`
- [ ] **Not ported:** `issue #26393: Multiple cursors + Word wrap` (word wrap, phase 6); suites `Multicursor selection`
      and below (73 tests: Find, phase 5) — reasons in the PORTING tests table
- [ ] New: `editor.action.insertCursorAbove` on line 1 adds nothing; on line 3 adds a cursor on line 2 at the same visible column
- [ ] New: `editor.action.insertCursorAtEndOfEachLineSelected` with a selection over lines 1–3 → 3 cursors at the line ends
- [ ] New: `scrollLineDown` scrolls by one line height and does not move the cursor; `scrollPageDown` by a page;
      `EditorScroll.Parse` rejects an unknown `to`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `PublicAPI.Unshipped.txt` of `LoomEditor.View`: only `MultiCursorFeature` and `MultiCursorCommands`
- [ ] `docs/PORTING.md`: map rows `contrib/multicursor/browser/multicursor.ts` (🔶 without Find, P3-38) and `coreCommands.ts`
      (+ scroll); tests table `multicursor.test.ts` 2 / 76
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Keybindings (P3-39), Find-based multi-cursor (phase 5).

## Suggested commit message

`feat(view): add multi-cursor and scroll commands`
