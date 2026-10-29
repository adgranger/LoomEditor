# P11-16 — Accessibility of the comment widget: UIA names, announcements, high contrast

- **Status:** todo
- **Depends on:** P11-15
- **Size:** M

## Goal

A screen reader user can find the thread, read each comment with its author and time, use every toolbar button by name, write, preview, post, cancel and apply a suggestion, and is told when a thread appears or a comment is posted. In Windows high contrast the widget uses system colors.

## Sources

Sources: none (specified in this card; ADR 0014 decision 8; the pattern of phase 7, ADR 0010). Read first: `src/LoomEditor.WinUI/Accessibility/README.md`, `AutomationAnnouncer.cs`, `LoomEditorAutomationPeer.cs` (does the peer's children list include overlay children? does it hide them?), `SystemHighContrast.cs`, `LoomEditor.HighContrast.cs`, `Find/FindWidget*.cs` (its automation names and high contrast handling: **copy the approach**), `docs/testing/phase-7-manual-tests.md` (the checks style).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget*.cs`, `CommentComposerToolbar.cs`, `SuggestionBlockView.cs` — change: `AutomationProperties` names, `AutomationProperties.AutomationId`s
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadAutomationPeer.cs` — create **only if** a plain `Border` cannot expose a useful name and role (`Group`); prefer `AutomationProperties.Name` + `AutomationProperties.LocalizedControlType` on the existing elements
- `src/LoomEditor.WinUI/Controls/Comments/CommentZoneHost.cs` — change: announcements
- `src/LoomEditor.WinUI/Accessibility/LoomEditorAutomationPeer.cs` — change only if the overlay widgets are not reachable in the UIA tree
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Theme.cs` — change: high contrast
- `docs/testing/phase-11-manual-tests.md` — change: section "P11-16"

## Design decisions (already made — do not change)

- **Names** (strings from `CommentStrings`, P11-07): widget `"Comment thread on line {N}"` (`AutomationProperties.Name`, kept current when the line moves), `AutomationId` `"LoomEditor.CommentThread"`; each comment row `"{author}, {relative time}"` followed by the plain text of the body in `FullDescription` (the Markdown element itself stays readable text); toolbar buttons their `ToolbarName`; text box `"Comment text"`; Preview toggle `"Preview"` (a `ToggleButton` exposes its toggle state by itself); `Cancel`, `Comment`, `Reply`, `Apply suggestion`; the suggestion block `"Suggested change"` with `FullDescription = diff.Describe()`.
- **Tab / reading order** is the visual order (verify, do not reorder with `TabIndex`).
- **Announcements** through `AutomationAnnouncer` (notification kind and activity ids as the existing callers; **polite**): thread added from the API with `startComposing` → `"Comment thread added on line {N}"`; comment added → `"Comment posted"`; suggestion applied → `"Suggestion applied"`; composer cancelled with text → `"Comment discarded"`. No announcement for threads added silently by a host loading existing data (a `startComposing == false` add stays quiet) or while a screen reader is not running (use the same detector gate the existing announcements use).
- **UIA tree:** the widget must be reachable as a child of the editor in the UIA tree (Accessibility Insights shows it under the editor control, not detached). If the peer or the template hides overlay children, fix the minimum and say so in the card result.
- **High contrast:** when `SystemHighContrast` is on, background / foreground / border / button colors come from the system colors (`Window`, `WindowText`, `GrayText`, `Highlight`, `ButtonFace`…) exactly as the find widget resolves them; the diff rows of the suggestion block use `Window` background and the `-` / `+` markers carry the meaning.
- **Focus visuals:** every interactive element keeps WinUI's default focus rectangle (do not set `UseSystemFocusVisuals = false`).
- **Reduced motion / animations:** none exist in the widget (nothing to do; stated).

## Tests

- [ ] Automated: none for WinUI. If the names are built by pure functions (e.g. the comment row name), put them in `CommentStrings` / a View helper with unit tests (`CommentRowName_AuthorAndTime`, `ThreadName_UsesCurrentLine`).
- [ ] Manual (section "P11-16"; Narrator and Accessibility Insights; also NVDA if installed): (1) Insights shows the widget under the editor with the names above and no unnamed focusable element; (2) Narrator reads "Comment thread on line 13" when focus enters; each toolbar button by name; the text box as "Comment text, edit"; (3) the Preview toggle reads its state; (4) Apply suggestion reads the diff description; (5) the announcements above are spoken once; (6) high contrast (Aquatic and Desert themes): every control readable, focus visible, the diff still understandable without color; (7) `Tab` order = visual order; (8) the UIA text pattern of the **editor** is unchanged (the existing phase 7 self-check `--uia-selfcheck` still passes).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] The manual section is written; the card result states that Narrator, NVDA and Insights were **not** run by the agent
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Localized names, a landmark role, live regions for the Preview, automation of the Gallery with UIA clients, screen reader sounds.

## Suggested commit message

`feat(winui): add UI Automation names, announcements and high contrast to comment widgets`
