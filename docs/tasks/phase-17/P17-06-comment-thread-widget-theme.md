# P17-06 — Share `CommentThreadWidget.ApplyTheme`

- **Status:** todo
- **Depends on:** P17-05
- **Size:** S

**Recommended model:** Sonnet

## Goal

The theme logic of the comment thread widget (which color id, which fallback, what high contrast turns off) exists once. Only the brush fields and
the calls that name framework properties stay per host.

## Sources

Not a Monaco port. Read first: [phase-17/README.md](README.md) (rules). Precedent: `src/LoomEditor.HostShared/Controls/Suggest/SuggestWidget.Theme.cs`
(shared `ApplyTheme` over host brush fields, host `Set`).

## Duplicated block (SonarCloud, PR #26, 2026-11-03)

| Avalonia (`src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.Theme.cs`) | WinUI (same path) | What |
|---|---|---|
| 9+33 | 9+33 | the nine brush fields and `ApplyTheme` up to the composer colors |

The two files differ only in the composer lines (`Control.XxxProperty` vs `TemplatedControl.XxxProperty`, `Control[]` vs `TemplatedControl[]`) and in
`Set` / `SetOrClear` (the color type).

## Files to create or change

- `git mv src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Theme.cs src/LoomEditor.HostShared/Controls/Comments/CommentThreadWidget.Theme.cs` — the shared
  part keeps only `ApplyTheme`. Its composer part becomes:

  ```csharp
  // The composer: a missing id keeps the framework default (the brush is cleared, not set); in high contrast nothing is set by code.
  SetComposerColors(
      highContrast ? null : theme.GetColor("input.background"),
      highContrast ? null : theme.GetColor("input.border"),
      highContrast ? null : theme.GetColor("input.foreground"),
      highContrast ? null : theme.GetColor("button.background"),
      highContrast ? null : theme.GetColor("button.foreground"));
  var plain = highContrast ? null : _foreground;
  SetPlain(_previewButton, plain);
  SetPlain(_cancelButton, plain);
  SetPlain(_replyButton, plain);
  _toolbar.SetForeground(plain);
  ```

- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Theme.Host.cs` — new: the nine brush fields, `SetOrClear`, the new host member
  `private void SetComposerColors(ThemeColor? inputBackground, ThemeColor? inputBorder, ThemeColor? inputForeground, ThemeColor? buttonBackground, ThemeColor? buttonForeground)`
  (the five `SetOrClear` calls of today, same order), `Set`, `SetPlain` (it stays `internal static`: `CommentComposerToolbar` calls it).
- `git mv src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.Theme.cs src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.Theme.Host.cs` —
  the same content as the WinUI host part, with the Avalonia property types (Avalonia's `SetPlain` stays in `CommentThreadWidget.Composer.cs`).
- `tests/LoomEditor.Architecture.Tests/HostTwinDuplicationTests.cs` — delete the `CommentThreadWidget.Theme.cs` pair from `KnownDuplicates`.

## Design decisions (already made — do not change)

- The three `SetPlain` calls replace the `foreach` over the array: same buttons, same order.
- XML docs of `ApplyTheme`: the Avalonia wording of the two `<param>` (host-neutral); the WinUI-specific detail ("WinUI's own high contrast resources
  apply") goes to the remarks of the WinUI `SetComposerColors`.
- Member order of each `.Theme.Host.cs` (phase 17 README, rule 4): fields, `SetOrClear`, `SetComposerColors`, `Set`, then `SetPlain` (WinUI). The fields are
  identical in both hosts; a member whose signature differs (`SetOrClear`) must follow them so they do not join a 100-token run.

## Tests

- [ ] No new test. The Avalonia headless tests (theme and high contrast of the comment widget) stay green **unchanged**.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green, the duplication test included
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] The pair is gone from `KnownDuplicates`, and no new pair was added
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Changing which colors high contrast keeps.

## Suggested commit message

`refactor(hosts): share the comment thread widget theme`
