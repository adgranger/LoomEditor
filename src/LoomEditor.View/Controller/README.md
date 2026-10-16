# Controller

**Phase 3.** The pure logic of Monaco's `src/vs/editor/browser/controller/` (clipboard data, mouse hit testing, drag
scrolling, edit context state). It knows nothing about Windows or UI: the WinUI wiring is in `LoomEditor.WinUI/Input/`.

What is here so far:

- P3-36: the data a copy puts on the clipboard and the metadata that makes the next paste smart (`ClipboardData.cs`).
- P3-50: the text and selection given to the IME and the conversion of its updates into `Type` / `CompositionType` (`EditContextState.cs`, `EditContextModel.cs`).
