# P1-32 — Gallery demo of the text model (edits, undo/redo, EOL)

- **Status:** todo
- **Depends on:** P1-29
- **Size:** S

## Goal

Make phase 1 visible: the Gallery gets a toolbar with buttons that edit the document through `TextModel`, undo and
redo, switch the EOL, and a status line showing the model state (line count, length, EOL, version ids, can undo/redo).
Typing and cursors do not exist yet (phase 3), so the demo drives the model from buttons.

## Monaco sources

None (sample app only).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — add `public TextModel Model => _context.Model;` (XML doc +
  threading note); update `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` with `./eng/Update-PublicApi.ps1 src/LoomEditor.WinUI`
- `samples/LoomEditor.Gallery/MainWindow.xaml` — title text updated to phase 1; a `CommandBar` (or horizontal
  `StackPanel` of `Button`s) in the first row, the editor in the second, a status `TextBlock` in a third row
- `samples/LoomEditor.Gallery/MainWindow.xaml.cs` — button handlers (see below); subscribe to `Editor.Model.ContentChanged`
  to refresh the status line
- `samples/LoomEditor.Gallery/ModelDemoActions.cs` — create: one `internal static` method per demo action, taking a
  `TextModel` (keeps `MainWindow.xaml.cs` small and the actions unit-testable)
- `tests/LoomEditor.Gallery.Tests/` — **not created** (a WinUI test project is out of scope); instead test the actions in
  `tests/LoomEditor.View.Tests/` only if `ModelDemoActions` can live in a non-WinUI project — otherwise leave it untested
  and rely on the manual check below

## Design decisions (already made — do not change)

- Buttons and what they do (every edit goes through `Model.PushEditOperations(null, edits, null)` so that it is
  undoable; call `Model.PushStackElement()` before each button action so that one click = one undo step):
  1. **Insert header** — insert `"// Edited with TextModel\n"` at 1,1.
  2. **Append line** — insert `"\n// line appended at " + current line count` at the end of the last line.
  3. **Delete first line** — delete line 1 including its EOL (range 1,1 → 2,1); disabled-looking no-op if only one line.
  4. **Replace "Hello"** — replace every `Hello` by `Howdy` in a single `PushEditOperations` call with several
     edits (finds matches with a plain `string.IndexOf` per line; this is NOT the Find feature of phase 5).
  5. **Undo** / **Redo** — `Model.Undo()` / `Model.Redo()`; buttons enabled from `CanUndo` / `CanRedo`.
  6. **LF / CRLF** — `Model.PushEol(EndOfLineSequence.LF / CRLF)`.
  7. **Reset** — `Editor.Text = SampleText.HelloWorld` (`SetValue` clears undo history).
- The status line format: `Lines: {n} · Length: {n} · EOL: LF|CRLF · Version: {n} · Alt. version: {n} · Undo: yes|no · Redo: yes|no`,
  refreshed on `ContentChanged` (undo/redo raise it too).
- No new feature in `EditorFeatures.cs`, no keyboard shortcut for undo/redo (that comes with the commands of phase 3).
- No reflection, no `x:Bind` to functions that need reflection: use plain `Click` handlers (Native AOT).
- Do not reformat or change `SampleText.HelloWorld` beyond what is needed (it must contain the word `Hello`).

## Tests

- [ ] Existing tests pass unchanged
- [ ] Manual check, described in the PR/commit body: insert header → undo → redo; replace "Hello" (2 occurrences) →
      one undo restores both; CRLF → status shows CRLF, undo returns to LF; Reset clears undo (Undo disabled)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, and the app starts and every button works
- [ ] `PublicAPI.Unshipped.txt` of `LoomEditor.WinUI` updated (only `Model` added)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Typing, cursors, keyboard undo/redo (phase 3); Find/replace (phase 5); a WinUI UI-test project; making `ModelDemoActions`
public.

## Suggested commit message

`feat(gallery): add a text model demo with edits, undo/redo and EOL`
