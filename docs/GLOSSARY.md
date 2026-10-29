# Glossary

Words you will meet in the code, explained simply. Monaco uses the same words, so you can search its source too.

**Alignment (diff)** — A pair of line ranges, one in the original text and one in the modified text, that must start and end level in a side by side diff, with the height each side needs (`LineRangeAlignment`). The shorter side gets a *view zone* to make up the difference.

**Alternative version id** — A number on a `TextModel` that goes back to an earlier value when undo returns the text
to an earlier state, unlike the *version id*, which only grows. Used to know whether a document is "dirty".

**Anchor** — In a selection, the place where the user *started* selecting. Called `SelectionStart` in `Selection`.
The other end is the *caret* (`Position`).

**bigNumbersDelta** — A number the viewport data subtracts from the scroll position, so that the vertical offsets of the
visible lines stay small. In a 100 MB document `scrollTop` is in the millions of DIPs, and large numbers lose
precision when they become `float`; the renderer works with `scrollTop - bigNumbersDelta` instead.

**Caret** — The blinking text cursor. In code, `Selection.Position`.

**Column** — Position inside a line, **starting at 1**. Column 1 is *before* the first character; a line of
length *n* has columns 1 to *n + 1*. Columns count UTF-16 code units (like `string.Length`), so an emoji may take 2.

**Column (box) selection** — A rectangular selection made of one selection per line, built with Alt+Shift+mouse or
Shift+Alt+arrows. `ColumnSelection` computes it, using *visible columns*.

**Command** — An action identified by a string id, e.g. `editor.action.selectAll`. Commands are what keyboard
shortcuts, menus and code run. See `CommandRegistry`.

**Composition (IME)** — The text an IME shows while the user is still choosing the final characters (underlined). It is
replaced by the final text when the composition ends. See *IME* and *edit context*.

**Content width / scroll width** — The width of the scrollable content: the widest line seen so far plus some extra
room, and the width of the scrollbar. The *scroll width* is the larger of it and the viewport width. They set the
range of the horizontal scrollbar (`ViewLayout.GetContentWidth`, `GetScrollWidth`).

**Contribution / Feature** — An optional piece of editor behavior (Find, Folding…). Monaco calls them
*editor contributions*; we call them *features* (`IEditorFeature`), one folder each.

**Cursor** — In Monaco, a cursor is a caret plus its selection. With multi-cursor editing there are several.

**Decoration** — Styling attached to a range of text: a squiggly underline, a highlight, a breakpoint dot.
Decorations move with the text when it is edited.

**DIP** — Device-independent pixel: 1/96 inch. WinUI and Win2D use DIPs, so sizes are the same on every screen.

**DirectWrite / Direct2D** — Windows' text and 2D graphics engines. We use them through **Win2D**.

**Edit command (`IEditCommand`)** — An object that, given a model, says which edits to make and how to compute the
resulting selection (`ShiftCommand`, `ReplaceCommand`…). The cursor collects the commands of all cursors and runs them as one edit.

**Edit context** — The small copy of the text (the lines of the primary selection) and selection that the editor gives to
the IME and other Windows text services, and which they update (`EditContextModel`, Windows `CoreTextEditContext`).

**Edit operation identifier** — A label (`identifier`) attached to an edit so that the cursor can find out which range
that edit produced when the model reports its result.

**EOL** — End of line: `\n` (LF) or `\r\n` (CRLF). A text model has one preferred EOL.

**Grapheme cluster** — What a user sees as one character: a letter with accents, an emoji with a skin tone, a family emoji…
It can be several UTF-16 units; the cursor moves over a whole cluster (`GraphemeIterator`).

**Gutter** — The margin on the left of the text: line numbers, folding arrows, breakpoints.

**IME** — Input Method Editor: how people type Chinese, Japanese, Korean… (a candidate window turns key strokes
into characters). Needs special support from the editor (phase 3).

**Input mode (overtype)** — The way typing works: *insert* (the default) pushes the text after the caret to the right, *overtype* replaces the character under the caret. The `Insert` key toggles it; `EditorContext.InputMode` holds it, one per editor, and `OvertypeCursorStyle` draws the caret.

**Interval tree** — A tree that quickly finds all ranges overlapping a given range. Used to store decorations.

**Keybinding / `when` clause** — A key combination bound to a command id. A `when` clause says in which state the
binding is active (for example "the editor has a selection"); here it is a C# predicate on a `KeybindingContext`.

**Layout info** — The sizes of the parts of the editor, in DIPs: line numbers, glyph margin, folding strip, text area,
scrollbars (`EditorLayoutInfo`, computed from the editor size and the font). `ContentLeft` is where the text starts.

**Leftover visible columns** — When the cursor moves up or down through shorter lines, it remembers the column it
started from (the "leftover") to return to it on a longer line.

**Ligature** — Several characters drawn as one glyph by a font (`=>` as an arrow in Cascadia Code). The editor turns them on with `FontLigatures` or `FontFeatureSettings`; the caret still crosses one character at a time, sharing the glyph width equally.

**Line layout cache** — A least-recently-used cache of laid-out lines (`LineLayoutCache`), keyed by the text of the
line. A line that did not change keeps its layout while the user scrolls or edits other lines. It is cleared when the
font or the DPI changes.

**Line number** — Starts at **1**, like in every editor UI.

**Model / TextModel** — The document: its text, end-of-line style, options, undo history. No pixels, no UI.

