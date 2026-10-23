# Accessibility

UI Automation support, so that screen readers (Narrator, NVDA, JAWS) can read and navigate the text.

- Phase 0: `LoomEditorAutomationPeer` exposes the control as an `Edit` control with a name.
- Phase 7: `ITextProvider`, `IValueProvider` and the read members of `ITextRangeProvider` (P7-17, tracer bullet for managed
  providers under Native AOT); the model they delegate to is in `LoomEditor.View/Accessibility`.

Files:

- `LoomEditorAutomationPeer.cs`: the UIA peer of the control.
- `ScreenReaderDetector.cs`: detects a running screen reader (`SPI_GETSCREENREADER`), used by `LoomEditor.AccessibilitySupport`.
- `LoomEditorTextRangeProvider.cs`: `ITextRangeProvider` over `AutomationTextRange` (read members; moving and selecting come in P7-18).
- `RendererLineMetrics.cs`: line measures for UI Automation, taken from the renderer.
