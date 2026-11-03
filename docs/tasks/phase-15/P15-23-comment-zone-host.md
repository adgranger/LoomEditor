# P15-23 — Comment zones: the widget between lines, its height, its automation peer

- **Status:** done
- **Depends on:** P15-22
- **Size:** M

**Recommended model:** Sonnet

## Goal

Comment threads open as zones between lines of the Avalonia `LoomEditor`. Their height follows their content, and screen readers see them. The comment feature is complete on
Avalonia.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/Comments/CommentZoneHost.cs`, `CommentZoneHost.Height.cs`, `CommentThreadAutomationPeer.cs`. Also read
`docs/tasks/phase-15/P15-20-comment-gutter.md` (the stub to delete).

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/Comments/` — the same 3 files (twins)
- `src/LoomEditor.Avalonia/Controls/LoomEditor.CommentThreads.cs` — remove the P15-20 stub, call the real zone host
- `tests/LoomEditor.Avalonia.Tests/Controls/CommentZoneTests.cs` — create
- `docs/testing/phase-15-manual-tests.md` — section "P15-23"

## Design decisions (already made — do not change)

- Height: measured with `widget.Measure(new Size(zoneWidth, double.PositiveInfinity))` and `DesiredSize.Height` where WinUI uses its measure path; the same debounce as the WinUI
  `Height` partial.
- `CommentThreadAutomationPeer : ControlAutomationPeer`, control type `Group`, same name.

## Tests

- [x] New: `OpenThread_InsertsZoneBelowLine`; `AddComment_GrowsZone`; `CloseThread_RemovesZone`; `Peer_IsGroup_WithThreadName`
- [x] Parity: no `AddCommentRequested`, `CommentSubmitted`, `CommentThread…` key left in `AvaloniaParityPending.txt` for `LoomEditor`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated

## Out of scope

Comment threads in the diff editor (P15-27).

## Suggested commit message

`feat(avalonia): comment thread zones`
