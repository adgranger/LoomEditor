# Accessibility

UI Automation support, so that screen readers (Narrator, NVDA, JAWS) can read and navigate the text.

- Phase 0: `LoomEditorAutomationPeer` exposes the control as an `Edit` control with a name.
- Phase 7: `ITextProvider`, `ITextProvider2`, `IValueProvider` and `ITextRangeProvider` (text, navigation, find, selection, scrolling), UIA events and notifications, screen reader and high contrast detection;
  the model they delegate to is in `LoomEditor.View/Accessibility`.

Files:

- `LoomEditorAutomationPeer.cs`: the UIA peer of the control.
- `ScreenReaderDetector.cs`: detects a running screen reader (`SPI_GETSCREENREADER`), used by `LoomEditor.AccessibilitySupport`.
- `LoomEditorTextRangeProvider.cs`: `ITextRangeProvider` over `AutomationTextRange` (read, navigation, find, selection and scrolling).
- `AutomationEventSink.cs`: raises the coalesced UIA events (`RaiseAutomationEvent`).
- `AutomationAnnouncer.cs`: announcements as UIA notifications (`RaiseNotificationEvent`).
- `SystemHighContrast.cs`: reads the Windows high contrast state and system colors.
- `RendererLineMetrics.cs`: line measures for UI Automation, taken from the renderer.

Manual test plan (Narrator, NVDA, Accessibility Insights): [docs/testing/phase-7-manual-tests.md](../../../docs/testing/phase-7-manual-tests.md).
