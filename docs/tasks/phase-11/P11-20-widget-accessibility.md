# P11-20 — Accessibility of the comment widget: UIA names, announcements, high contrast

- **Status:** todo
- **Depends on:** P11-19
- **Size:** M

## Goal

A screen reader user can find the thread, read each comment with its author and time, use every toolbar button by name, write, preview, post, cancel and apply a suggestion, and is told when a thread appears or a comment is posted. In Windows high contrast the widget uses system colors.

## Sources

Sources: none (specified in this card; ADR 0014 decision 8; the pattern of phase 7, ADR 0010). Read first: `src/LoomEditor.WinUI/Accessibility/README.md`, `AutomationAnnouncer.cs`, `LoomEditorAutomationPeer.cs` (does the peer's children list include overlay children? does it hide them?), `SystemHighContrast.cs`, `LoomEditor.HighContrast.cs`, `Find/FindWidget*.cs` (its automation names and high contrast handling: **copy the approach**), `docs/testing/phase-7-manual-tests.md` (the checks style).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget*.cs`, `CommentComposerToolbar.cs`, `SuggestionBlockView.cs` — change: `AutomationProperties` names, `AutomationProperties.AutomationId`s
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadAutomationPeer.cs` — create **only if** a plain `Border` cannot expose a useful name and role (`Group`); prefer `AutomationProperties.Name` + `AutomationProperties.LocalizedControlType` on the existing elements
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Composer.cs` — change: the `CommentDiscarded` announcement
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Theme.cs` — change: high contrast
- `docs/testing/phase-11-manual-tests.md` — change: section "P11-20"

## Design decisions (already made — do not change)

- **Names** (strings from `CommentStrings`, P11-05): widget `"Comment thread on line {N}"` (`AutomationProperties.Name`, kept current when the line moves), `AutomationId` `"LoomEditor.CommentThread"`; each comment row `"{author}, {relative time}"` followed by the plain text of the body in `FullDescription` (the Markdown element itself stays readable text); toolbar buttons their `ToolbarName`; text box `"Comment text"`; Preview toggle `"Preview"` (a `ToggleButton` exposes its toggle state by itself); `Cancel`, `Comment`, `Reply`, `Apply suggestion`; the suggestion block `"Suggested change"` with `FullDescription = diff.Describe()`.
- **Tab / reading order** is the visual order (verify, do not reorder with `TabIndex`).
- **Announcements:** the controller (P11-10) already announces `ThreadAdded` (only with `startComposing`), `CommentPosted` and `SuggestionApplied` through `EditorContext.Announcer` (the control sets it to `AutomationAnnouncer`, which raises a UIA notification `Other` / `ImportantMostRecent` on the editor's peer; there is no screen-reader gate: UIA drops notifications nobody listens to). This card adds only `CommentDiscarded` when a **non-empty** draft is cancelled (in the composer, through the same announcer) and verifies all four with Narrator.
- **UIA tree:** `LoomEditorAutomationPeer` overrides only class name, control type (`Edit`), name and patterns, not `GetChildrenCore`, so the XAML children of the template (the find widget today, the zone-widget layer now) are its UIA children. Verify with Accessibility Insights that each widget appears under the editor; if Narrator's text reading of the `Edit` control swallows them, report it (do not change the peer's control type in this card).
- **High contrast:** nothing to resolve by hand: in Windows high contrast the control passes its **system high contrast theme** (`LoomEditor.HighContrast.cs`, `EffectiveTheme`) to `ApplyOverlayWidgetsTheme`, so the widget's `ApplyTheme` already gets system colors; the `TextBox`, `Button`s and `ToggleButton` must **not** get brushes set by code while high contrast is on (`SystemHighContrast` tells; WinUI's own high contrast resources then apply). The suggestion rows keep their `-` / `+` markers; their backgrounds come from the high contrast theme's diff colors when it has them, else none.
- **Focus visuals:** every interactive element keeps WinUI's default focus rectangle (do not set `UseSystemFocusVisuals = false`).
- **Reduced motion / animations:** none exist in the widget (nothing to do; stated).

## Tests

- [ ] Automated: none for WinUI. If the names are built by pure functions (e.g. the comment row name), put them in `CommentStrings` / a View helper with unit tests (`CommentRowName_AuthorAndTime`, `ThreadName_UsesCurrentLine`).
- [ ] Manual (section "P11-20"; Narrator and Accessibility Insights; also NVDA if installed): (1) Insights shows the widget under the editor with the names above and no unnamed focusable element; (2) Narrator reads "Comment thread on line 13" when focus enters; each toolbar button by name; the text box as "Comment text, edit"; (3) the Preview toggle reads its state; (4) Apply suggestion reads the diff description; (5) the announcements above are spoken once; (6) high contrast (Aquatic and Desert themes): every control readable, focus visible, the diff still understandable without color; (7) `Tab` order = visual order; (8) the UIA text pattern of the **editor** is unchanged (the existing phase 7 self-check `--uia-selfcheck` still passes).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] The manual section is written; the card result states that Narrator, NVDA and Insights were **not** run by the agent
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Localized names, a landmark role, live regions for the Preview, automation of the Gallery with UIA clients, screen reader sounds.

## Suggested commit message

`feat(winui): add UI Automation names, announcements and high contrast to comment widgets`
