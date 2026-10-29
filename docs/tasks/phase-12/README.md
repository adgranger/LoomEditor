# Phase 12 — read this before any card

Phase 12 lets an inline comment thread cover **a range of lines** instead of one line, as a GitHub pull request review does ("Comment on lines +12 to +15")
([phase-12-comment-ranges.md](../../roadmap/phase-12-comment-ranges.md)). It extends phase 11: read [phase-11/README.md](../phase-11/README.md) and
[ADR 0014](../../adr/0014-inline-comments.md) first. **Every convention of phase-11/README.md (sections "This phase is not a port", 1 and 2) applies unchanged**;
this page only adds what is new. A card never repeats these rules.

**Extended on 2026-10-29 (step E):** after testing steps A–D, the user asked that a thread can also be anchored to **the exact selected text** (start line and column
to end line and column), not only to whole lines: selecting one word gave a plain line comment. Section 3 records that design; it supersedes decision 1 of section 2
for selections that do not cover whole lines. Everything steps A–D built for whole lines stays as it is.

## This phase is not a port

Same as phase 11: no Monaco source, no Monaco header, no `// Monaco:` comment, no row in the Monaco → LoomEditor map. Each card says
`Sources: none (specified in this card)` and lists the **existing LoomEditor files to read first**. Write the card's tests before or with the code.

## Order of the cards

