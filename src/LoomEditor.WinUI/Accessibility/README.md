# Accessibility

UI Automation support, so that screen readers (Narrator, NVDA, JAWS) can read and navigate the text.

- Phase 0: `LoomEditorAutomationPeer` exposes the control as an `Edit` control with a name.
- Phase 7: implement `ITextProvider` / `ITextRangeProvider` (Monaco equivalent: the hidden textarea and
  `src/vs/editor/browser/controller/editContext/`).

Files:

- `LoomEditorAutomationPeer.cs`: the UIA peer of the control.
- `ScreenReaderDetector.cs`: detects a running screen reader (`SPI_GETSCREENREADER`), used by `LoomEditor.AccessibilitySupport`.
