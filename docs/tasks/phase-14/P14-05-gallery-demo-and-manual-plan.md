# P14-05 — Gallery: diff actions demo and the complete manual plan

- **Status:** done
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

- [x] None automated. Manual plan complete; not run by the agent.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Any git behavior in the demo.

## Suggested commit message

`docs(gallery): demo of the diff block and line actions`

## Result

Build (0 warnings) and tests green (7 589, no new test). The Gallery diff demo has a **Staging** expander (`DiffDemoWindow.Actions.cs`) with the card's controls
and an event log, newest first. `docs/testing/phase-14-manual-tests.md` now names the real controls and the blocks of the staging sample (checked with a throwaway
diff test: `2,0 -> 2,1`, `4,1 -> 5,0`, `7,1 -> 7,1`, `10,2 -> 10,4`, `14,1 -> 16,1`); its checks were written, not run (agents do not run the Gallery UI).
Notes: so that the P14-03/P14-04 checks can be run as written, the expander also has **Stage action** (clears the primary glyph), **Word wrap**, **Line height 30**
and **Light theme**, and a **Selected:** text with the `SelectedLines` (the log keeps the card's `SelectionChanged {count}`). The plan gained a short P14-05 section
(3 checks). The primary glyph is `U+E710` as the card says (the P14-03 plan header said `U+E73E`, the checkbox's check mark).
