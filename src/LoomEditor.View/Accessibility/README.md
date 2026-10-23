# Accessibility

**Phase 7.** The UI-free model of what UI Automation sees of the editor, with no WinUI types. The spec of the text units and of `Move` is in `docs/tasks/phase-7/README.md` §4.

- `AutomationTextDocument`: the model text as `
`-joined lines (positions, validation, `GetText`).
- `AutomationTextRange.cs`, `AutomationTextRange.Navigation.cs`: a mutable range over a document (clone, compare, read, expand, move, find).
- `AutomationTextSelection.cs`, `AutomationTextEndpoint.cs`, `AutomationTextUnit.cs`: selection ranges, the endpoint and unit enums.
- `AutomationUnitStarts.cs` and `CharacterUnitStarts.cs`, `WordUnitStarts.cs`, `LineUnitStarts.cs`, `ParagraphUnitStarts.cs`, `PageUnitStarts.cs`, `DocumentUnitStarts.cs`: where each text unit starts.
- `AutomationTextGeometry.cs`, `AutomationRect.cs`, `IAutomationLineMetrics.cs`: bounding rectangles and points.
- `AutomationEventCoalescer.cs`, `IAutomationEventSink.cs`: one UIA event per burst of selection or text changes.
- `IEditorAnnouncer.cs`, `ContextAnnouncer.cs`, `AccessibilityMessages.cs`: announcements and the Monaco message texts.
