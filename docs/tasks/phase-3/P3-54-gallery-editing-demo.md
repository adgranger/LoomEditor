# P3-54 — Gallery: editing demo and the phase 3 manual test list

- **Status:** done
- **Depends on:** P3-53
- **Size:** S

## Goal

Make phase 3 visible in the Gallery and leave a repeatable manual test list for what unit tests cannot check
(keyboard layouts, IME, mouse, clipboard with other apps). The phase 3 "done when" — *Japanese and Chinese IME input
works* — is checked here and recorded.

## Monaco sources

None (sample app and docs).

## Files to create or change

- `samples/LoomEditor.Gallery/MainWindow.xaml(.cs)` — title "phase 3: editing"; a toggle **Read-only** (`IsReadOnly`);
  the status line also shows the number of cursors and the primary selection (refreshed on `Editor.SelectionChanged`);
  a **Sample: multi-cursor** button that loads a short text and sets three cursors with `Editor.SetSelections`
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs` — change: public `IReadOnlyList<Selection> Selections { get; }`,
  `void SetSelections(IReadOnlyList<Selection> selections)` and `event EventHandler? SelectionChanged`, forwarding to the
  `EditorContext` (P3-21). The control has no public selection API before this card: the Gallery needs one.
- `docs/testing/phase-3-manual-tests.md` — create: the manual test list (below), with a "Result" column

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- The new public members of the control only forward to `EditorContext`: no new behavior. XML doc on each.
- The manual test list groups the checks of P3-41, P3-46, P3-47, P3-51, P3-52, P3-53 in one file, each with steps and
  the expected result, in this order: keyboard (US, French with dead keys and AltGr), mouse, multi-cursor, clipboard
  (with Notepad), undo/redo, read-only, IME Japanese, IME Chinese (Pinyin), IME Korean, emoji panel, display scales
  (100 %, 150 %), Native AOT build.

## Tests

- [x] Existing tests pass
- [x] Manual: run the whole list of `docs/testing/phase-3-manual-tests.md` on the **AOT** build on Windows 11 and fill the
      Result column (pass / fail + note). Record the Windows build and the IME versions at the top of the file
- [x] The IME rows (Japanese and Chinese) pass — this is the phase 3 "done when". If they fail, **stop**: set the card
      to `blocked` in the index and propose a follow-up card

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings
- [x] `PublicAPI.Unshipped.txt` of `LoomEditor.WinUI`: only `Selections`, `SetSelections`, `SelectionChanged`
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

New editing features; performance work.

## Suggested commit message

`feat(gallery): editing demo and phase 3 manual test list`

## Note

The manual list was run by the maintainer on 2026-10-19 on the AOT build: every row passes except 9.1 (Korean IME) and 12.2 (Windows 10), which were not tested. The IME rows 7.1 and 8.1 pass.
