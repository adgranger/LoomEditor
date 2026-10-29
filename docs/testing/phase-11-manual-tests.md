# Phase 11 — manual test list

What unit tests cannot check: the inline comment threads under a real UI (layout over view zones, focus, keyboard, IME, screen readers,
themes) in the editor and the diff editor. Fill the **Result** column (`pass` / `fail` + a note, or `pending`) and the **Findings** table at the end.

**Machine:** Windows 11, a mouse with a wheel, Narrator (built in), Accessibility Insights for Windows, NVDA if installed, a Japanese or Chinese
IME for P11-14. About 90 minutes in total.

**Gallery setup:** pick **Comment threads** in the "Features" picker: a C# file with three threads — line 8 (Ada Lovelace, Markdown), line 10
`int max = 10;` (a comment, a reply and a suggestion to `const int Max = 10;`), line 24 (a long line, a resolved thread). The **Comment threads**
Expander holds **Add comment at caret**, **Add 20 threads**, **Remove all threads**, **Resolve first thread**, the **Gutter "+" button** switch, a
diagnostics line (`zones … visible … lastArrange … heightPasses … heightLimitHits …`) and the event log (newest first). The Gallery is the host:
it adds a posted comment as "You", answers an add request with a composer on that line, and marks a thread resolved when its suggestion is
applied. `Ctrl+Alt+M` (a Gallery shortcut) runs the command `loom.commentThreads.add`. The **Diff editor window** has a **Comment sample**
button, **Add on original** / **Add on modified** (caret line of that side), a **Gutter "+" button** box (both sides) and a one-line event log.

**Self-check (no human needed):** publish the Gallery with Native AOT (`dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror`)
and run `LoomEditor.Gallery.exe --comments-selfcheck=%TEMP%\comments-selfcheck.txt` (added by P11-24): the report ends with `n of n checks passed`
and the exit code is 0.

- Windows build:
- Gallery build kind (Debug / Release / Native AOT):
- Display scale:
- Date and tester:

## P11-12 — The zone-widget layer

Each thread's box sits over its gap, above the text and below the find widget, the hover and the scroll bars. Use the zoom at 100 % unless a
check says otherwise. The text-area origin is the zone-widget canvas origin (both fill the same template cell with no margin): check 1 verifies it.

| # | Check | Result |
|---|---|---|
| 1 | The box of line 8 has its top edge exactly at the bottom of line 8, and line 9 starts right below the box (screenshot ruler) | pending |
| 2 | Scroll by wheel and by dragging the scroll bar for 10 s: in every screenshot taken during the scroll (Win+Shift+S) the box is never more than 1 px away from its line | pending |
| 3 | Scroll a box above the top edge: it is cut at the editor's top edge, nothing is drawn above the editor; the same at the bottom edge and above the horizontal scroll bar | pending |
| 4 | `Ctrl+F` and scroll a box under the find widget: the find widget stays on top; a hover popup and the completion list are drawn above a box | pending |
| 5 | Insert a line above line 8: the three boxes move down one line | pending |
| 6 | Resize the window and zoom (`Ctrl` + wheel): each box keeps `ContentLeft + 4` and the content width; it never covers the line numbers, the minimap or the vertical scroll bar | pending |
| 7 | Word wrap on, the window narrow so line 24 wraps, **Add comment at caret** on line 24: the box sits under the **last** wrapped row | pending |
| 8 | A click and a drag on a box's empty area do not move the caret or select text; the wheel over a box scrolls the editor | pending |
| 9 | **Add 20 threads** (twice, on a longer file such as "Large C# (20 000 lines)" plus the button several times): scrolling stays smooth; `lastArrange` stays under 1 ms (write the value) | pending |
| 10 | **Add comment at caret** on line 14 (inside the `for` loop), then collapse the loop's fold (line 12): the box disappears (`visible` drops); expand: it returns | pending |

## P11-13 — The zone height follows the content

| # | Check | Result |
|---|---|---|
| 1 | **Reply** on line 8, type a comment, **Comment**: the box and its gap grow together; the line below touches the box's bottom edge (0 px gap or overlap) | pending |
| 2 | After check 1, `heightPasses` went up by 1 or 2 and `heightLimitHits` is 0 | pending |
| 3 | Narrow the window so the comments wrap on more rows: the gap follows within one frame after the resize stops | pending |
| 4 | **Resolve first thread**, then click its header to expand and collapse it several times: no flicker, `heightLimitHits` stays 0 | pending |
| 5 | **Add 20 threads**, resize the window: no freeze longer than a visible hitch, `heightLimitHits` = 0 | pending |
| 6 | Zoom in and out: the comment text follows the editor's font size and the gaps follow the boxes | pending |

