# P17-05 — Share the constructor and the framework-free members of `CommentThreadWidget`

- **Status:** todo
- **Depends on:** P17-01
- **Size:** S

**Recommended model:** Sonnet

## Goal

The comment thread widget is assembled once (view model, Markdown builder, panels, automation id, first rebuild); the host parts keep the base class,
the framework-typed fields and the members whose signatures name framework types.

## Sources

Not a Monaco port. Read first: [phase-17/README.md](README.md) (rules). The shared parts P16-16 made: `src/LoomEditor.HostShared/Controls/Comments/CommentThreadWidget.Model.cs`
and `CommentThreadWidget.Suggestions.cs`; the host parts `CommentThreadWidget.Host.cs` (`SetAutomationName`, `SetAutomationHelp`).

## Duplicated block (SonarCloud, PR #26, 2026-11-03)

| Avalonia (`src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.cs`) | WinUI (same path) | What |
|---|---|---|
| 28+25 | 27+25 | the constructor |

## Files to create or change

- `git mv src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.cs src/LoomEditor.HostShared/Controls/Comments/CommentThreadWidget.cs`, then make it the shared
  part: the header block (with `using LoomEditor.WinUI.Controls.Markdown;` / `using LoomEditor.Avalonia.Controls.Markdown;` inside it), the class XML doc,
  `internal sealed partial class CommentThreadWidget` **without** base class, `_viewModel`, `_markdown`, `_fontSize`, the constructor, `ContentChanged`,
  `Thread`, `Announce`, `Detach`.
- The shared constructor, in this order: `ThrowIfNull`; `_viewModel`; `_markdown = new MarkdownElementBuilder(new MarkdownTheme { … })` (unchanged:
  it reads the host brush fields by name); `_panel.Children.Add(_rows)`; `_panel.Children.Add(CreateComposer())`; `BuildBox()`;
  `SetAutomationId("LoomEditor.CommentThread")`; `_viewModel.Changed += OnChanged`; `Rebuild()`.
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Host.cs` and the Avalonia twin — receive what stays per host: the base class
  (`internal sealed partial class CommentThreadWidget : UserControl`), `_border`, `_panel`, `_rows`, `_codeFont` (each host keeps its own default
  font), `MeasureContentHeight`, `ApplyFont`, `OnCreateAutomationPeer`, Avalonia's `Box` (for the tests), and two new host members:
  - `private void BuildBox()` — the five `_border` lines of today's constructor (thickness, corner radius, padding, background, border brush),
    `_border.Child = _panel;` and `Content = _border;`;
  - `private void SetAutomationId(string id) => AutomationProperties.SetAutomationId(this, id);`.
- `git rm src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.cs` (its content is now in the shared file or in `CommentThreadWidget.Host.cs`).
- `tests/LoomEditor.Architecture.Tests/HostTwinDuplicationTests.cs` — delete the `CommentThreadWidget.cs` pair from `KnownDuplicates`; if the test then
  reports a `CommentThreadWidget.Host.cs` pair, see the last design decision.

## Design decisions (already made — do not change)

- The border styling now runs after the panels are filled instead of before: these are independent property assignments, nothing is measured in
  between, so the result is the same.
- XML docs: the WinUI wording (shared file first), but host-neutral ("the editor's announcer"; no "WinUI" in a shared doc).
- Member order of the `CommentThreadWidget.Host.cs` files (phase 17 README, rule 4): fields, `MeasureContentHeight`, `BuildBox`, `OnCreateAutomationPeer`,
  (Avalonia: `Box`), `ApplyFont`, `SetAutomationId`, `SetAutomationName`, `SetAutomationHelp`. Identical host-only members must not line up into a
  100-token run; if the test reports one, reorder within the file, nothing else.

## Tests

- [ ] No new test. The Avalonia headless tests (`CommentThreadWidget*`) stay green **unchanged**.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green, the duplication test included
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] The pair is gone from `KnownDuplicates`, and no new pair was added
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The theme, header and composer parts of the widget (P17-06 … P17-08).

## Suggested commit message

`refactor(hosts): share the comment thread widget constructor`
