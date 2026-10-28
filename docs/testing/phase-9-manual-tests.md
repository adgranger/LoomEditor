# Phase 9 — manual test list

What unit tests cannot check: the diff editor under a real UI (drawing, alignment, scrolling, pointer, Narrator).
Fill the **Result** column (`pass` / `fail` + a note, or `pending`) and the **Findings** table at the end.

- Windows build:
- Gallery build kind (Debug / Release / Native AOT):
- Display scale:
- Narrator version (section 8):
- Date and tester:

## Setup

- Two builds to test: the normal Debug / Release build, and the Native AOT publish:

  ```powershell
  dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true
  ```

- In the Gallery, open the **Phase 9: diff editor** section of the side panel and press **Diff editor window** (P9-31). The
  window shows two C# files (`DiffSamples`): an unchanged block of 30 lines at the top, `Describe` moved from the top to the end,
  one line of `Add` that only changes its indentation, `Remove` rewritten (inner character changes), `Clear` deleted,
  `GetLowStock` added, one word renamed in a long line (`reorder` → `restock`), 30 unchanged lines at the end.
- Switches of the window: *Side by side*, *Ignore trim whitespace*, *Hide unchanged regions*, *Render indicators*, *Original editable*,
  *Read-only modified*, *Overview ruler*. Buttons: *Previous change*, *Next change*, *Accessible viewer*, *Collapse all*, *Show all*,
  *Swap sides*, *Reset texts*, *Large files* (two texts of 5 000 lines, about 1 % of them changed).
- The status line shows `N change(s) — diff up to date: True / False`.
- The Gallery window also has a **Diff editor preview** switch (the diff editor in place of the editor of the main window); the plan
  below uses the separate window.

## 1. Basic diff (P9-22)

| # | Card | Steps | Expected | Result |
|---|---|---|---|---|
| 1.1 | P9-22 | Open the window | The two texts show side by side; the status line reaches `diff up to date: True` within a second | |
| 1.2 | P9-16, P9-17 | Look at the changed lines | Green background on added lines, red on deleted lines, with a darker highlight on the changed characters in `Remove` and on `reorder` / `restock` | |
| 1.3 | P9-22 | Drag the sash between the two editors; double click it | The two editors resize; the double click puts the sash back in the middle | |
| 1.4 | P9-22 | Type in the modified text (for example a new line in `Add`) | The highlights update after about 0.2 s; the status line goes to `False` then back to `True` | |
| 1.5 | P9-22 | Press **Swap sides**, then **Reset texts** | The sides swap (added lines become deleted ones), then the original samples come back | |

## 2. Alignment and scrolling (P9-19, P9-23)

| # | Card | Steps | Expected | Result |
|---|---|---|---|---|
| 2.1 | P9-19 | Look at the gap left where one side has no line (the deleted `Clear` on the modified side, the added `GetLowStock` on the original side) | A hatched (diagonal) area fills the gap | |
| 2.2 | P9-23 | Scroll with the wheel, with the vertical scrollbar of the left editor, then of the right one | Both editors stay level; the lines of an unchanged block face each other | |
| 2.3 | P9-23 | Scroll horizontally with the horizontal scrollbar of either side (make the window narrow, or type a very long line) | Both sides follow | |
| 2.4 | P9-23 | Resize the window and move the sash | The alignment is kept | |
| 2.5 | P9-23 | Insert several lines in the modified text | The sides are aligned again without flicker | |

## 3. Inline view (P9-24)

| # | Card | Steps | Expected | Result |
|---|---|---|---|---|
| 3.1 | P9-24 | Switch off *Side by side* | One editor; the deleted blocks appear in red inside it with the old line numbers and a `−` sign | |
| 3.2 | P9-24 | Switch *Side by side* on again, then make the window narrower than 900 DIPs | The control switches to the inline view by itself; making the window wider switches back | |
| 3.3 | P9-24 | Try to select the deleted text in the inline view | It cannot be selected (known difference, see 11) | |

## 4. Hide unchanged regions (P9-25)

| # | Card | Steps | Expected | Result |
|---|---|---|---|---|
| 4.1 | P9-25 | Switch on *Hide unchanged regions* | The long unchanged blocks collapse, 3 lines of context are kept, and a bar "N hidden lines" replaces each block | |
| 4.2 | P9-25 | Click the bar | All its lines show | |
| 4.3 | P9-25 | Click the top edge, then the bottom edge of a bar | 20 more lines show from that edge | |
| 4.4 | P9-25 | Drag an edge of a bar | The lines show one by one and the bar follows the pointer | |
| 4.5 | P9-25, P9-26 | With a collapsed block, press `Ctrl+End`; then `Ctrl+F` and search for text inside a hidden block; then **Next change** to a change in a hidden block | Each of them shows the hidden lines it needs | |
| 4.6 | P9-25 | Switch *Side by side* off | The bars still work in the inline view | |
| 4.7 | P9-25 | **Collapse all**, then **Show all** | Every block collapses, then every block is shown | |

