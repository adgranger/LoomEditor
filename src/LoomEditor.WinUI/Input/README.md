# Input

Keyboard, mouse/pointer, and IME (Input Method Editor, for Chinese/Japanese/Korean input) handling.
Input handlers translate WinUI events into **commands** (by id) and never change the text directly.

- `KeyboardInput` turns a key press and the modifiers into a `Keybinding`, resolves it with
  `KeybindingResolver` (table: `LoomEditor.View/Commands/DefaultKeybindings.cs`) and runs the command. `KeyMapping` maps
  `VirtualKey` to `KeyCode`.
- Typed characters (`CharacterReceived`) go through `TextInputBuffer` (`LoomEditor.View/Controller/`) and then to
  `ViewModel.Type`; they do not go through commands (ADR 0006, decision 11).
- IME: `ImeInput` owns the `CoreTextEditContext` (Windows 11; absent on Windows 10) and an `EditContextModel`; it answers text and selection requests, turns updates into cursor calls and queues every `Notify*` call on the dispatcher. While `IsComposing`, `LoomEditor` ignores keys and characters.
- Mouse: `LoomEditor.Pointer.cs` turns WinUI pointer events (`PointerInput`) into `EditorMouseEvent`s for `MouseDownOperation`
  (click, drag, double/triple click, Alt+click, column selection, line numbers, drag scrolling). Touch and pen count as the
  left mouse button: there are no gestures in phase 3.
