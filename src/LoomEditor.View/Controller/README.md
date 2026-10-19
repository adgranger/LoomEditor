# Controller

**Phase 3.** The pure logic of Monaco's `src/vs/editor/browser/controller/` and `view/viewController.ts`. It knows nothing
about Windows or UI: the WinUI wiring is in `LoomEditor.WinUI/Input/`.

- **Mouse**: `MouseTargetFactory` (hit testing on our own layout, `ILineHitTester`), `ViewController` (clicks to cursor
  commands), `MouseDownOperation` / `MouseDownState` (drag selection), `DragScrolling`.
- **Clipboard**: `ClipboardData` (what a copy puts on the clipboard and the metadata that makes the next paste smart),
  `ClipboardCommands`, `IClipboardService`.
- **Text input**: `TextInputBuffer` (typed characters, surrogate pairs), `EditContextState` / `EditContextModel` (the
  text and selection given to the IME, its updates as `Type` / `CompositionType`), `CompositionFormat` (the IME underlines).
