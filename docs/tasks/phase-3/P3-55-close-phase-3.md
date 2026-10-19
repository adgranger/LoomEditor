# P3-55 — Close phase 3

- **Status:** done
- **Depends on:** all other phase 3 cards
- **Size:** S (docs only)

## Goal

Make sure the documentation tells the truth about phase 3, and that the phase's "done when" criterion is met and
recorded: *Japanese and Chinese IME input works* (P3-54 manual test list).

## Files to create or change

- `docs/PORTING.md` — map complete for every file touched in phase 3 (strings, cursorColumns, character classifiers,
  intervalTree, textModel additions, cursorCommon, cursor/*, commands/*, coordinatesConverter, viewModelImpl cursor part,
  viewModelEventDispatcher, viewEvents subset, coreCommands, keybindings, wordOperations / wordPartOperations / multicursor /
  clipboard contributions, viewLines reveal, viewCursors / currentLineHighlight / lineNumbers multi-cursor, mouseTarget,
  viewController, mouseHandler, dragScrolling, clipboardUtils, nativeEditContext); "Behavior differences" complete;
  a "Monaco tests ported in phase 3" table (file, ported / total, why not); a short "Done in phase 3" paragraph pointing
  to `docs/tasks/phase-3/`
- `docs/roadmap/phase-3-editing.md` — tick every item, with card numbers; add the IME note (Windows 11)
- `docs/ARCHITECTURE.md` — add "How a key press becomes an edit (phase 3)": `KeyDown` → `KeyboardInput` →
  `KeybindingResolver` → `CommandRegistry` → core command → `CursorsController` → `CommandExecutor` →
  `TextModel.PushEditOperations` → events → reveal → redraw; and the text path `CharacterReceived` /
  `CoreTextEditContext` → `ViewModel.Type` / `CompositionType`. "Where do I find…?" rows for cursors, commands and
  keybindings, mouse, clipboard, IME. Update the phase 0 section title if it is now outdated
- `docs/GLOSSARY.md` — add missing terms: *primary cursor / secondary cursor*, *leftover visible columns*, *visible column*,
  *grapheme cluster*, *tracked range*, *stickiness*, *edit command (`IEditCommand`)*, *edit operation identifier*,
  *undo stop*, *auto whitespace*, *column (box) selection*, *keybinding / when clause*, *composition (IME)*, *edit context*
- `src/LoomEditor.View/Cursors/README.md`, `Controller/README.md`, `Events/README.md`, `Commands/README.md`,
  `src/LoomEditor.Core/Decorations/README.md`, `src/LoomEditor.WinUI/Input/README.md` — describe what is there now
- `README.md` — status line and roadmap table ("we are here" → phase 4); the usage example can show editing
- `docs/tasks/README.md` — all phase 3 cards `done`

## Checks

- [x] Count the ported Monaco tests per file (by `test(` name, as in phases 1–2) and fill the table in `docs/PORTING.md`
- [x] Every `internal` class added in phase 3 has a threading note (`<remarks>Threading: ...</remarks>`)
- [x] Every "Phase 4–5 cut point" comment in the code is listed once in `docs/PORTING.md` (a "Cut points for phases 4–5"
      section: file, Monaco lines, what is missing) — phases 4–5 will start from this list
- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, the app starts

## Out of scope

Writing phase 4 task cards.

## Suggested commit message

`docs: close phase 3`
