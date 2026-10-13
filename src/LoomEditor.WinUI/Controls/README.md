# Controls

- `LoomEditor.cs` — the public editor control (template: `../Themes/Generic.xaml`).
- Phase 6: `Minimap`. The gutter is currently drawn by `TextViewRenderer` (LoomEditor.Rendering); it becomes a
  separate control only if it needs its own input handling (folding arrows, breakpoints).
