# Phase 10 — manual test list

What unit tests cannot check: text rendering options, input and snippets under a real UI.
Fill the **Result** column (`pass` / `fail` + a note, or `pending`) and the **Findings** table at the end.

- Windows build:
- Gallery build kind (Debug / Release / Native AOT):
- Display scale:
- Date and tester:

## P10-04 — Font options in DirectWrite

Needs Cascadia Code (installed on every Windows 11) and the Gallery switches added by P10-05.

| # | Check | Result |
|---|---|---|
| 1 | Ligatures on: `=>`, `!=`, `->`, `<=` are fused. Off: separate characters | pending |
| 2 | `Left` / `Right` across `=>` takes two key presses and the caret is drawn between the two halves | pending |
| 3 | A click on the right half of `=>` places the caret between `=` and `>` | pending |
| 4 | Letter spacing 2 widens the text; the caret and the selection follow | pending |

## Findings

| # | Section | Finding | Severity |
|---|---|---|---|
