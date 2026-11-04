# P17-08 — Share the composer actions and layout of `CommentThreadWidget`

- **Status:** done
- **Depends on:** P17-07
- **Size:** S

**Recommended model:** Sonnet

## Goal

What the composer's buttons do (Cancel, Reply) and how its parts are put together (toolbar slot, text box, preview, buttons, Reply) is written once.
The host parts keep the framework-specific wiring: focus, key handling, styles, visibility.

## Sources

Not a Monaco port. Read first: [phase-17/README.md](README.md) (rules). `CommentThreadWidget.Composer.cs` of both hosts (whole file).

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| Avalonia (`src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.Composer.cs`) | WinUI (same path) | What |
|---|---|---|
| 76+18 | 82+18 | `CreateComposer`: toolbar, Cancel handler, Submit |
| 105+17 | 115+17 | `CreateComposer`: the button row, the composer children, the area |

## Files to create or change

- Both hosts: `git mv …/Controls/Comments/CommentThreadWidget.Composer.cs …/Controls/Comments/CommentThreadWidget.Composer.Host.cs` (the host part keeps its
  file comment).
- `src/LoomEditor.HostShared/Controls/Comments/CommentThreadWidget.Composer.cs` — new shared part:
  - `FocusReturnRequested` (moved from both hosts, with its XML doc);
  - `private void CancelComposer()` — today's body of the Cancel `Click` lambda (draft discarded → `Announce`, then `FocusReturnRequested`);
  - `private void StartReply()` — `_viewModel.StartComposing(); FocusComposer();`;
  - `private void BuildComposerParts()` — `_toolbar = new CommentComposerToolbar(ApplyToolbarAction);`, `_toolbarSlot.Children.Add(_toolbar);`, the three
    `_composerButtons.Children.Add` (Preview, Cancel, Submit), the four `_composer.Children.Add` (toolbar slot, text box, preview, buttons) and the two
    `_composerArea.Children.Add` (Reply, composer), in today's order.
- Both host parts:
  - two new fields next to `_composer`: `_composerButtons` (today's `buttons` panel: horizontal, `Spacing = 8`, right-aligned) and `_composerArea`
    (today's `area`: `Spacing = 6`);
  - `CreateComposer` becomes: `BuildComposerParts();`, the automation name of the text box, `_cancelButton.Click += (_, _) => CancelComposer();`, Submit,
    the host's accent style, Preview, `_replyButton.Click += (_, _) => StartReply();`, `TextChanged`, the host's key handler, `return _composerArea;`.
- `tests/LoomEditor.Architecture.Tests/HostTwinDuplicationTests.cs` — delete the `CommentThreadWidget.Composer.cs` pair from `KnownDuplicates`.

## Design decisions (already made — do not change)

- The parts are put together before the handlers are attached instead of after: the handlers only run on user input, so nothing changes.
- `Click` / `TextChanged` subscriptions stay in the host parts (framework events); the shared code holds what the handlers do.
- `UpdateComposer`, `FocusComposer`, `HasFocusWithin`, `FromTextBox` / `ToTextBox`, the key handler and `SetPlain` (Avalonia) stay per host: they use
  `Visibility` / `IsVisible`, focus states and key types.

## Tests

- [x] No new test. The Avalonia headless tests of the composer (Cancel announcement, Reply, focus return) stay green **unchanged**.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green, the duplication test included
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [x] The pair is gone from `KnownDuplicates`, and no new pair was added (a `CommentThreadWidget.Composer.Host.cs` pair counts as new)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The text box properties and the Enter/Escape key handling.

## Suggested commit message

`refactor(hosts): share the comment composer actions and layout`
