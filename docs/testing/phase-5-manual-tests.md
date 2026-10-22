# Phase 5 — manual test list

What unit tests cannot check: how the widgets look, the keyboard and mouse under a real UI, the Native AOT build.
Run it on the **Native AOT** build of the Gallery, on Windows 11:

```powershell
dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64
```

and fill the **Result** column (`pass` / `fail` + a note).

- Windows build:
- Date and tester:

The Gallery has a **Features** picker: choosing an entry loads a sample (language, text, cursor) and shows a hint with
the shortcuts to try. Entries: Find / replace, Folding, Folding (off-side, Python / YAML), Brackets, Line operations,
Multi-cursor, Word highlight, Indentation (tabs), and one "Typing: <language>" entry per built-in language.

## 1. Find and replace (sample: Find / replace)

| # | Steps | Expected | Result |
|---|---|---|---|
| 1.1 | Put the cursor in `_count`, press Ctrl+F | The widget opens with the word at the cursor and shows "n of m" | |
| 1.2 | Enter / Shift+Enter | Next / previous match; both wrap around | |
| 1.3 | Toggle Alt+C, Alt+W, Alt+R; search `retry \d+` as a regex | Each toggle changes the matches as named (case, whole word, regex) | |
| 1.4 | Select the four `// log` lines, enable find in selection | Only matches inside the selection count | |
| 1.5 | Ctrl+H, replace one, then replace all | The matches are replaced; undo restores them | |
| 1.6 | Escape | The widget closes, focus returns to the editor | |
| 1.7 | Fold a region (Folding sample), then find a word inside it | The match unfolds the region | |

## 2. Folding (samples: Folding, Folding (off-side))

| # | Steps | Expected | Result |
|---|---|---|---|
| 2.1 | Move the mouse over / away from the gutter | Chevrons show on hover only | |
| 2.2 | Click a chevron | The region folds; the line shows `⋯` and the fold background | |
| 2.3 | Click the chevron or the `⋯` of a folded line | It unfolds | |
| 2.4 | Shift+click a chevron | The children fold first | |
| 2.5 | Ctrl+Shift+[ / ], Ctrl+K Ctrl+0 / J, Ctrl+K Ctrl+8 | Fold, unfold, fold all, unfold all, fold the `#region` | |
| 2.6 | Fold a block, type a line above it | The fold stays on its block | |
| 2.7 | Fold a block, move the cursor into it with the arrow keys | It unfolds | |
| 2.8 | Python and YAML samples: fold the blocks | The blank lines inside a block are folded with it | |

## 3. Brackets (sample: Brackets)

| # | Steps | Expected | Result |
|---|---|---|---|
| 3.1 | Look at the nested pairs, with **Dark Modern** then **Light Modern** | Pairs are colorized by depth; brackets in the string and the comment are not | |
| 3.2 | Put the cursor next to a bracket | The matching bracket is boxed after a short delay | |
| 3.3 | Ctrl+Shift+\ | The cursor jumps to the matching bracket | |

## 4. Typing helpers (samples: Typing: <language>)

| # | Steps | Expected | Result |
|---|---|---|---|
| 4.1 | For each language, type `{`, `(`, `[`, `"`, `'` | The closing character is added; typing it overtypes it | |
| 4.2 | Select a word and type a quote or a bracket | The selection is surrounded | |
| 4.3 | Type `{` then Enter | The cursor is on an indented line between the brackets | |
| 4.4 | Ctrl+/ on a line | Line comment toggled (block comment for CSS, HTML, XML) | |
| 4.5 | Select lines, Shift+Alt+A | Block comment toggled | |

## 5. Line operations and multi-cursor (samples: Line operations, Multi-cursor)

| # | Steps | Expected | Result |
|---|---|---|---|
| 5.1 | Alt+Up/Down, Shift+Alt+Up/Down, Ctrl+Shift+K, Ctrl+Enter, Ctrl+Shift+Enter | Move, copy, delete, insert below / above | |
| 5.2 | Same with several cursors (Ctrl+Alt+Down twice) | Each cursor's line is handled | |
| 5.3 | Ctrl+D on `alpha` | The next match is added; repeat adds more | |
| 5.4 | Ctrl+K Ctrl+D | The last added match is skipped (moved to the next) | |
| 5.5 | Ctrl+Shift+L | All occurrences are selected | |

## 6. Word highlight (sample: Word highlight)

| # | Steps | Expected | Result |
|---|---|---|---|
| 6.1 | Put the cursor on `_count` | The occurrences are highlighted after a short delay | |
| 6.2 | F7 / Shift+F7 | The cursor cycles through the occurrences | |
| 6.3 | Type a character | The highlights clear | |

## 7. Indentation (sample: Indentation (tabs))

| # | Steps | Expected | Result |
|---|---|---|---|
| 7.1 | Load the sample | The **Tab size** box shows the detected size; Tab inserts a tab character | |

## 8. Large file and Native AOT

| # | Steps | Expected | Result |
|---|---|---|---|
| 8.1 | Click **Large C# (20 000 lines)**, fold all (Ctrl+K Ctrl+0), scroll | Fluid; scrolling with folds is smooth | |
| 8.2 | Same file: put the cursor on an identifier | Word highlight stays fluid | |
| 8.3 | Repeat 1.1, 2.2, 3.2, 4.1 and 6.1 on the published build | Same results as the other builds | |
