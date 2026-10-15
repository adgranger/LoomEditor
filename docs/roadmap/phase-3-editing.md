# Phase 3 — Editing: selection, multi-cursor, keyboard, mouse, clipboard, IME

**Done when:** Japanese and Chinese IME input works.

Step-by-step task cards: [docs/tasks/README.md](../tasks/README.md) (P3-01 … P3-55). Design: [ADR 0006](../adr/0006-editing-and-text-input.md).

- [ ] Prerequisites in Core: graphemes, cursor columns, word classifiers, interval tree and tracked ranges, model options, identified edits, auto-whitespace trimming — P3-01 … P3-11
- [ ] Cursors and movements (port `editor/common/cursor/*` and their tests) — P3-12 … P3-30
- [ ] Typing, deleting, Tab, paste, copy/cut, undo/redo (no language configuration until phases 4–5) — P3-31 … P3-37
- [ ] `DefaultKeybindings.cs` (VS Code default shortcuts), keyboard handling, reveal — P3-38 … P3-41
- [ ] Mouse selection, double/triple click, drag, column selection; several cursors drawn — P3-42 … P3-46
- [ ] Clipboard (multi-cursor aware) — P3-36, P3-47
- [ ] IME through `CoreTextEditContext` (Windows.UI.Text.Core; Windows 11, plain typing without IME on Windows 10) — P3-48 … P3-53
- [ ] Proof: Gallery editing demo and the manual test list — P3-54, P3-55