**Native AOT** — Ahead-of-time compilation of .NET code to a native executable: fast start-up, no JIT, but **no
run-time reflection or code generation**. That is why we use source generators.

**Overview ruler** — The narrow strip next to the scrollbar that shows colored marks for the whole document (find matches, markers; in the diff editor the changed lines) and, in the diff editor, a slider for the visible part. `OverviewZoneManager` turns line ranges into pixel bands; `OverviewRulerPainter` draws them.

**Oniguruma** — The regular-expression library used by TextMate grammars (C, BSD-2 license).

**Piece tree** — How Monaco stores text efficiently. The original file and everything typed are kept in
append-only *buffers*; the document is a list of *pieces* ("characters 10 to 50 of buffer 0") kept in a balanced
binary tree (a red-black tree) so that finding a line or inserting text is fast even in a 100 MB file.

**Pixel ratio** — The number of physical pixels in one DIP (1 at 100 % scale, 1.5 at 150 %). Fonts are measured with it,
and the caret width is rounded to whole physical pixels.

**Placeholder** — A *tab stop* of a snippet with a default text (`${1:i}`). It is selected when reached, so typing replaces the default; the same number elsewhere is a mirror and follows. In code, `Placeholder`.

**Position** — A (line number, column) pair, both starting at 1.

**PRI** — Package Resource Index: the file where Windows App SDK stores an app's resources, including compiled XAML.

**Primary cursor / secondary cursor** — With several cursors, the primary one is the one the user created last; the
view reveals it and the status bar shows it. The others are secondary.

**Range** — A start position and an end position, start ≤ end. *Empty* if both are equal.

**Raw change event** — The line-level description of a change (`ModelRawContentChangedEvent`): lines changed, inserted
or deleted, or "everything flushed". The view uses it to update only the lines that changed. The *content change
event* describes the same edit as text ranges.

**Scope** (TextMate) — A dotted name describing what a token is, e.g. `string.quoted.double.cs`. Themes pick
colors by scope.

**Scroll beyond last line** — The option that lets the user scroll until the last line is at the top of the viewport,
leaving empty space below it (`ViewLayoutOptions.ScrollBeyondLastLine`).

**Selection** — A range plus a direction (where the anchor and the caret are).

**Snapshot** — A frozen view of the text of a model, read in chunks (`TextModel.CreateSnapshot`), safe to read while
the model keeps changing. Used for saving files.

**Snippet** — A text template with *tab stops*, *placeholders* and variables (`for (${1:i} = 0; …$0`), in the TextMate syntax. Inserted at every cursor by `InsertSnippet`; see [how-to/use-snippets.md](how-to/use-snippets.md).

**Source generator** — Code that runs at compile time and writes more C# code. Replaces reflection.

**Stickiness** — What a tracked range does when text is inserted exactly at one of its ends: grow to include it or stay
outside (`TrackedRangeStickiness`).

**Tab stop** — A place of a *snippet* that `Tab` jumps to, in numeric order; `$0` is the last one, where the caret ends. Tracked by a model decoration, so it moves with the text.

**Token** — A piece of a line with one meaning and one color (a keyword, a string, a comment…).

**Tokenization** — Cutting lines into tokens using a TextMate grammar. Runs in the background.

**Tracked range** — A range the model keeps up to date through edits (stored in the interval tree). The cursor uses
them to follow the selections during a command.

**Typography feature** — An OpenType switch of a font, named by a four letter tag (`liga`, `calt`, `ss01`…) and a value, set with `FontFeatureSettings` in the CSS syntax `'ss01' 1, "calt" 0`. Applied through DirectWrite typography.

**Unchanged region** — A block of lines that did not change in a diff, longer than the context kept around the changes. The diff editor can collapse it behind a bar "N hidden lines" (`UnchangedRegion`, `HideUnchangedRegions`).

**Undo stack element** — One undo step: all the edits (and EOL change) that one undo reverts together.

**Undo stop** — A boundary between two undo steps. Typing keeps adding to the current step until an undo stop is
pushed (for example when the cursor moves or a command ends).

**View line** — A line *as displayed*. With word wrap, one model line may become several view lines; with folding,
several model lines may be hidden. Model line numbers ≠ view line numbers.

**Viewport** — The part of the document currently visible on screen.

**View zone** — A gap between two lines that a widget owns (Monaco: `IViewZone`; here `ViewZone` over the *whitespaces* of the layout): the hatched filler that aligns the two sides of a diff, the deleted lines of the inline diff, the "N hidden lines" bar. Its *kind* tells the renderer what to paint.

**Virtualization** — Only creating/drawing what is visible in the viewport, so huge files stay fast.

**Visible column** — A column counted as the user sees it: a tab counts up to the next tab stop, a wide character
counts 2. Differs from the *column* (one per UTF-16 unit). `CursorColumns` converts between them.


**Whitespace (view zone gap)** — Vertical space inserted between two lines, used by widgets such as inline diffs or
peek views (`EditorWhitespace` in `LinesLayout`). It adds to the height of the document but is not a line.

**Win2D** — Microsoft's C#-friendly wrapper over Direct2D and DirectWrite. See ADR 0002.

**XBF** — Compiled (binary) XAML.
**Auto whitespace** — Whitespace the editor inserted by itself (for example the indentation of a new line). If the
user leaves the line without typing anything, the editor removes it (`trimAutoWhitespace`).
