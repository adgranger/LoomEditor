# P17-09 — Remove the copies in the composer toolbar and the suggestion block

- **Status:** done
- **Depends on:** P17-08
- **Size:** S

**Recommended model:** Sonnet

## Goal

The composer toolbar's labels and enabling exist once; the suggestion block's host code no longer repeats the other host's.

## Sources

Not a Monaco port. Read first: [phase-17/README.md](README.md) (rules, especially rule 3). P16-17's Result (why these two classes were left per host).

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| Avalonia (`src/LoomEditor.Avalonia/Controls/Comments/…`) | WinUI (same path) | What |
|---|---|---|
| `CommentComposerToolbar.cs` 63+32 | 58+32 | `SetForeground` body, `Label`, `MakeTabStop` |
| `SuggestionBlockView.cs` 67+17 | 70+17 | constructor: scroll sizing, panel, the `Border` frame |
| `SuggestionBlockView.cs` 105+16 | 110+16 | end of `Row`, `Code` |

## Files to create or change

- `src/LoomEditor.HostShared/Controls/Comments/CommentComposerToolbar.Logic.cs` — new shared part (`internal sealed partial class CommentComposerToolbar`, no base
  class): `Label` (moved unchanged) and `SetEnabled` (`foreach (var button in _buttons) { button.IsEnabled = enabled; }`, its XML doc unchanged).
- `src/LoomEditor.WinUI/Controls/Comments/CommentComposerToolbar.cs` and the Avalonia twin — `Label` and `SetEnabled` go; the Avalonia class becomes
  `partial`. `MakeTabStop` (it sets `IsTabStop`) and `OnKeyDown` stay per host.
- `SuggestionBlockView` (both hosts):
  - `Code` becomes an instance method of the `SuggestionBlockTheme` record, `public TextBlock Code(string text)`, using the record's own `CodeFont`,
    `FontSize` and `Foreground` (the code text blocks belong with the code font); `Row` calls `theme.Code(…)`.
  - Avalonia only (phase 17 README, rule 3): the frame's `Margin = new Thickness(0, 4)` (Avalonia's horizontal/vertical form of `0, 4, 0, 4`).
- `tests/LoomEditor.Architecture.Tests/HostTwinDuplicationTests.cs` — delete the `CommentComposerToolbar.cs` and `SuggestionBlockView.cs` pairs from
  `KnownDuplicates`.

## Design decisions (already made — do not change)

- Same labels, same buttons, same frame, same rows: no property value changes.
- If the test still reports a `SuggestionBlockView.cs` block in the constructor, move the Avalonia automation statements (`SetControlTypeOverride`,
  `SetName`, `SetHelpText`) up, right after the `scroll.SizeChanged` line: they do not depend on the content. Do nothing else to that constructor.
- `SuggestionBlockView` stays a host class (no shared part): everything in it names framework types.

## Tests

- [x] No new test. The Avalonia headless tests of the toolbar (roving focus, enabling) and of suggestion blocks stay green **unchanged**.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green, the duplication test included
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [x] Both pairs are gone from `KnownDuplicates`, and no new pair was added
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The toolbar's keyboard handling (key types differ, not reported).

## Suggested commit message

`refactor(hosts): remove the copies in the composer toolbar and the suggestion block`