Take the cards in table order of [../README.md](../README.md) (each card's "Depends on" is satisfied by the cards above it). Steps:

- **A — Logic, no UI** (P12-01 … P12-04): the line range of `CommentThread`, the suggestion edit / diff for N lines, the strings and the toolbar seed.
- **B — Ranges in the editor** (P12-05 … P12-08): the manager anchors and highlights a range, the controller, the add command with a selection.
- **C — Public API and WinUI** (P12-09 … P12-12): `LoomEditor`, `LoomDiffEditor`, the widget, the gutter button.
- **D — Prove it** (P12-13, P12-14): Gallery demo and manual plan, the Native AOT self-check.
- **E — Exact text ranges (columns)** (P12-16 … P12-23, added 2026-10-29, section 3): the columns of `CommentThread`, the `CommentRange` rule, the manager anchors and
  highlights the exact text, the controller and the add command, the public API, the widget and the gutter button, the Gallery, the self-check and the manual plan.
- **Close** (P12-15, reopened): close the phase after step E.

Steps A, B and the logic cards of E (P12-16 … P12-20) are unit tested (`LoomEditor.Core.Tests`, `LoomEditor.View.Tests`, `LoomEditor.Rendering.Tests`). There is still **no
WinUI test project**: each WinUI card (P12-09 … P12-12, P12-21, P12-22) adds a `##` section "P12-nn" with **measurable** manual checks to `docs/testing/phase-12-manual-tests.md`
(P12-09 creates the file from the header of `docs/testing/phase-11-manual-tests.md`; P12-13 completes it for steps A–C, P12-23 for step E). The Gallery controls those checks
use are added by P12-13 (steps A–C) and P12-23 (step E): a WinUI card's manual section names them as "(Gallery, P12-13)" or "(Gallery, P12-23)". Agents do not run the
Gallery UI; a WinUI card's Result section says the manual checks were not run.

**Recommended model:** every card is written for **Sonnet**: decisions are made, files and members are named, each card is ≤ ~300 lines of production code (step E:
≤ ~400). No card is marked Opus: the only delicate logic (decoration tracking, the zone that follows the last line) already exists in `CommentThreadManager` and is
extended, not redesigned (step E reuses the same decoration and the same stickiness for columns), and every edge case is listed as a test. If a card does not fit in one
session, stop and report with a proposed split.

## Out of scope for phase 12

Dragging from the gutter "+" button or shift-clicking the gutter to select lines (the user selects lines with the keyboard or the mouse in the text, as for any
selection), a gutter bar or overview-ruler mark for the range, any rule about overlapping ranges (two threads may cover the same lines or text; one box per **last** line
from the gutter, as in phase 11), and everything phase 11 deferred. For step E (section 3): a suggestion that replaces only the selected text (it replaces the lines that
contain it, decision 19), columns in the thread title, a thread per secondary cursor, a thread that is switched between "whole lines" and "exact text" after creation, and
a quote of the commented text drawn in the box. Anything the manual plan finds is a follow-up card.

(Until 2026-10-29 this list began with "character-precise ranges (a thread always covers whole lines)"; step E delivers them.)

## 1. Concepts (new in phase 12)

| Concept | LoomEditor |
|---|---|
| the lines a thread covers | `CommentThread.StartLineNumber` … `CommentThread.LineNumber` (inclusive, 1-based model lines; equal for a single-line thread) |
| where the box is | under `LineNumber`, the **last** line of the range (unchanged from phase 11: the zone's `AfterLineNumber`) |
| the range follows edits | the same one model decoration per thread, now spanning `(start, 1)` → `(end, maxColumn)` (P12-05) |
| the highlight | the anchor decoration switches to options with the class `comment-thread-range` (whole-line background) while the thread is multi-line and open (P12-06) |
| the "range text" | the content of lines `start … end` joined with `\n` (never the model's EOL), from `SuggestionEdit.GetRangeText` (P12-02); for one line it is the line content |
| a thread on whole lines / on exact text (step E) | `CommentThread.IsWholeLines`; the columns are `CommentThread.StartColumn` / `EndColumn` (1-based, end exclusive, like `Range`) (P12-16) |
| a selection or range becomes a thread anchor (step E) | `CommentRange.FromRange` / `FromSelection` (P12-17): whole lines, or the exact text (`CommentRange.TextRange`) |
| the "commented text" (step E) | the text of an exact thread's range, lines joined with `\n` (`CommentThreadsController.GetCommentedText`, P12-20); empty for whole lines |

## 2. Design decisions (already made — do not change)

1. **Lines, not columns** — **superseded on 2026-10-29 by section 3** for selections that do not cover whole lines. The original decision (kept for the history of
   steps A–D): a thread covers whole lines; the public API takes two line numbers, never a `Range`; the only place a selection becomes lines is
   `CommentLineRange.FromSelection` (P12-08): `StartLineNumber` … `EndLineNumber`, minus the last line when the selection ends at column 1 of a later line. **What changed
   and why:** the user tested it and selecting one word gave a plain line comment, which loses what the reviewer pointed at. A thread now covers whole lines **or** an
   exact text range (decisions 13–15); the two-line API, the whole-line behavior and the column-1 rule are all kept, `CommentRange` (P12-17) replaces
   `CommentLineRange` (deleted by P12-23), and a `Range` overload is added to the public API (decision 21).
2. **`LineNumber` keeps its name and meaning "the line of the box"**: for a range it is the last line (GitHub's `line`). New: `StartLineNumber` (GitHub's `start_line`).
   A single-line thread has `StartLineNumber == LineNumber`. Every phase 11 caller that reads `LineNumber` stays correct (the box, the gutter "one box per line" rule, the
   caret after Escape).
3. **One change kind.** When `StartLineNumber`, `LineNumber` or both change, `CommentThread` raises `Changed` **once** with the existing `CommentThreadChangeKind.LineChanged`
   (its XML doc says "the line or the range of lines changed"). No new enum member.
4. **Source compatibility.** Every phase 11 public member keeps its signature. New public members are **overloads or added properties only**:
   - `LoomEditor.AddCommentThread(int startLineNumber, int endLineNumber, string? title = null, bool startComposing = false)`;
   - `LoomDiffEditor.AddCommentThread(DiffSide side, int startLineNumber, int endLineNumber, string? title = null, bool startComposing = false)`;
   - `CommentThread.StartLineNumber`; `AddCommentRequestedEventArgs.StartLineNumber`, `DiffAddCommentRequestedEventArgs.StartLineNumber`,
     `SuggestionAppliedEventArgs.StartLineNumber`, each with a new constructor overload that takes it (the old constructors set it to the line).
   - The PublicAPI analyzer may report **RS0026** / **RS0027** (several public overloads with optional parameters). If it does, suppress it on the members it names with
     `[System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Phase 12: the single-line overload stays source compatible.")]`
     (same pattern for RS0027). Do **not** change a signature to avoid it.
5. **Validation.** `1 <= startLineNumber <= endLineNumber <= LineCount`, else `ArgumentOutOfRangeException` (with the parameter that is wrong: `startLineNumber` when it is
   out of the document, `endLineNumber` when it is out of the document or smaller than the start).
6. **Edits.** The decoration keeps `TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges`: typing at the start of the first line or the end of the last line does not grow the
   range; `Enter` at column 1 of the first line moves the whole range down; lines inserted **inside** the range grow it; deleting lines inside shrinks it; deleting the whole
   range collapses it to one line (the thread then is a single-line thread on the line that took its place — never deleted by an edit, as in phase 11). A flush
   (`SetValue`) clamps both ends to the new line count.
7. **Highlight.** Whole-line background with the class `comment-thread-range` → `DecorationStyles` row `new("loom.commentThreadRangeBackground", "editor.rangeHighlightBorder")`;
   new theme id `loom.commentThreadRangeBackground` = `("#BB800933", "#D4A72C33", null, null)` (dark, light, hc-dark, hc-light) in `EditorColorDefaults`; high contrast gets the
   `contrastActiveBorder` outline of `editor.rangeHighlightBorder`. Shown only while the thread is **multi-line and `Open`**; a resolved or single-line thread shows nothing
   (phase 11 look unchanged). Implemented by switching the options of the **same** decoration (`IModelDecorationsChangeAccessor.ChangeDecorationOptions`), never a second decoration.
8. **Suggestions replace the range.** The old text of a suggestion is the range text. `SuggestionEdit` replaces lines `start … end` (deletion removes them and one end of
   line); the diff compares N old lines to M new lines with a line LCS (P12-03); "stale" means the range text differs from the one the block was drawn against. Apply is
   still one undo step and the caret goes to `(start, 1)` (clamped).
9. **The toolbar's "Suggest a change" button** seeds the block with the range text (lines separated by `\n`); `MarkdownToolbar` stops rejecting a multi-line seed (P12-04).
10. **Strings.** New strings go in `CommentStrings` (P12-04). A range reads "lines {start} to {end}" (`"Comment on lines 12 to 15"`); a single line keeps the phase 11 string.
11. **Native AOT.** Same rules as phase 11 (no reflection, no `Regex`, no JSON, no binding). P12-14 adds range checks to `--comments-selfcheck`.
12. **Docs updated at close only** (P12-15): ADR 0014 gets a section "Phase 12: line ranges"; ARCHITECTURE, how-to, PORTING ("Phase 12 (not a port)"), GLOSSARY.
    P12-15 was first done on 2026-10-29 for steps A–D, then **reopened** for step E: it now also documents exact text ranges (decision 23).

## 3. Exact text ranges — step E (added 2026-10-29, already made — do not change)

13. **Why.** Reviewers point at a word or an expression, not only at lines (a word processor's comment, or a code review comment on a token). A thread is anchored on
    **whole lines** (everything decisions 1–12 describe, unchanged) **or on an exact text range**: start line and column to end line and column.
14. **Two kinds, fixed at creation.** `CommentThread.IsWholeLines` (public, get only) says which. New public `CommentThread.StartColumn` and `EndColumn` (1-based, the end is
    **exclusive** as in `Range`):
    - exact text: the columns of the tracked range, kept current by the manager;
    - whole lines: "a line range = columns 1 to the end of its last line": `StartColumn == 1`, `EndColumn == GetLineMaxColumn(LineNumber)`, kept current by the manager.
    A thread never changes kind (an exact range that comes to cover whole lines after edits stays exact).
15. **One rule turns a selection or a range into an anchor:** `CommentRange.FromRange(TextModel model, Range range)` (P12-17), used by the add command, the gutter button and
    the public `Range` overloads. In order: (a) clamp the columns with `model.ValidateRange` (callers have already validated the **lines**); (b) a range ending at column 1
    of a later line ends at the end of the previous line instead; (c) an empty range is the whole line of its start; (d) a range that starts at column 1 and ends at the max
    column of its last line is whole lines; (e) anything else is exact text. So an empty selection, lines selected with `Shift+Down`, a triple-clicked line or a mouse
    drag over full lines keep the phase 12 whole-line behavior (P12-08's tests keep their meaning), and a word gives an exact thread.
16. **Events.** `LineChanged` is still raised once when `StartLineNumber`, `LineNumber` or both change (decision 3). A change of **columns only raises nothing**, and
    `CommentThreadManager.ThreadMoved` is not raised for it: typing before the text on its line shifts the columns on every keystroke, and each `Changed` rebuilds the
    comment box. Hosts that store the columns read them when they save.
17. **Edits.** The same one decoration per thread, now over the exact range, with the same stickiness (`NeverGrowsWhenTypingAtEdges`): typing just before or just after the
    text does not grow the range; typing inside grows it; deleting all of its text leaves an **empty** range (the thread stays, as an edit never deletes a thread, and is no
    longer highlighted); `Enter` inside it makes it multi-line (the box moves under the new last line); a flush (`SetValue`) clamps lines and columns.
18. **Highlight of the exact text.** Open, exact and non-empty → the decoration's options switch to the class `comment-thread-text` (**not** whole-line), drawn by the
    existing normal-decoration renderer: on one line only the selected characters; on several lines the first line from the start column, the middle lines whole, the last
    line up to the end column. Same colors as `comment-thread-range` (`loom.commentThreadRangeBackground`, border `editor.rangeHighlightBorder`); no new theme id. Unlike
    whole lines, a **single-line** exact thread is highlighted (that is the point of selecting a word). Still one decoration per thread; options switched with
    `ChangeDecorationOptions`, never a second decoration.
19. **Suggestions work on the whole lines that contain the range** (rule chosen for step E). For every thread, exact or not, the suggestion block replaces lines
    `StartLineNumber` … `LineNumber`: the "Suggest a change" seed, the range text, the stale check, the diff rows and Apply are exactly decisions 8 and 9, unchanged. Why:
    a ```` ```suggestion ```` block is a code change and GitHub's rule is "the block replaces the commented lines"; one rule for both kinds; no new edit, diff or stale
    code (a block that replaced a fragment would need fragment diff rows and fragment stale checks, and a fragment spanning a line break is ambiguous in a line-based
    block). After Apply, an exact thread's range follows its decoration (usually collapsed to an empty range: no highlight); the host usually resolves the thread.
20. **Title and accessibility.** The title is unchanged ("Comment on line 4" / "Comment on lines 3 to 6"): columns would change on every keystroke before the text and a
    reviewer reads the highlight, not numbers. For screen readers, the box of an exact thread gets `AutomationProperties.HelpText` = `CommentStrings.CommentedText(text)`
    ("Commented text: …", truncated) (P12-22). Announcements and the gutter button name say "the selected text" (decision 22).
21. **Public API (source compatible, additions only).**
    - `LoomEditor.AddCommentThread(Range range, string? title = null, bool startComposing = false)` and
      `LoomDiffEditor.AddCommentThread(DiffSide side, Range range, string? title = null, bool startComposing = false)`: the lines are validated as decision 5 (the
      `ParamName` is the expression, e.g. `range.StartLineNumber`), then decision 15 applies (so a range over full lines gives a whole-line thread).
    - `CommentThread.StartColumn`, `EndColumn`, `IsWholeLines`.
    - `AddCommentRequestedEventArgs.TextRange` and `DiffAddCommentRequestedEventArgs.TextRange` (`Range?`): the exact text asked for, **`null` for whole lines**, with new
      constructors `(Range textRange)` and `(DiffSide side, Range textRange)`. A host written for steps A–D, which reads only the lines, keeps getting whole-line threads.
    - RS0026 / RS0027: decision 4's suppression, on the new overloads.
22. **Strings** (P12-17, all invariant culture): `CommentedText(string text)` → `"Commented text: {text}"` (line breaks shown as spaces, more than
    `MaxCommentedTextLength = 80` characters cut and ended with `…`); `AddCommentOnSelectedText(start, end)` → `"Add a comment on the selected text on line {start}"` /
    `"… on lines {start} to {end}"`; `ThreadAddedOnText(start, end)` → `"Comment thread added on the selected text on line {start}"` / `"… on lines {start} to {end}"`.
23. **Close.** The reopened P12-15 documents column ranges in every doc it writes: the ADR section becomes "Phase 12: line and text ranges", the how-to gains an exact-range
    example (`AddCommentThread(new Range(4, 5, 4, 9))` and answering `AddCommentRequested` with `e.TextRange`), the suggestion rule of decision 19 is stated, and
    "character-precise ranges" leaves every "not delivered" list.
