# Phase 11 — manual test list

What unit tests cannot check: the comment thread widgets under a real UI.
Fill the **Result** column (`pass` / `fail` + a note, or `pending`) and the **Findings** table at the end.

**Machine:** Windows 11, a mouse with a wheel and (optional) a touchpad. About 15 minutes for the sections written so far.
Pick the sample "Comment threads (P11-12)" in the Gallery "Features" picker; its buttons are in the **Comment threads (P11-12)** Expander, whose
status line shows `zones {n} visible {v} lastArrange {ms} ms` (refreshed every 500 ms).

- Windows build:
- Gallery build kind (Debug / Release / Native AOT):
- Display scale:
- Date and tester:

## P11-12 — The zone-widget layer

A placeholder box (title and comments as plain text) covers each thread's 120 px gap. Use the Gallery's zoom at 100 % unless a check says otherwise.
The text-area origin is the origin of the zone-widget canvas (both fill the same template cell with no margin): check 1 verifies it.

| # | Check | Result |
|---|---|---|
| 1 | **Add 3 threads**: the box of line 5 has its top edge exactly at the bottom of line 5, and line 6 starts 120 px lower (screenshot ruler or line numbers) | pending |
| 2 | Scroll by wheel and by dragging the scroll bar for 10 s: in every screenshot taken during the scroll (Win+Shift+S) the box is never more than 1 px away from line 5 | pending |
| 3 | Scroll line 5 above the top edge: the box is cut at the editor's top edge, nothing is drawn above the editor; the same at the bottom edge and above the horizontal scroll bar | pending |
| 4 | `Ctrl+F` and scroll a box under the find widget: the find widget stays on top; a hover popup and the completion list are drawn above a box | pending |
| 5 | Insert a line above line 5: the three boxes move down one line | pending |
| 6 | Resize the window and zoom (`Ctrl` + wheel): the box keeps `ContentLeft + 4` and the content width; it never covers the line numbers, the minimap or the vertical scroll bar | pending |
| 7 | Word wrap on with a long line 5: the box sits under the **last** wrapped row of line 5 | pending |
| 8 | A click and a drag on a box do not move the caret or select text; the wheel over a box scrolls the editor | pending |
| 9 | **Add 200 threads**: scrolling stays smooth; `lastArrange` stays under 1 ms (write the measured value here) | pending |
| 10 | Collapse a fold that contains a thread's line: its box disappears (`visible` drops); expand: it returns | pending |

## P11-13 — The zone height follows the widget's content

Start from **Add 3 threads**. The diagnostics line now also shows `heightPasses {n} heightLimitHits {m}`. A new thread's gap shrinks from 120 px to its title row.

| # | Check | Result |
|---|---|---|
| 1 | **Add a comment to the first thread**: the box and its gap grow together; the line below the box touches its bottom edge (no gap, no overlap: 0 px in a screenshot) | pending |
| 2 | After check 1, `heightPasses` increased by 1 or 2 and `heightLimitHits` = 0 | pending |
| 3 | Narrow the window so the comment wraps on more rows: the gap follows within one frame after the resize stops | pending |
| 4 | **Toggle a long title** repeatedly (narrow window so the title wraps): no flicker, `heightLimitHits` stays 0 | pending |
| 5 | **Add 200 threads**, resize the window: no freeze longer than a visible hitch, `heightLimitHits` = 0 | pending |
| 6 | Zoom in and out (`Ctrl` + wheel, A− / A+): the heights follow the font size. The placeholder text uses the XAML default font, so until P11-16 only the text lines move; write what you see | pending |

## P11-14 — Focus and keys between the editor and the widget's text box

Click **Start composing on line 5**: a thread opens on line 5 with a temporary text box and **Cancel** / **Comment** buttons (P11-17 replaces them),
and the box has the focus. The status line counts `CommentSubmitted` events (the Gallery adds each submitted comment to its thread).

