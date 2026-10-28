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

## P10-05 — Font control properties

| # | Check | Result |
|---|---|---|
| 1 | The checks of P10-04 pass with the Gallery switches (Cascadia Code + Ligatures) | pending |
| 2 | Font weight Bold (700) on Cascadia Code is visibly bolder than Normal | pending |
| 3 | Letter spacing 5 widens the text; the caret and the selection stay aligned | pending |
| 4 | Switching any of these keeps the scroll position | pending |

## P10-06 — Editor zoom

| # | Check | Result |
|---|---|---|
| 1 | "A+" ×3: the text is 30 % bigger, the line height too, the scroll position is kept | pending |
| 2 | "A−" ×5 stops at -5 (50 %) | pending |
| 3 | "100 %" resets the zoom | pending |

## P10-07 — Ctrl + mouse wheel zoom

| # | Check | Result |
|---|---|---|
| 1 | Switch off: Ctrl + wheel scrolls as before | pending |
| 2 | Switch on: Ctrl + wheel zooms, plain wheel still scrolls | pending |
| 3 | Touchpad two-finger scroll with Ctrl zooms smoothly | pending |

## P10-09 — Render whitespace

| # | Check | Result |
|---|---|---|
| 1 | The five modes (None, Selection, Boundary, Trailing, All) on the sample: dots over spaces, arrows over tabs | pending |
| 2 | Tabs show arrows at the tab start, as wide as the tab | pending |
| 3 | With a proportional font (Segoe UI) nothing overlaps the text | pending |
| 4 | The dots stay aligned while scrolling horizontally | pending |
| 5 | High contrast theme: the dots are visible | pending |

## Findings

| # | Section | Finding | Severity |
|---|---|---|---|
