# P15-25 — `LoomDiffEditor` options and change navigation

- **Status:** todo
- **Depends on:** P15-24
- **Size:** M

**Recommended model:** Sonnet

## Goal

Every diff option property (side by side, ignore whitespace, word wrap, line numbers on the sides, …) and `GoToNextChange` / `GoToPreviousChange` (Alt+F5 / Shift+Alt+F5) work on
Avalonia as on WinUI.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.Options.cs`, `LoomDiffEditor.Navigation.cs`.

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.Options.cs`, `LoomDiffEditor.Navigation.cs` — create (twins)
- `tests/LoomEditor.Avalonia.Tests/Controls/DiffEditor/DiffOptionsTests.cs` — create
- `PublicAPI.Unshipped.txt`, `AvaloniaParityPending.txt`

## Design decisions (already made — do not change)

- `StyledProperty<T>` per WinUI DP; forwarding to the side editors as the twin. `Changes` (phase 14) is part of `LoomDiffEditor.cs` or its partial in WinUI: port it where WinUI has it.

## Tests

- [ ] New: `RenderSideBySide_False_ShowsInline`; `IgnoreTrimWhitespace_WhitespaceOnly_NoChange`; `AltF5_GoesToNextChange`; `ShiftAltF5_GoesToPrevious`; `Changes_MatchesDiff`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated

## Out of scope

Zones and overview (P15-26).

## Suggested commit message

`feat(avalonia): LoomDiffEditor options and change navigation`
