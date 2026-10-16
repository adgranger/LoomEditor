# Input

Keyboard, mouse/pointer, and IME (Input Method Editor, for Chinese/Japanese/Korean input) handling.
Input handlers translate WinUI events into **commands** (by id) and never change the text directly.

- `KeyboardInput` turns a key press and the modifiers into a `Keybinding`, resolves it with
  `KeybindingResolver` (table: `LoomEditor.View/Commands/DefaultKeybindings.cs`) and runs the command. `KeyMapping` maps
  `VirtualKey` to `KeyCode`.
- Typed characters (`CharacterReceived`) go through `TextInputBuffer` (`LoomEditor.View/Controller/`) and then to
  `ViewModel.Type`; they do not go through commands (ADR 0006, decision 11).
- Later in phase 3: mouse selection, clipboard, IME via `CoreTextEditContext` (Windows.UI.Text.Core).