| # | Check | Result |
|---|---|---|
| 1 | Type `abc` and `Backspace` in the box: the box shows `ab`; the document is unchanged (after leaving the box, `Ctrl+Z` does nothing) and its caret does not blink | pending |
| 2 | `Tab` goes box → Cancel → Comment → the next control after the editor; `Shift+Tab` goes back; the document gets no tab | pending |
| 3 | `Enter` adds a line in the box; `Ctrl+Enter` submits (`CommentSubmitted` goes up by exactly 1) | pending |
| 4 | `Ctrl+A`, `Ctrl+Z`, `Ctrl+C` / `Ctrl+V`, `Ctrl+F`, `F12`, `Ctrl+D` in the box act on the box or do nothing; **no** editor command runs (no find widget, no selection change in the document) | pending |
| 5 | `Escape` with an empty box: the composer closes, the focus is in the document, the caret on line 5 blinks. `Escape` with text: nothing happens | pending |
| 6 | Click in the document while the box has text, then click back in the box: the text is still there | pending |
| 7 | **Remove all** while the box has the focus: the focus lands in the document (not on the window); typing works at once. Clicking the button moves the focus to it first, so also check the same path from inside the widget: on a thread with no comment, click **Cancel** (the thread is removed while its button has the focus) | pending |
| 8 | A Japanese or Chinese IME composes in the box, its candidate window is next to the box; `Win+.` inserts an emoji in the box | pending |
| 9 | Narrator follows the focus into the box and back | pending |

## P11-15 — The shared Markdown element builder

The hover's Markdown now comes from `MarkdownElementBuilder`; nothing should look different. **Before** checking out this card's commit, take screenshots
of the popups below with the previous build and keep them out of the repository; then compare with this build.

| # | Check | Result |
|---|---|---|
| 1 | The hover of a marker ("Problems" sample, **Add sample problems**): message, dimmed source / code, separators look the same as before | pending |
| 2 | A Markdown hover with code, a list, a heading, a link and bold text (Gallery **Demo providers: on** or a language server): same fonts, margins, bullet glyph, heading size and link color as before | pending |
| 3 | The completion documentation (details pane of the suggest widget) looks the same as before | pending |

## P11-16 — The widget (1/4): header, comments and the resolved state

Load the "Comment threads (P11-12)" sample and click **Add review threads (lines 3, 12, 25, 33)**: line 3 has one comment, line 12 three comments
with Markdown (bold, italic, strike, underline, inline code, a link, a list, a quote, a fenced block; the second comment is edited), line 25 is a
resolved thread, line 33 a comment with a very long line.

| # | Check | Result |
|---|---|---|
| 1 | Text and layout look like a review comment: bold title, avatar circles with initials, author in semi-bold, dimmed relative time (`(edited)` on the edited comment), a 1 px line between comments, corner radius 6 | pending |
| 2 | Strike and underline are drawn (line 12); the link is colored and underlined; the quote has a bar on its left and dimmed text | pending |
| 3 | The resolved thread (line 25) is one line: `▸`, title, `Resolved` badge, `Typo in the name… · 2 comments`; a click expands it (`▾`, the comments), a click collapses it | pending |
| 4 | Light, dark and high-contrast themes (Gallery theme switch) recolor the widget: background, border, text, dimmed text, link | pending |
| 5 | **Add a comment to the first thread**: the widget grows and the text below moves; the long line of line 33 wraps inside the box | pending |

## P11-17 — The widget (2/4): the composer

Use **Add review threads** and **Start composing on line 5**. The status line counts `CommentSubmitted` (the Gallery adds each submitted comment).

| # | Check | Result |
|---|---|---|
| 1 | **Reply** under the comments of line 12 opens the composer and the caret is in the box | pending |
| 2 | The placeholder `Leave a comment` shows while the box is empty | pending |
| 3 | **Comment** is disabled for an empty or blank box and enabled after one letter | pending |
| 4 | Type `**bold** and [a link](http://x)`, toggle **Preview**: bold text and a link; the height does not jump; toggle back: the caret is where it was | pending |
| 5 | **Comment** and `Ctrl+Enter` each raise exactly one `CommentSubmitted`; after the Gallery adds the comment the composer closes and the box is empty | pending |
| 6 | **Cancel** on a new thread (line 5) removes it and the focus is in the document on line 5; on a thread with comments it only closes the composer | pending |
| 7 | Type 30 lines: the box stops growing at 280 px and scrolls; the gap under the line follows (no overlap with the next line) | pending |
| 8 | The `Escape` rules of P11-14 still hold (empty box: closes and the focus returns; with text: nothing) | pending |

## P11-18 — The widget (3/4): the Markdown toolbar

