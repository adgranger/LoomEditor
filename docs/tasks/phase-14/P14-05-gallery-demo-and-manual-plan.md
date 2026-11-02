# P14-05 — Gallery: diff actions demo and the complete manual plan

- **Status:** todo
- **Depends on:** P14-04
- **Size:** S

**Recommended model:** Sonnet

## Goal

The Gallery's diff demo shows several blocks with their action buttons and line selection, and logs every phase 14 event, so the manual checks of P14-03 and
P14-04 can be run as written.

## Sources

Sources: none (specified in this card). Read first: `samples/LoomEditor.Gallery/DiffDemoWindow.xaml`, `DiffDemoWindow.xaml.cs`, `DiffDemoWindow.Comments.cs` (log and
toggles pattern), `docs/testing/phase-14-manual-tests.md`.

## Files to create or change

- `samples/LoomEditor.Gallery/DiffDemoWindow.Actions.cs` — create
- `samples/LoomEditor.Gallery/DiffDemoWindow.xaml` — change: a "Staging" expander with toggles
- `docs/testing/phase-14-manual-tests.md` — complete (replace every "(Gallery, P14-05)" with the real control names)

## Design decisions (already made — do not change)

- Controls: a combo `BlockActionsVisibility` (Off/OnHover/Always), a toggle "Discard action" (sets the secondary glyph `` / "Discard block", null when off),
  a toggle `LineSelectionEnabled`, a toggle "Line hover button" (`LineActionGlyph` `` / "Stage lines"), buttons "Clear selection" and "Select all added lines"
  (`SetSelection` with every modified line of `Changes`), a "Large file" button (50 000 lines, ~2 000 blocks, generated with `new Random(14)`).
- Primary glyph `` / tooltip "Stage block". The log line formats: `BlockActionInvoked {Action} {block}`, `LineActionInvoked {lines}`, `SelectionChanged {count}`,
  `DiffChanged {Changes.Count} blocks` (`block.ToString()`; lines as `O7 M7`).
- The demo texts: the existing sample, plus a "Staging sample" button loading a pair with 5 blocks: a pure insertion, a pure deletion, a modified pair, a 2→4 block,
  and a CRLF original with an LF modified text.

## Tests

- [ ] None automated. Manual plan complete; not run by the agent.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Any git behavior in the demo.

## Suggested commit message

`docs(gallery): demo of the diff block and line actions`