## P11-14 — Focus and keys

**Add comment at caret** on line 3: a composer opens and its text box has the focus.

| # | Check | Result |
|---|---|---|
| 1 | Type `abc` and `Backspace`: the box shows `ab`; the document is unchanged (after leaving the box, `Ctrl+Z` in the document does nothing) and its caret does not blink | pending |
| 2 | `Tab` goes through the composer's controls in visual order and then leaves the editor; `Shift+Tab` goes back; the document gets no tab | pending |
| 3 | `Enter` adds a line in the box; `Ctrl+Enter` posts (one `CommentSubmitted` in the log) | pending |
| 4 | `Ctrl+A`, `Ctrl+Z`, `Ctrl+C` / `Ctrl+V`, `Ctrl+F`, `F12`, `Ctrl+D` in the box act on the box or do nothing; **no** editor command runs (no find widget, no selection change in the document) | pending |
| 5 | `Escape` with an empty box: the composer closes (the new thread is removed), the focus is in the document, the caret on the thread's line blinks. `Escape` with text: nothing happens | pending |
| 6 | Click in the document while the box has text, then click back in the box: the text is still there | pending |
| 7 | A thread with no comment, focus in its box, click **Cancel**: the thread is removed while its button has the focus, the focus lands in the document and typing works at once. Also **Remove all threads** with a composer open (the click moves the focus to the button first) | pending |
| 8 | A Japanese or Chinese IME composes in the box, its candidate window is next to the box; `Win+.` inserts an emoji in the box | pending |
| 9 | Narrator follows the focus into the box and back | pending |

## P11-15 — The shared Markdown element builder

Take screenshots of these popups with a build from before P11-15 and compare. Nothing may look different.

| # | Check | Result |
|---|---|---|
| 1 | The hover of a marker ("Problems" sample, **Add sample problems**): message, dimmed source / code, separators unchanged | pending |
| 2 | A Markdown hover with code, a list, a heading, a link and bold text (**Demo providers: on** or a language server): same fonts, margins, bullet glyph, heading size and link color | pending |
| 3 | The completion documentation looks the same | pending |

## P11-16 — Header, comments and the resolved state

| # | Check | Result |
|---|---|---|
| 1 | Text and layout look like a review: bold title, initials in colored circles, author in semi-bold, dimmed relative time, a 1 px line between comments, corner radius 6 | pending |
| 2 | Line 8: bold, inline code and the list are drawn; post `~~struck~~ <u>under</u> > quote` style text through **Reply** and check strike, underline and the quote bar | pending |
| 3 | Line 24 (resolved) is one line: `▸`, title, `Resolved` badge, `This line is too long… · 2 comments`; a click expands it (`▾`, the comments), a click collapses it | pending |
| 4 | Light, dark and high-contrast themes (Theme box, Windows high contrast) recolor the widget | pending |
| 5 | A comment posted from the composer appears at once, the widget grows and the text below moves | pending |

## P11-17 — The composer

| # | Check | Result |
|---|---|---|
| 1 | **Reply** under line 8's comments opens the composer and the caret is in the box | pending |
| 2 | The placeholder `Leave a comment` shows while the box is empty | pending |
| 3 | **Comment** is disabled for an empty or blank box and enabled after one letter | pending |
| 4 | Type `**bold** and [a link](http://x)`, **Preview**: bold text and a link; the height does not jump; back: the caret is where it was | pending |
| 5 | **Comment** and `Ctrl+Enter` each raise exactly one `CommentSubmitted`; the composer then closes and the box is empty | pending |
| 6 | **Cancel** on a new thread removes it and the focus is in the document on its line; on a thread with comments it only closes the composer | pending |
| 7 | Type 30 lines: the box stops growing at 280 px and scrolls; the gap follows (no overlap with the next line) | pending |
| 8 | The `Escape` rules of P11-14 hold | pending |

## P11-18 — The Markdown toolbar

Open a composer and type the "Start" text. `«…»` marks a selected range and `‸` the caret; `⏎` is a line break. Expected texts come from the
unit tests `MarkdownToolbarInlineTests` and `MarkdownToolbarBlockTests`.

