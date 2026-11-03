# P17-10 — Remove the copies in the hover and suggest widgets

- **Status:** todo
- **Depends on:** P17-01
- **Size:** S

**Recommended model:** Sonnet

## Goal

The completion list's sizes, rows array, state and highlighted-label splitting exist once; the hover's frame no longer repeats the other host's.

## Sources

Not a Monaco port. Read first: [phase-17/README.md](README.md) (rules). `src/LoomEditor.View/Suggest/SuggestRowModel.cs` and
`tests/LoomEditor.View.Tests/Suggest/SuggestRowModelTests.cs`; the shared `SuggestWidget.Theme.cs` / `HoverWidget.Theme.cs` (P16-17).

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| Avalonia (`src/LoomEditor.Avalonia/Controls/…`) | WinUI (same path) | What |
|---|---|---|
| `Hover/HoverWidget.cs` 28+22 | 28+22 | constructor: the `Border` / `ScrollViewer` frame, pointer events, `PointerOverChanged` |
| `Suggest/SuggestWidget.cs` 16+20 | 14+20 | constants, brush fields, `_rows`, `_details` |
| `Suggest/SuggestWidget.Rows.cs` 57+21 | 58+21 | `Fill`: the highlighted label runs, the description |

## Files to create or change

- `src/LoomEditor.View/Suggest/SuggestRowModel.cs` — new method
  `public IReadOnlyList<(string Text, bool IsMatch)> LabelSegments()`: today's loop of `Fill` (clamp each match to the label, the plain text before it,
  the match, then the rest), **including empty segments** in the same places, so the hosts' `AddRun` (which skips empty text) sees the same calls.
- `tests/LoomEditor.View.Tests/Suggest/SuggestRowModelTests.cs` — tests for `LabelSegments` (see Tests).
- `SuggestWidget.Rows.cs` (both hosts) — the loop and the last `AddRun` of the label become
  `foreach ((string text, bool isMatch) in model.LabelSegments()) { AddRun(label, text, isMatch ? matchBrush : null, isMatch); }`.
- `src/LoomEditor.HostShared/Controls/Suggest/SuggestWidget.Logic.cs` — new shared part: `WidgetWidth`, `RowHeight`, `_rows`, `_state`, `RowPressed`
  (moved from both `SuggestWidget.cs`; `SuggestRow` is the host's private nested class, reachable by name).
- `src/LoomEditor.Avalonia/Controls/Suggest/SuggestWidget.cs` — `_details` gets `Margin = new Thickness(8, 4)` (phase 17 README, rule 3).
- `src/LoomEditor.Avalonia/Controls/Hover/HoverWidget.cs` — the frame gets `Padding = new Thickness(8, 4)` (rule 3).
- `tests/LoomEditor.Architecture.Tests/HostTwinDuplicationTests.cs` — delete the `HoverWidget.cs`, `SuggestWidget.cs` and `SuggestWidget.Rows.cs` pairs from
  `KnownDuplicates`.

## Design decisions (already made — do not change)

- `LabelSegments` goes to View, not HostShared: it names no host type (ADR 0017 decision 8). `SuggestRowModel` is internal, so no public API change.
- If the test still reports `SuggestWidget.cs`, declare `_details` above the brush fields (rule 4). If it still reports `HoverWidget.cs`, move the
  `PointerOverChanged` event above the constructor. Nothing else.
- Same rows, same brushes, same sizes: no value changes.

## Tests

- [ ] `LabelSegments_NoMatch_ReturnsTheWholeLabel`, `LabelSegments_Matches_AlternatePlainAndMatchedText`,
  `LabelSegments_MatchPastTheEnd_IsClamped` (a match beyond `Label.Length`, as the clamps handle today) in `SuggestRowModelTests`.
- [ ] The Avalonia headless tests of the suggest and hover widgets stay green **unchanged**.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green, the duplication test included
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] The three pairs are gone from `KnownDuplicates`, and no new pair was added
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`HoverWidget.Update` and the Markdown rows (not reported).

## Suggested commit message

`refactor(hosts): remove the copies in the hover and suggest widgets`
