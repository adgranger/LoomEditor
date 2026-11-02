# Phase 14 — Diff line and block actions

**Done when:** a host using only the NuGet packages can offer stage / unstage / discard on one line, a modified pair or a block in the side-by-side view of
`LoomDiffEditor`: it reads the change blocks, shows its own action button on every block, lets the user tick changed lines (one side, or a deleted + added pair in one
gesture), and receives events with exact line ranges. `LoomDiffEditor` knows nothing about git. Works with word wrap on and off, light and dark themes, and files of tens
of thousands of lines.

Origin: VIGO bug report ("I only want to stage one line, but I have to tick both boxes on the left; Fork has a button for this"). Not a port (Monaco's
`revertButtonsFeature` / `gutterFeature` stay not ported). Design: [docs/tasks/phase-14/README.md](../tasks/phase-14/README.md). Task cards:
[docs/tasks/phase-14/](../tasks/phase-14/README.md) (6 cards).

## Checklist

- [x] `DiffChangeBlock`, `LoomDiffEditor.Changes` (P14-01)
- [x] Line selection model: `SelectedLines`, `SetSelection`, `ClearSelection`, `SelectionChanged` (P14-02)
- [x] Glyph lanes and per-block action buttons: `BlockActionsVisibility`, `BlockActionInvoked` (P14-03)
- [x] Line checkboxes, line hover button and key: `LineSelectionEnabled`, `LineActionInvoked` (P14-04)
- [x] Gallery demo and manual plan (P14-05)
- [x] Package test, AOT check, how-to, CHANGELOG (P14-06)
