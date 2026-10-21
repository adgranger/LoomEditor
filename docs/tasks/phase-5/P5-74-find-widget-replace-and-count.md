# P5-74 — The find widget: replace row, "n of m", find in selection, manual checks

- **Status:** done (manual checks pending, maintainer)
- **Depends on:** P5-73
- **Size:** M

## Goal

The widget is complete: Ctrl+H opens the replace row (preserve case toggle, replace, replace all), the widget shows
"3 of 12" / "No results" / "?" / "19999+", the find-in-selection toggle limits the search to the selection, and every
button is enabled exactly when VS Code enables it. The maintainer runs the full manual check list of the find feature.

## Sources

`contrib/find/browser/findWidget.ts` at `0c9c0a4295a9222e12a605537738719455ae3056`:

| Lines | What to reproduce |
|---|---|
| 76–78 | `NLS_MATCHES_COUNT_LIMIT_TITLE`, `NLS_MATCHES_LOCATION` (`{0} of {1}`), `NLS_NO_RESULTS` (`No results`) |
| 198–233 | read-only change hides the replace row; `loop` option; selection change updates the find-in-selection toggle |
| 365–442 | `_onStateChanged`: `replaceString`, `isReplaceRevealed`, `preserveCase`, `searchScope`, `matchesCount` / `matchesPosition` (`no-results`), `loop` |
| 457–489 | `_updateMatchesCount` (without the `alertFn` announcement and the width bookkeeping) |
| 517–549 | `_updateToggleSelectionFindButton`, `_updateButtons` |
| 856–884 | `_updateSearchScope` |
| 1100–1135 | find-in-selection toggle `onChange` |
| 1155–1258 | replace input (`onInput`, `onDidOptionChange`), replace / replace all buttons, toggle-replace button |
| `findWidget.css` 222–224 | `.no-results .matchesCount` → `errorForeground` |

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Find/FindWidget.Replace.cs` — create (partial: replace row, toggle replace, preserve case)
- `src/LoomEditor.WinUI/Controls/Find/FindWidget.xaml.cs` — change: matches count, find in selection, `UpdateButtons`
- `src/LoomEditor.View/Features/Find/FindMatchesLabel.cs` — create: `internal static string Format(int matchesPosition, int matchesCount)`
  (pure; `MatchesLimit` adds `+`, position 0 shows `?`, count 0 gives `No results`)
- `src/LoomEditor.WinUI/Controls/Find/README.md`, `src/LoomEditor.View/Features/Find/README.md` — update (final)
- `tests/LoomEditor.View.Tests/Features/Find/FindMatchesLabelTests.cs` — create

## Design decisions (already made — do not change)

- Replace row visible when `State.IsReplaceRevealed && !viewModel.CursorConfig.ReadOnly` (Monaco 380–395); the
  toggle-replace button changes `IsReplaceRevealed` (`moveCursor: false`, Monaco 1246) and is disabled when read-only.
  Replace box `TextChanged` → `Change(new() { ReplaceString = … }, false)`; preserve case toggle → `Change(new() { PreserveCase = … }, false)`;
  replace / replace all buttons → `controller.Replace()` / `ReplaceAll()`. Line breaks as in P5-72.
- `UpdateButtons` is Monaco's `_updateButtons` with `State.CanNavigateBack()` / `CanNavigateForward()` (P5-65); a disabled
  element is `IsEnabled = false`.
- Count text: `FindMatchesLabel.Format`; its foreground is `errorForeground` when `searchString != ""` and
  `matchesCount == 0` (Monaco's `no-results`), else `editorWidget.foreground`. Tooltip `NLS_MATCHES_COUNT_LIMIT_TITLE`
  when `matchesCount >= MatchesLimit`. The screen reader announcement (`alertFn`) is not ported (the automation peer of
  the `TextBlock` exposes the text).
- Find in selection: checked → selections with `endColumn == 1` on a later line end on the previous line's max column,
  multi-line ones only, not equal to `State.CurrentMatch`; non-empty list → `Change(new() { SearchScope = ranges }, true)`;
  unchecked → `Change(new() { SearchScope = null }, true)`. The toggle is enabled when visible and (checked or the selection
  is not empty); the control's `SelectionChanged` refreshes it (Monaco 230–233).
- The `loop` option is read from `EditorFindOptions` once (no configuration change events in phase 5).

## Tests

- [x] New: `Format_NoMatches_NoResults`, `Format_PositionZero_QuestionMark`, `Format_AtLimit_AddsPlus`, `Format_Normal_NOfM`
- [ ] **Manual check by the maintainer** (Gallery, Debug and the AOT build; write the results in the card when done):
  - [ ] Ctrl+H opens the widget with the replace row; with a one-line selection the find box gets the selection and the
        replace box gets the focus; read-only editor: Ctrl+H does nothing, the chevron is disabled
  - [ ] Replace replaces the current match and selects the next; Replace All (button and Ctrl+Alt+Enter) replaces all in
        one Ctrl+Z; `(\w+)=` → `$1:` with regex on; preserve case: `foo`/`Foo`/`FOO` → `bar`/`Bar`/`BAR`
  - [ ] "n of m" follows F3 / Shift+F3; "No results" in red for a missing word; "?" after moving the cursor away; a file with
        25 000 matches shows `19999+`
  - [ ] Find in selection (Alt+L or the button) with a multi-line selection searches only inside it and draws the scope
  - [ ] Alt+Enter selects all matches (multi-cursor); Ctrl+F3 finds the next occurrence of the word under the cursor
  - [ ] Match highlight colors: current match vs other matches in Dark Modern, Light Modern, High Contrast
  - [ ] Typing in the text while the widget is open updates the matches after a short delay; undo restores them
  - [ ] Esc / close, then Ctrl+F again: the previous search string and toggles are kept

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings (the manual checks in the AOT app are still to do)
- [x] `docs/PORTING.md`: map row `findWidget.ts` 🔶 final (XAML; not ported: history, sash, overview ruler, accessibility
      help, animation, view zone, find options widget)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Search history (Up/Down in the boxes), widget resizing, the "Find in Selection" automatic mode when the widget opens
(`autoFindInSelection` is in the controller, P5-70).

## Suggested commit message

`feat(winui): finish the find widget with replace and match count`
