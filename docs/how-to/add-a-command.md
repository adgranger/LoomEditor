# How to add a command

A **command** is an action with a string id (`editor.action.selectAll`). Keyboard shortcuts, menus, the command
palette and your own code all run commands by id, through the editor's `CommandRegistry`.

## 1. Pick the feature that owns it

Commands belong to a feature (`src/LoomEditor.View/Features/<Name>/`). If none fits, create a feature first:
see [add-a-feature.md](add-a-feature.md).

## 2. Declare the id

In `<Name>Commands.cs`:

```csharp
public const string SelectAll = "editor.action.selectAll";
```

- Same id as Monaco when the command exists there (keeps keybinding files and docs compatible).
- Otherwise `loom.<feature>.<action>`, e.g. `loom.gallery.resetText`.

## 3. Register it in the feature's `Attach`

```csharp
_registration = context.Commands.Register(new EditorCommand(
    SelectAllCommands.SelectAll,
    execute: () => context.Selection = ComputeSelection(context.Model),
    canExecute: () => context.Model.LineCount > 0));   // optional
```

Dispose `_registration` in the feature's `Dispose`. Registering the same id twice throws: ids are unique.

## 4. (Optional) Bind a keyboard shortcut

- Phase 0: add the key to the table in `src/LoomEditor.WinUI/Input/KeyboardInput.cs`.
- From phase 3: one table, `src/LoomEditor.View/Commands/DefaultKeybindings.cs`, using VS Code's default shortcuts.

## 5. Run it from code

```csharp
editor.Commands.Execute(SelectAllCommands.SelectAll);   // returns false if unknown or disabled
```

## 6. Test it

```csharp
var context = new EditorContext();
using var feature = new SelectAllFeature();
feature.Attach(context);

Assert.True(context.Commands.Execute(SelectAllCommands.SelectAll));
```
