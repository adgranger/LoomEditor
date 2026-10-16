# Commands

`CommandRegistry` holds the commands of one editor, by id. Command ids reuse Monaco's ids when they exist
(`editor.action.selectAll`), otherwise they start with `loom.`.

Command ids are declared as constants in the `<Name>Commands.cs` file of the feature that owns them.
See docs/how-to/add-a-command.md.

## Keybindings

`DefaultKeybindings.Windows` lists VS Code's default Windows shortcuts for the commands ported so far, and
`KeybindingResolver` turns a `Keybinding` plus a `KeybindingContext` into a command id (heaviest matching rule wins;
equal weights: the rule registered last wins). Notes:

- `when` conditions are C# predicates (`WhenClauses`), not expressions: there is no expression parser.
- A `Keybinding` is one key press. There are no chords (`Ctrl+K Ctrl+D`) in phase 3.
- Only the Windows table exists. The clipboard command ids are declared here but registered by the Windows clipboard (P3-47).
