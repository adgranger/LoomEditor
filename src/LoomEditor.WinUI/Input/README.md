# Input

Keyboard, mouse/pointer, and IME (Input Method Editor, for Chinese/Japanese/Korean input) handling.
Input handlers translate WinUI events into **commands** (by id) and never change the text directly.

- Phase 0: `KeyboardInput` maps a few shortcuts to command ids.
- Phase 3: full keybinding table (`LoomEditor.View/Commands/DefaultKeybindings.cs`), mouse selection, IME via
  `CoreTextEditContext` (Windows.UI.Text.Core).
