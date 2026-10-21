# Controls

- `LoomEditor.cs` — the public editor control (template: `../Themes/Generic.xaml`).
- `Find/FindWidget.xaml` — the find widget (see `Find/README.md`); `LoomEditor.Find.cs` connects it to the find controller.
- Phase 6: `Minimap`. The gutter is currently drawn by `TextViewRenderer` (LoomEditor.Rendering); it becomes a
  separate control only if it needs its own input handling (folding arrows, breakpoints).