Open a composer (**Start composing on line 5**) and type the "Start" text. `«…»` marks a selected range and `‸` the caret; `⏎` is a line break.
Expected texts come from the P11-03 / P11-04 unit tests (`MarkdownToolbarInlineTests`, `MarkdownToolbarBlockTests`).

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
| 23 | `±` Suggest a change, on a thread whose line is `int max = 10;` | empty box | \`\`\`suggestion⏎«int max = 10;»⏎\`\`\` (the line selected) | pending |
| 24 | `±` Suggest a change | ab‸cd | ab⏎\`\`\`suggestion⏎«the line»⏎\`\`\`⏎cd (the block starts on a new line) | pending |

| # | Check | Result |
|---|---|---|
| 25 | `Ctrl+B`, `Ctrl+I`, `Ctrl+K` give the same results as **B**, **I**, **Link** | pending |
| 26 | `Tab` from the document side enters the toolbar once and the next `Tab` leaves it for the text box; `Left` / `Right` (wrapping), `Home` / `End` move between buttons; after moving to `Link`, `Shift+Tab` back into the toolbar lands on `Link` | pending |
| 27 | The toolbar is disabled while **Preview** is on | pending |
| 28 | With Narrator, each button reads its name (`Heading`, `Bold`, …) and the tooltip shows it | pending |

## P11-19 — The widget (4/4): the suggestion block

On a line of the sample, type `int max = 10;` (replace its text), keep the caret there and click **Add suggestions on the caret line**: a thread with
three comments — `Use a constant:` + a `suggestion` block `const int Max = 10;`; an empty suggestion; a two-line suggestion.

| # | Check | Result |
|---|---|---|
| 1 | The first comment shows a "Suggested change" block with one red `-` row `int max = 10;` and one green `+` row `const int Max = 10;` | pending |
| 2 | **Apply suggestion** replaces the line; `Ctrl+Z` in the editor restores it; after the apply (before the undo) the block reads "No changes" with no button | pending |
| 3 | The empty suggestion shows the removed row only; Apply deletes the line | pending |
| 4 | The two-line suggestion adds a line (`// The largest count.` above `const int Max = 10;`) | pending |
| 5 | Edit the commented line by hand: the blocks redraw against the new text while typing | pending |
| 6 | Gallery **Read-only** on: the Apply buttons are disabled (their tooltip says the editor is read-only, when WinUI shows tooltips of disabled buttons); off: enabled again | pending |
| 7 | Start composing, type a `suggestion` block, toggle **Preview**: the block is drawn without a button | pending |
| 8 | High contrast: the `-` / `+` markers are readable (the soft red / green backgrounds come from the fallback colors) | pending |
| 9 | A very long suggested line scrolls horizontally inside the block, it does not wrap | pending |

## P11-20 — Accessibility of the comment widget

Narrator and Accessibility Insights for Windows (also NVDA if installed). Use **Add review threads**, **Add suggestions on the caret line** and
**Start composing on line 5**.

| # | Check | Result |
|---|---|---|
| 1 | Insights shows each widget under the editor as a group `Comment thread on line N` (AutomationId `LoomEditor.CommentThread`), each comment as a group `author, time` with the body in its full description, and no unnamed focusable element | pending |
| 2 | Narrator reads `Comment thread on line 13` (or the thread's line) when the focus enters; each toolbar button by name; the text box as "Comment text, edit" | pending |
| 3 | The Preview toggle reads its state (on / off) | pending |
| 4 | **Apply suggestion** reads the diff description (e.g. "Removed: int max = 10; Added: const int Max = 10;") | pending |
| 5 | Announcements, each spoken once: thread added (Start composing), comment posted, suggestion applied, comment discarded (Cancel with text) | pending |
| 6 | High contrast (Aquatic and Desert): every control readable, focus rectangles visible, the suggestion diff understandable from the `-` / `+` markers | pending |
| 7 | `Tab` order = visual order (header button of a resolved thread, Reply, toolbar, text box, Preview, Cancel, Comment, Apply) | pending |
| 8 | The editor's UIA text pattern is unchanged: the Gallery's **UIA self-check** button (phase 7) still passes with comment threads present | pending |

## Findings

| # | Section / check | What happened | Card or fix |
|---|---|---|---|
