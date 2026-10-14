# Glossary

Words you will meet in the code, explained simply. Monaco uses the same words, so you can search its source too.

**Alternative version id** — A number on a `TextModel` that goes back to an earlier value when undo returns the text
to an earlier state, unlike the *version id*, which only grows. Used to know whether a document is "dirty".

**Anchor** — In a selection, the place where the user *started* selecting. Called `SelectionStart` in `Selection`.
The other end is the *caret* (`Position`).

**Caret** — The blinking text cursor. In code, `Selection.Position`.

**Column** — Position inside a line, **starting at 1**. Column 1 is *before* the first character; a line of
length *n* has columns 1 to *n + 1*. Columns count UTF-16 code units (like `string.Length`), so an emoji may take 2.

**Command** — An action identified by a string id, e.g. `editor.action.selectAll`. Commands are what keyboard
shortcuts, menus and code run. See `CommandRegistry`.

**Contribution / Feature** — An optional piece of editor behavior (Find, Folding…). Monaco calls them
*editor contributions*; we call them *features* (`IEditorFeature`), one folder each.

**Cursor** — In Monaco, a cursor is a caret plus its selection. With multi-cursor editing there are several.

**Decoration** — Styling attached to a range of text: a squiggly underline, a highlight, a breakpoint dot.
Decorations move with the text when it is edited.

**DIP** — Device-independent pixel: 1/96 inch. WinUI and Win2D use DIPs, so sizes are the same on every screen.

**DirectWrite / Direct2D** — Windows' text and 2D graphics engines. We use them through **Win2D**.

**EOL** — End of line: `\n` (LF) or `\r\n` (CRLF). A text model has one preferred EOL.

**Gutter** — The margin on the left of the text: line numbers, folding arrows, breakpoints.

**IME** — Input Method Editor: how people type Chinese, Japanese, Korean… (a candidate window turns key strokes
into characters). Needs special support from the editor (phase 3).

**Interval tree** — A tree that quickly finds all ranges overlapping a given range. Used to store decorations.

**Line number** — Starts at **1**, like in every editor UI.

**Model / TextModel** — The document: its text, end-of-line style, options, undo history. No pixels, no UI.

**Native AOT** — Ahead-of-time compilation of .NET code to a native executable: fast start-up, no JIT, but **no
run-time reflection or code generation**. That is why we use source generators.

**Oniguruma** — The regular-expression library used by TextMate grammars (C, BSD-2 license).

**Piece tree** — How Monaco stores text efficiently. The original file and everything typed are kept in
append-only *buffers*; the document is a list of *pieces* ("characters 10 to 50 of buffer 0") kept in a balanced
binary tree (a red-black tree) so that finding a line or inserting text is fast even in a 100 MB file.

**Position** — A (line number, column) pair, both starting at 1.

**PRI** — Package Resource Index: the file where Windows App SDK stores an app's resources, including compiled XAML.

**Range** — A start position and an end position, start ≤ end. *Empty* if both are equal.

**Raw change event** — The line-level description of a change (`ModelRawContentChangedEvent`): lines changed, inserted
or deleted, or "everything flushed". The view uses it to update only the lines that changed. The *content change
event* describes the same edit as text ranges.

**Scope** (TextMate) — A dotted name describing what a token is, e.g. `string.quoted.double.cs`. Themes pick
colors by scope.

**Selection** — A range plus a direction (where the anchor and the caret are).

**Snapshot** — A frozen view of the text of a model, read in chunks (`TextModel.CreateSnapshot`), safe to read while
the model keeps changing. Used for saving files.

**Source generator** — Code that runs at compile time and writes more C# code. Replaces reflection.

**Token** — A piece of a line with one meaning and one color (a keyword, a string, a comment…).

**Tokenization** — Cutting lines into tokens using a TextMate grammar. Runs in the background.

**Undo stack element** — One undo step: all the edits (and EOL change) that one undo reverts together.

**View line** — A line *as displayed*. With word wrap, one model line may become several view lines; with folding,
several model lines may be hidden. Model line numbers ≠ view line numbers.

**Viewport** — The part of the document currently visible on screen.

**Virtualization** — Only creating/drawing what is visible in the viewport, so huge files stay fast.

**Win2D** — Microsoft's C#-friendly wrapper over Direct2D and DirectWrite. See ADR 0002.

**XBF** — Compiled (binary) XAML.
