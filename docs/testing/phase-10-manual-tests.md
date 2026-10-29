# Phase 10 — manual test list

What unit tests cannot check: text rendering options, input and snippets under a real UI.
Fill the **Result** column (`pass` / `fail` + a note, or `pending`) and the **Findings** table at the end.

**Machine:** Windows 11, Cascadia Code installed, a mouse with a wheel and (optional) a touchpad. About 20 minutes in total.
Pick the sample of each section in the Gallery "Features" picker (its hint says what to try); the **Insert snippet**, **Insert snippet around selection** and
**Add sample problems** buttons are in the same Expander as **Inject text**.

**Self-check (no human needed):** publish the Gallery with Native AOT (`dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror`) and run
`LoomEditor.Gallery.exe --text-selfcheck=%TEMP%\text-selfcheck.txt`: the report ends with `n of n checks passed` and the exit code is 0.

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

## P10-10 — Control characters

| # | Check | Result |
|---|---|---|
| 1 | The sample line shows `a␁b␇c␡d�e`; the three pictures sit on dark red | pending |
| 2 | The caret moves one character per key press across them | pending |
| 3 | Switch off: the line looks as before, except the `�` | pending |

## P10-12 — Cursor styles and the Insert key

| # | Check | Result |
|---|---|---|
| 1 | `Insert` toggles overtype: the "Overtype" switch follows, and the caret becomes a block with the character visible inside | pending |
| 2 | In overtype mode, typing overwrites the next character | pending |
| 3 | Every style of the "Cursor style" combo box looks like Monaco's (line, block, underline, thin line, outlined block, thin underline) | pending |
| 4 | The caret still blinks | pending |
| 5 | In a long wrapped line the caret is on the right view line | pending |

## P10-13 — Selection anchor and Expand Line Selection

| # | Check | Result |
|---|---|---|
| 1 | `Ctrl+K Ctrl+B`, move the caret, `Ctrl+K Ctrl+K`: the text between is selected | pending |
| 2 | A blue 2 px bar marks the anchor while it is set | pending |
| 3 | `Escape` removes the bar (and does not close an open find widget or suggest list first) | pending |
| 4 | `Ctrl+L` selects the line, then the next line, repeatedly | pending |

## P10-14 — Cursor undo and redo

| # | Check | Result |
|---|---|---|
| 1 | Click in three places, press `Ctrl+U` twice: the caret goes back to each place, and the view scrolls back | pending |
| 2 | Typing text clears the history: `Ctrl+U` then does nothing | pending |

## P10-17 — Drag state of the mouse operation

| # | Check | Result |
|---|---|---|
| 1 | Select a word, press inside it and drag: the selection does not change (nothing moves yet: P10-18) | pending |
| 2 | Press inside the selection and release without moving: the selection stays as it was (P10-18 will collapse it) | pending |
| 3 | Press outside the selection, double-click, Alt+click: selecting with the mouse is unchanged | pending |


## P10-18 — Drag and drop of the selection

| # | Check | Result |
|---|---|---|
| 1 | Select a word and drag it elsewhere in the line, then to another line: the text moves and stays selected | pending |
| 2 | While dragging, a bar follows the pointer and the cursor is an arrow; with `Ctrl` held the text is copied instead | pending |
| 3 | Drag below the viewport end: the view scrolls to reveal the target | pending |
| 4 | `Escape` during a drag cancels it: no change | pending |
| 5 | One `Ctrl+Z` undoes a move | pending |
| 6 | A plain click inside a selection collapses it to the click position | pending |

## P10-25 — Snippet mode

Sample "Snippets", button **Insert snippet** (template `for (let ${1:i} = 0; $1 < ${2:array}.length; $1++) {\n\tconst ${3:element} = $2[$1];\n\t$0\n}`).

| # | Check | Result |
|---|---|---|
| 1 | Click **Insert snippet**: the four `i` are selected; typing replaces them all | pending |
| 2 | `Tab` goes to `array`, `Tab` to `element`, `Tab` to the line between the braces (`$0`) and snippet mode ends | pending |
| 3 | `Shift+Tab` goes back to the previous tab stop | pending |
| 4 | `Escape` leaves snippet mode and keeps only the primary cursor; the text stays | pending |
| 5 | Select `foo` then click **Insert snippet around selection**: `console.log('foo', foo);` replaces it | pending |
| 6 | `Ctrl+Z` after the insertion removes the snippet in one step and leaves snippet mode | pending |

## P10-26 — Completion items that are snippets

Language server demo of phase 8 with the fake server as the server: set `LOOMEDITOR_LSP_COMMAND` to the `LoomEditor.Lsp.FakeServer` executable with the argument `--snippet`
before starting the Gallery (see `tests/LoomEditor.Lsp.FakeServer/README.md`).

| # | Check | Result |
|---|---|---|
| 1 | Type `forlo`: the `forloop` item appears; `Enter` inserts `for (i = 0; i < n; i++) {` with three `i` selected (the second and third follow while typing) | pending |
| 2 | `Tab` goes to `n`, `Tab` to the body line, then snippet mode ends | pending |
| 3 | `Ctrl+Z` removes the whole snippet in one step | pending |

## P10-28 — Go to next / previous problem

Sample "Problems", button **Add sample problems** (2 errors, 2 warnings, 1 info).

| # | Check | Result |
|---|---|---|
| 1 | `F8` moves the caret to each problem in turn (errors first), wrapping around; `Shift+F8` goes back | pending |
| 2 | Narrator reads the line, the severity, the message, the position and "n of m problems" on each move | pending |
| 3 | `Escape` stops the navigation; the next `F8` starts again from the caret | pending |
| 4 | Moving the caret away from the current problem, then `F8`, goes to the next problem after the caret | pending |

## Findings

| # | Section | Finding | Severity |
|---|---|---|---|