## 5. Navigation (P9-26)

| # | Card | Steps | Expected | Result |
|---|---|---|---|---|
| 5.1 | P9-26 | Click in the modified editor, press `Alt+F5` repeatedly | The caret goes from change to change, the change is centered, and it wraps to the first one after the last | |
| 5.2 | P9-26 | Press `Shift+Alt+F5` repeatedly | The same backwards | |
| 5.3 | P9-26 | Use **Next change** / **Previous change** with the focus on the original editor | Same behavior; the focus ends in the modified editor | |

## 6. Overview ruler (P9-27, P9-28)

| # | Card | Steps | Expected | Result |
|---|---|---|---|---|
| 6.1 | P9-28 | Look at the strip on the right | Red marks (deleted lines) and green marks (added lines) line up with the changes; a translucent slider shows the visible part | |
| 6.2 | P9-28 | Scroll | The slider follows | |
| 6.3 | P9-28 | Click in the strip outside the slider; drag the slider | Both editors scroll; the click centers the slider on the pointer | |
| 6.4 | P9-28 | Turn the wheel over the strip | The editors scroll | |
| 6.5 | P9-28 | Switch off *Overview ruler* | The strip and its room disappear | |

## 7. Read-only (P9-20)

| # | Card | Steps | Expected | Result |
|---|---|---|---|---|
| 7.1 | P9-20 | Try typing, pasting, deleting and `Ctrl+Z` in the original editor | Nothing changes | |
| 7.2 | P9-20 | Switch on *Original editable* | The original editor accepts edits; the diff updates | |
| 7.3 | P9-20 | Switch on *Read-only modified* | The modified editor refuses typing | |
| 7.4 | P9-20 | In a read-only editor: select, copy, `Ctrl+F`, scroll | All of them work | |

## 8. Accessible diff viewer with Narrator (P9-30)

| # | Card | Steps | Expected | Result |
|---|---|---|---|---|
| 8.1 | P9-30 | Start Narrator, click in the modified editor, press `F7` (or **Accessible viewer**) | The viewer covers both editors, on the group nearest to the cursor; Narrator reads the header, like "Difference 1 of 3: original line …, 1 line changed, modified line …, 2 lines changed" | |
| 8.2 | P9-30 | Press `Down` / `Up` | The selected row is read: "- old text original line 3", "+ new text modified line 3", "text unchanged line 5" | |
| 8.3 | P9-30 | Press `F7` again, then `Shift+F7` | The next group, then the previous one | |
| 8.4 | P9-30 | Press `Enter` on a row | The viewer closes and the caret is on that line (a deleted row goes to the original editor; in the inline view to its block in the modified editor) | |
| 8.5 | P9-30 | Open it again, press `Esc` (or the Close button) | It closes and the focus returns to the modified editor | |
| 8.6 | P9-30 | While it is open, explore with Narrator | The two editors are not announced | |

## 9. Options (P9-12 … P9-17)

| # | Card | Steps | Expected | Result |
|---|---|---|---|---|
| 9.1 | P9-12 | Switch off *Ignore trim whitespace* | The line of `Add` that only changes its indentation shows as a change; switching it on makes it disappear | |
| 9.2 | P9-16 | Switch off *Render indicators* | The `+` / `−` signs in the margins disappear | |
| 9.3 | P9-16 | Switch to a high contrast theme | Everything stays readable (the dashed borders are solid: known difference) | |

## 10. Performance feel (P9-32)

| # | Card | Steps | Expected | Result |
|---|---|---|---|---|
| 10.1 | P9-32 | Press **Large files** | The status line reaches `diff up to date: True` in a few seconds, and the window does not freeze | |
| 10.2 | P9-32 | Scroll, type and resize | No visible lag | |

## 11. Known differences to confirm (from `PORTING.md`)

| # | Steps | Expected | Result |
|---|---|---|---|
| 11.1 | Look at `Describe`, moved from the top to the end of the class | It shows as one deletion and one insertion: moved blocks are not drawn | |
| 11.2 | Select deleted text in the inline view | It cannot be selected | |
| 11.3 | Type quickly in the modified text | The old highlights stay (stale) for a moment before the new diff arrives | |
| 11.4 | Use the accessible viewer, the navigation and the diff | No sounds | |
| 11.5 | Look for revert / stage buttons | There are none | |

## Findings

| # | Section | What happened | Severity |
|---|---|---|---|
| | | | |
