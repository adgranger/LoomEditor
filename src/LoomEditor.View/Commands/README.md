# Commands

`CommandRegistry` holds the commands of one editor, by id. Command ids reuse Monaco's ids when they exist
(`editor.action.selectAll`), otherwise they start with `loom.`.

Command ids are declared as constants in the `<Name>Commands.cs` file of the feature that owns them.
See docs/how-to/add-a-command.md.

## Core commands

`CoreNavigationCommands` (moves, selections, scrolling, mouse, column selection) and `CoreEditingCommands` (type, delete,
Tab, Enter, undo, redo, cut, paste) are Monaco's `coreCommands.ts`. `CoreCommandRegistration` registers them on a
`CommandRegistry`; each is an `EditorCommand` that runs on the `ViewModel` of the editor.

## Keybindings

`DefaultKeybindings.Windows` lists VS Code's default Windows shortcuts for the commands ported so far, and
`KeybindingResolver` turns a `Keybinding` plus a `KeybindingContext` into a command id (heaviest matching rule wins;
equal weights: the rule registered last wins). Notes:

- `when` conditions are C# predicates (`WhenClauses`), not expressions: there is no expression parser.
- A `Keybinding` is one key press. A two-key chord (`Ctrl+K Ctrl+C`) is a rule with a `Chord` (its second key press); `KeybindingResolver.Resolve(context, currentChord, keypress)` answers `NoMatchingKb`, `MoreChordsNeeded` or `KbFound`, and `KeybindingDispatcher` remembers the first key press. At most two key presses. There is no status bar message and no timeout while waiting for the second key: chord mode ends on the next key press or when the editor loses focus (`KeyboardInput.LeaveChordMode`).
- Only the Windows table exists. The clipboard command ids are declared here but registered by the Windows clipboard (P3-47).
