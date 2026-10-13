# Commands

`CommandRegistry` holds the commands of one editor, by id. Command ids reuse Monaco's ids when they exist
(`editor.action.selectAll`), otherwise they start with `loom.`.

Command ids are declared as constants in the `<Name>Commands.cs` file of the feature that owns them.
Default keyboard shortcuts will be declared in a single `DefaultKeybindings.cs` file (phase 3).
See docs/how-to/add-a-command.md.
