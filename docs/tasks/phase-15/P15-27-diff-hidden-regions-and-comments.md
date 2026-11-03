# P15-27 — Hidden unchanged regions and comment threads in the diff editor

- **Status:** todo
- **Depends on:** P15-26
- **Size:** M

**Recommended model:** Sonnet

## Goal

`HideUnchangedRegions` collapses unchanged regions with their expand controls. Comment threads (and `DiffAddCommentRequested`) work on both sides of the Avalonia
`LoomDiffEditor`.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/DiffEditor/HideUnchangedRegionsController.cs`, `LoomDiffEditor.CommentThreads.cs`.

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/DiffEditor/HideUnchangedRegionsController.cs`, `LoomDiffEditor.CommentThreads.cs` — create (twins)
- `src/LoomEditor.Avalonia/Themes/Icons.axaml` — the expand/fold icons the controller uses
- `tests/LoomEditor.Avalonia.Tests/Controls/DiffEditor/HiddenRegionsTests.cs`, `DiffCommentThreadsTests.cs` — create
- `PublicAPI.Unshipped.txt`, `AvaloniaParityPending.txt`

## Design decisions (already made — do not change)

- Same overlay controls as WinUI, placed through `OverlayHost` (P15-15).

## Tests

- [ ] New: `HideUnchanged_CollapsesLongUnchangedRun`; `ExpandControl_Click_ShowsLines`; `AddCommentOnModifiedSide_RaisesDiffAddCommentRequested_WithSide`;
  `ThreadOnOriginalSide_OpensZoneOnThatSide`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated

## Out of scope

The accessible viewer (P15-28).

## Suggested commit message

`feat(avalonia): hidden unchanged regions and diff comment threads`
