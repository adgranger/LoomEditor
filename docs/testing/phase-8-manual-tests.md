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

## Findings

| # | Row | What happened | Severity | Follow-up card |
|---|---|---|---|---|
| | | | | |