| # | Button | Start | Expected | Result |
|---|---|---|---|---|
| 1 | `H` Heading | «a b» | «### a b» | pending |
| 2 | `H` Heading | a‸b | ### a‸b (the caret keeps its place in the line) | pending |
| 3 | `B` Bold | x «a» y | x «\*\*a\*\*» y | pending |
| 4 | `B` Bold | x‸y | x\*\*‸\*\*y | pending |
| 5 | `I` Italic | «a» | «\_a\_» | pending |
| 6 | `I` Italic | x‸y | x\_‸\_y | pending |
| 7 | `U` Underline | «a» | «`<u>a</u>`» | pending |
| 8 | `U` Underline | x‸y | x`<u>`‸`</u>`y | pending |
| 9 | `S` Strikethrough | «a» | «\~\~a\~\~» | pending |
| 10 | `S` Strikethrough | x‸y | x\~\~‸\~\~y | pending |
| 11 | `<>` Inline code | «a» | «\`a\`» | pending |
| 12 | `<>` Inline code | x‸y | x\`‸\`y | pending |
| 13 | `{ }` Code block | «a⏎b» | \`\`\`⏎«a⏎b»⏎\`\`\` | pending |
| 14 | `{ }` Code block | empty box | \`\`\`⏎‸⏎\`\`\` | pending |
| 15 | `Link` | «abc» | \[abc\](«url») (`url` selected) | pending |
| 16 | `Link` | a‸b | a\[‸\](url)b | pending |
| 17 | `•` Bulleted list | «a⏎⏎b» | «- a⏎⏎- b» | pending |
| 18 | `•` Bulleted list | x⏎a‸b | x⏎- a‸b | pending |
| 19 | `1.` Numbered list | «a⏎⏎b» | «1. a⏎⏎2. b» | pending |
| 20 | `1.` Numbered list | x⏎a‸b | x⏎1. a‸b | pending |
| 21 | `"` Quote | «a⏎b» | «> a⏎> b» | pending |
| 22 | `"` Quote | x⏎a‸b | x⏎> a‸b | pending |
| 23 | `±` Suggest a change, composer of the line-10 thread (**Reply**) | empty box | \`\`\`suggestion⏎«int max = 10;»⏎\`\`\` (the line selected) | pending |
| 24 | `±` Suggest a change | ab‸cd | ab⏎\`\`\`suggestion⏎«the line»⏎\`\`\`⏎cd (the block starts on a new line) | pending |

| # | Check | Result |
|---|---|---|
| 25 | `Ctrl+B`, `Ctrl+I`, `Ctrl+K` give the same results as **B**, **I**, **Link** | pending |
| 26 | `Tab` enters the toolbar once and the next `Tab` leaves it; `Left` / `Right` (wrapping), `Home` / `End` move between buttons; after moving to `Link`, `Shift+Tab` back into the toolbar lands on `Link` | pending |
| 27 | The toolbar is disabled while **Preview** is on | pending |
| 28 | Narrator reads each button's name; the tooltip shows it | pending |

## P11-19 — The suggestion block

| # | Check | Result |
|---|---|---|
| 1 | Line 10's thread shows a "Suggested change" block: a red `-` row `int max = 10;` and a green `+` row `const int Max = 10;` | pending |
| 2 | **Apply suggestion** replaces the line (the Gallery resolves the thread: expand it); the block now reads "No changes" without a button; `Ctrl+Z` in the editor restores the line and the block shows the diff again | pending |
| 3 | Post (**Reply**) a comment with an empty suggestion block (`±` then delete the line text): it shows the removed row only; Apply deletes the line | pending |
| 4 | Post a two-line suggestion: Apply adds a line | pending |
| 5 | Edit line 10 by hand: the blocks redraw against the new text while typing | pending |
| 6 | **Read-only** on: the Apply buttons are disabled (their tooltip says the editor is read-only when WinUI shows tooltips of disabled buttons); off: enabled | pending |
| 7 | Preview of a draft with a suggestion block: drawn without a button | pending |
| 8 | High contrast: the `-` / `+` markers are readable | pending |
| 9 | A suggestion with a very long line scrolls horizontally inside the block, it does not wrap | pending |

## P11-20 — Accessibility

| # | Check | Result |
|---|---|---|
| 1 | Insights: each widget is a group `Comment thread on line N` (AutomationId `LoomEditor.CommentThread`) under the editor; each comment a group `author, time` with the body as full description; no unnamed focusable element | pending |
| 2 | Narrator reads the thread name when the focus enters, each toolbar button by name, the text box as "Comment text, edit" | pending |
| 3 | The Preview toggle reads its state | pending |
| 4 | **Apply suggestion** reads the diff description ("Removed: … Added: …") | pending |
| 5 | Spoken once each: thread added (Add comment at caret), comment posted, suggestion applied, comment discarded (Cancel with text) | pending |
| 6 | High contrast (Aquatic, Desert): every control readable, focus rectangles visible, the diff understandable from the markers | pending |
| 7 | `Tab` order = visual order | pending |
| 8 | The editor's UIA text pattern is unchanged: the Gallery's **UIA self-check** button still passes with threads present | pending |

## P11-21 — The gutter "+" button

| # | Check | Result |
|---|---|---|
| 1 | Switch off: nothing over the line numbers. On: hovering the line numbers shows **+** on the hovered line only | pending |
| 2 | Moving down the gutter moves the button line by line without flicker | pending |
| 3 | Wheel scrolling with the pointer still in the gutter: the button follows the line under the pointer | pending |
| 4 | Clicking **+** opens a composer on that line (log: `AddCommentRequested`); the caret and selection did not move | pending |
| 5 | Lines 8, 10 and 24 (they have threads) show no button | pending |
| 6 | Word wrap on: on a wrapped line the button sits at the line's first row, whichever row is hovered | pending |
| 7 | Leaving the gutter to the text hides the button; moving from the gutter onto the button keeps it | pending |
| 8 | **Add comment at caret** and `Ctrl+Alt+M` open a composer on the caret line | pending |

## P11-22 — Comment threads in the diff editor

Diff editor window, **Comment sample**: the original side gets the three sample threads, the modified side a thread on its line 10.

| # | Check | Result |
|---|---|---|
| 1 | A thread on an original line: the modified side shows blank space of the same height after the matching line; the first line below the box is at the same y on both sides (±1 px) | pending |
| 2 | The same from the modified side (**Add on modified**) | pending |
| 3 | Scroll: both sides stay level | pending |
| 4 | Type ten lines in a composer on each side: the sides stay level after each line | pending |
| 5 | Inline view: the modified thread stays, the original threads are not shown; side-by-side again: they are back, a draft too | pending |
| 6 | Hide unchanged regions on: caret on a line in a region (Show all, place the caret, Collapse all), **Add on …**: the region expands and the box opens | pending |
| 7 | Apply works on the modified side; on the original side (Original editable off) the button is disabled with the read-only tooltip | pending |
| 8 | The log shows each event once with the right side (`CommentSubmitted (Modified, …)`, `AddCommentRequested (Original, line N)` with the gutter box on) | pending |

## End-to-end A — Review flow (editor)

| # | Step | Result |
|---|---|---|
| 1 | **Gutter "+" button** on, click **+** on line 12: a composer opens with the focus | pending |
| 2 | Type `Please`, select it, **B**; then `±` (suggest a change) and edit the suggested line | pending |
| 3 | **Preview** shows bold text and the suggested change without a button; back to the box | pending |
| 4 | **Comment**: the comment appears with the suggestion block and an **Apply suggestion** button | pending |
| 5 | **Apply suggestion**: the line changes, the thread is resolved by the Gallery; `Ctrl+Z` undoes the edit in one step | pending |

## End-to-end B — Review flow (diff)

| # | Step | Result |
|---|---|---|
| 1 | **Comment sample**, side by side: post a reply on an original thread and a new thread on the modified side; both log with their side | pending |
| 2 | Apply a suggestion on the modified side: the modified text changes, the sides stay level, the diff updates | pending |
| 3 | Inline view, then side by side: threads, drafts and alignment come back | pending |

## C — Robustness

| # | Check | Result |
|---|---|---|
| 1 | **Add 20 threads** several times (≈100 threads on a long file): scrolling stays usable | pending |
| 2 | Type 200 lines into a composer: no freeze; the box scrolls past 280 px | pending |
| 3 | Resize, zoom, word wrap on, dark / light / high contrast with threads open: no overlap, no crash | pending |
| 4 | Delete lines above threads, and delete a commented line: threads move up / take the next line | pending |
| 5 | Replace the whole text (pick another sample, then **Comment threads** again): no leftover box, no crash | pending |
| 6 | **Read-only** on: posting still works, Apply is disabled | pending |
| 7 | Two rapid **Add comment at caret** clicks: two zones on the line, no crash | pending |

## D — Keyboard only

| # | Check | Result |
|---|---|---|
| 1 | `Ctrl+Alt+M` in the document opens a composer on the caret line | pending |
| 2 | `Tab` / `Shift+Tab` through the widget: toolbar (one stop), text box, Preview, Cancel, Comment | pending |
| 3 | `Escape` rules (empty: closes and returns; text: nothing); `Ctrl+Enter` posts | pending |

## E — Screen reader

| # | Check | Result |
|---|---|---|
| 1 | With Narrator, repeat end-to-end A without looking: every step can be done and is announced | pending |

## Findings

| # | Section / check | What happened | Card or fix |
|---|---|---|---|
