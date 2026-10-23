# Phase 8 — manual test list

What unit tests cannot check: the language features under a real UI and a real language server.
Fill the **Result** column (`pass` / `fail` + a note) and the **Findings** table at the end.

- Windows build:
- Display scale:
- Language server (name and version):
- Date and tester:

## Setup

- `Editor.Markers.Change("manual", [new MarkerData(MarkerSeverity.Error, "x", 1, 1, 1, 4)])` puts an error mark in the overview ruler.
- Run the Gallery from a debugger with a breakpoint in a `MouseMove` handler (or the hover of P8-11) to see the events.

## 1. Squiggles and ruler

| # | Steps | Expected | Result |
|---|---|---|---|
| 1.1 | Gallery with a marker set from code (the Gallery's **Demo markers** button of P8-37, or the Setup line of the file) on `Console`, severities Error / Warning / Info / Hint on four words | Red zigzag, yellow zigzag, blue zigzag, three dots; the zigzag follows the text when you type before it | |
| 1.2 | Look at the overview ruler | A red / yellow / blue mark at the lines of the markers | |
| 1.3 | Switch the theme to a light one | The colors change with the theme | |

## 2. Hover popup

| # | Steps | Expected | Result |
|---|---|---|---|
| 2.1 | Rest the mouse on a word that has an error marker for about a second | A popup with the message appears under the word, with the theme's widget colors; moving the mouse off the word makes it go away | |
| 2.2 | Move the mouse from the word onto the popup | The popup stays; leaving the popup closes it | |
| 2.3 | With the popup open, press `Esc`; then scroll the wheel over the text; then type a character | Each of them closes it | |
| 2.4 | `Ctrl+K` then `Ctrl+I` with the caret on the word | The popup shows at once | |
| 2.5 | A word on the last visible line / near the bottom edge | The popup opens above the word | |
| 2.6 | A hover with Markdown (bold, code, a code block, a list) once P8-37 provides one | Rendered as text with bold / italic / monospace code; links are blue but do nothing | pending (needs P8-37) |
| 2.7 | Click and drag to select text across a word with a marker | No popup while dragging | |

## Findings

| # | Row | What happened | Severity | Follow-up card |
|---|---|---|---|---|
| | | | | |
