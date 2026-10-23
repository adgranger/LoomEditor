# P7-19 — UIA events, notifications and `ITextProvider2`

- **Status:** todo
- **Depends on:** P7-18
- **Size:** M

## Goal

Moving the caret or changing the text raises the UIA text events (Narrator speaks the character, word or line you moved to), the editor's announcements (find results, word highlight, tab focus mode) reach the screen reader as UIA
notifications, and `ITextProvider2.GetCaretRange` tells clients where the caret is.

## Sources

None to port. WinUI API reference: `Microsoft.UI.Xaml.Automation.Peers.AutomationPeer.RaiseAutomationEvent`, `AutomationPeer.ListenerExists`, `AutomationPeer.RaiseNotificationEvent`,
`FrameworkElementAutomationPeer.FromElement`, `AutomationEvents` (`TextPatternOnTextChanged`, `TextPatternOnTextSelectionChanged`), `AutomationNotificationKind`, `AutomationNotificationProcessing`,
`Microsoft.UI.Xaml.Automation.Provider.ITextProvider2`, `PatternInterface.TextPattern2`. The View classes it connects: `AutomationEventCoalescer` / `IAutomationEventSink` (P7-15), `IEditorAnnouncer` (P7-12).

## Files to create or change

- `src/LoomEditor.WinUI/Accessibility/AutomationEventSink.cs` — create (`IAutomationEventSink`)
- `src/LoomEditor.WinUI/Accessibility/AutomationAnnouncer.cs` — create (`IEditorAnnouncer`)
- `src/LoomEditor.WinUI/Accessibility/LoomEditorAutomationPeer.cs` — change: also implement `ITextProvider2`; `GetPatternCore(TextPattern2)`
- `src/LoomEditor.WinUI/Controls/LoomEditor.Accessibility.cs` — change: create and dispose the coalescer, assign the announcer; `internal bool AutomationHasFocus`
- `docs/testing/phase-7-manual-tests.md` — change: section 5 below

## Design decisions (already made — do not change)

- `AutomationEventSink(LoomEditorControl owner) : IAutomationEventSink`. `RaiseTextChanged()` → `Raise(AutomationEvents.TextPatternOnTextChanged)`; `RaiseSelectionChanged()` → `Raise(AutomationEvents.TextPatternOnTextSelectionChanged)`, with
  `void Raise(AutomationEvents e) { if (AutomationPeer.ListenerExists(e) && FrameworkElementAutomationPeer.FromElement(owner) is { } peer) { peer.RaiseAutomationEvent(e); } }`.
  `FromElement` returns the existing peer or `null` when no automation client ever asked for one: then nothing is raised (nobody listens). Do **not** call `CreatePeerForElement`: creating a peer just to raise an event would make the editor
  look "accessible" to tools that did not ask.
- `AutomationAnnouncer(LoomEditorControl owner) : IEditorAnnouncer`: `Announce(string message)`: ignores `null` or empty; `if (FrameworkElementAutomationPeer.FromElement(owner) is { } peer) { peer.RaiseNotificationEvent(AutomationNotificationKind.Other, AutomationNotificationProcessing.ImportantMostRecent, message, "LoomEditor.Announcement"); }`.
  The `activityId` is the constant `"LoomEditor.Announcement"` (Narrator and NVDA use it to group; one id for all messages).
- Control wiring (`LoomEditor.Accessibility.cs`): in the constructor, `_context.Announcer = new AutomationAnnouncer(this);`. In `OnLoaded`: `_automationEvents ??= new AutomationEventCoalescer(_context, new AutomationEventSink(this));` and in `OnUnloaded`: dispose it and set it to `null`
  (the editor may be loaded again: `OnLoaded` creates a new one). Both call sites already exist in `LoomEditor.cs`; add one line each.
- `ITextProvider2` members: `GetCaretRange(out bool isActive)`: `isActive = AutomationHasFocus` (the editor has the keyboard focus, `FocusState != Unfocused`; add `internal bool AutomationHasFocus => HasFocus;` to the control since `HasFocus` is private),
  returns a provider over the degenerate range at `owner.AutomationDocument.ViewModel.GetSelection().Position`; `RangeFromAnnotation(IRawElementProviderSimple annotationElement)` → `null` (no annotations). The peer's `GetPatternCore` returns `this` for `PatternInterface.TextPattern2`
  in addition to `Text` and `Value`.
- Events are raised on the UI thread by the coalescer's scheduler callback: no thread hopping here.
- The announcer is used by features through `ContextAnnouncer`; nothing else calls `RaiseNotificationEvent`.
- If `RaiseNotificationEvent` is not available in the installed Windows App SDK (compile error), stop and report — ADR 0010 decision 7 needs it; do not substitute `LiveRegionChanged`.

## Tests

No WinUI unit test; the coalescer and the messages are tested by P7-12 … P7-15. Manual checks, `docs/testing/phase-7-manual-tests.md` section 5 "Events and announcements" (Narrator: Win+Ctrl+Enter; NVDA: installed; Accessibility Insights → Events):

- [ ] 5.1 Accessibility Insights → Events, listen to the editor: pressing `Right` raises `TextSelectionChanged`; typing a letter raises `TextChanged` then `TextSelectionChanged`; holding `Right` does not freeze the editor
- [ ] 5.2 Narrator on, arrow keys: it reads the character after the caret (`Right`/`Left`), the word (`Ctrl+Right`/`Ctrl+Left`) and the line (`Down`/`Up`), the **view** line when word wrap is on
- [ ] 5.3 Narrator on, `Ctrl+F`, type a word present in the text → Narrator announces the "N of M found for 'word'" message (with the line text before it); a word that is absent → "No results found for 'word'"; `F3` in a state without matches repeats it
- [ ] 5.4 Narrator on, put the cursor on a word that occurs twice, `F7` → Narrator announces "line text, 2 of 2 for 'word'"
- [ ] 5.5 Narrator on, `Ctrl+M` twice → the two tab focus mode messages are spoken
- [ ] 5.6 Narrator off (and no other client): the same actions do not throw and the editor runs at normal speed (`FromElement` is `null`)
- [ ] 5.7 `ITextProvider2.GetCaretRange` in Accessibility Insights: `isActive` is `true` when the editor has the focus, `false` after clicking another control; the range is where the caret is
- [ ] 5.8 Repeat 5.1 – 5.5 on the Native AOT build (README §6)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` (README §6) — result reported in the commit body
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Focus-changed events (WinUI raises them), `LiveRegionChanged`, `StructureChanged`, `PropertyChanged` events of the Value pattern, announcements of typing.

## Suggested commit message

`feat(winui): raise the UI Automation text events and announcements`
