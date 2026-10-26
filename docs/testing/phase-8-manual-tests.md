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

## 3. Completion list

Needs a provider: the Gallery's **Demo providers** (P8-37) or a temporary one. Rows are `pending` until one exists.

| # | Steps | Expected | Result |
|---|---|---|---|
| 3.1 | Type a letter of a word the provider knows | The list opens under the word after a very short delay, with the matching items first and the typed letters highlighted | pending |
| 3.2 | Press `Down` / `Up` / `PageDown` / `PageUp` | The selection moves, wraps at the ends, and the list scrolls when the selection leaves the 12 visible rows | pending |
| 3.3 | Press `Enter` (then repeat with `Tab`) | The word is replaced by the item's text, the caret is after it, the list closes; one `Ctrl+Z` undoes the completion | pending |
| 3.4 | Press `Esc`; click elsewhere; scroll the text | The list closes in each case | pending |
| 3.5 | Click a row | That item is inserted | pending |
| 3.6 | Wheel over the list with more than 12 items | The list scrolls and the text does not | pending |
| 3.7 | Select an item that has documentation | The strip under the list shows the detail and the first part of the documentation | pending |
| 3.8 | `Ctrl+Space` on an empty word | The list opens with every item | pending |
| 3.9 | A word near the bottom of the editor | The list opens above the word | pending |
| 3.10 | Change the theme | The list's colors follow the theme | pending |

## 4. Language server in the Gallery

| # | Steps | Expected | Result |
|---|---|---|---|
| 4.1 | `dotnet tool install --global csharp-ls`; run the Gallery; **Load LSP sample**; **Start language server** | The status goes `Starting…` then `Running (csharp-ls (dotnet tool))`; no window pops up for the server process | pending |
| 4.2 | Wait a few seconds after starting | A squiggle under `"text"` (error: cannot convert string to int) and a mark in the overview ruler; hovering it shows the compiler message | pending |
| 4.3 | Hover `Console` | A popup with the type's documentation (Markdown rendered as text) | pending |
| 4.4 | Place the caret after `Console.` and press `Ctrl+Space`; type `Wr` | A list of members, `WriteLine` among them; `Enter` inserts it | pending |
| 4.5 | Type `Console.` anew | The list opens by itself right after the `.` | pending |
| 4.6 | `F12` on `Program` in `Program.Greet();` (and `Ctrl+click`) | The caret jumps to the declaration `class Program` | pending |
| 4.7 | **Stop language server** | Status `Stopped`, the squiggles are gone, no `csharp-ls` process remains (`Get-Process csharp-ls*`) | pending |
| 4.8 | Start, then kill the server in Task Manager | Status `Faulted: …`, squiggles gone, the editor still types normally; **Start** works again | pending |
| 4.9 | Uninstall / rename `csharp-ls` and press **Start language server** | The status says the server was not found and shows the install command; nothing else changes | pending |
| 4.10 | Close the window while the server runs | No `csharp-ls` process remains | pending |
| 4.11 | Set `LOOMEDITOR_LSP_COMMAND` to another stdio server and `LOOMEDITOR_LSP_LANGUAGE` to its language id, start | The Gallery uses it (status description `LOOMEDITOR_LSP_COMMAND`) | pending |
| 4.12 | **Demo providers** then type, hover, `F12` (no server) | The widgets of sections 2 and 3 work with the in-process providers; **Demo markers** shows four squiggle kinds | pending |
## Findings

| # | Row | What happened | Severity | Follow-up card |
|---|---|---|---|---|
| | | | | |
