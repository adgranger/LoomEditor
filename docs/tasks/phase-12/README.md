# Phase 12 — read this before any card

Phase 12 lets an inline comment thread cover **a range of lines** instead of one line, as a GitHub pull request review does ("Comment on lines +12 to +15")
([phase-12-comment-ranges.md](../../roadmap/phase-12-comment-ranges.md)). It extends phase 11: read [phase-11/README.md](../phase-11/README.md) and
[ADR 0014](../../adr/0014-inline-comments.md) first. **Every convention of phase-11/README.md (sections "This phase is not a port", 1 and 2) applies unchanged**;
this page only adds what is new. A card never repeats these rules.

## This phase is not a port

Same as phase 11: no Monaco source, no Monaco header, no `// Monaco:` comment, no row in the Monaco → LoomEditor map. Each card says
`Sources: none (specified in this card)` and lists the **existing LoomEditor files to read first**. Write the card's tests before or with the code.

## Order of the cards

Take the cards in table order of [../README.md](../README.md) (each card's "Depends on" is satisfied by the cards above it). Steps:

- **A — Logic, no UI** (P12-01 … P12-04): the line range of `CommentThread`, the suggestion edit / diff for N lines, the strings and the toolbar seed.
- **B — Ranges in the editor** (P12-05 … P12-08): the manager anchors and highlights a range, the controller, the add command with a selection.
- **C — Public API and WinUI** (P12-09 … P12-12): `LoomEditor`, `LoomDiffEditor`, the widget, the gutter button.
- **D — Prove it and close** (P12-13 … P12-15): Gallery demo and manual plan, the Native AOT self-check, close the phase.

Steps A and B are unit tested (`LoomEditor.Core.Tests`, `LoomEditor.View.Tests`, `LoomEditor.Rendering.Tests`). There is still **no WinUI test project**: each WinUI card
(P12-09 … P12-12) adds a `##` section "P12-nn" with **measurable** manual checks to `docs/testing/phase-12-manual-tests.md` (P12-09 creates the file from the header of
`docs/testing/phase-11-manual-tests.md`; P12-13 completes it). The Gallery controls those checks use are added by P12-13: a WinUI card's manual section names them as
"(Gallery, P12-13)". Agents do not run the Gallery UI; a WinUI card's Result section says the manual checks were not run.

**Recommended model:** every card is written for **Sonnet**: decisions are made, files and members are named, each card is ≤ ~300 lines of production code. No card is
marked Opus: the only delicate logic (decoration tracking, the zone that follows the last line) already exists in `CommentThreadManager` and is extended, not redesigned,
and every edge case is listed as a test. If a card does not fit in one session, stop and report with a proposed split.

## Out of scope for phase 12

Character-precise ranges (a thread always covers whole lines), dragging from the gutter "+" button or shift-clicking the gutter to select lines (the user selects lines
with the keyboard or the mouse in the text, as for any selection), a gutter bar or overview-ruler mark for the range, any rule about overlapping ranges (two threads may cover
the same lines; one box per **last** line from the gutter, as in phase 11), and everything phase 11 deferred. Anything the manual plan finds is a follow-up card.

## 1. Concepts (new in phase 12)

| Concept | LoomEditor |
|---|---|
| the lines a thread covers | `CommentThread.StartLineNumber` … `CommentThread.LineNumber` (inclusive, 1-based model lines; equal for a single-line thread) |
| where the box is | under `LineNumber`, the **last** line of the range (unchanged from phase 11: the zone's `AfterLineNumber`) |
| the range follows edits | the same one model decoration per thread, now spanning `(start, 1)` → `(end, maxColumn)` (P12-05) |
| the highlight | the anchor decoration switches to options with the class `comment-thread-range` (whole-line background) while the thread is multi-line and open (P12-06) |
| the "range text" | the content of lines `start … end` joined with `\n` (never the model's EOL), from `SuggestionEdit.GetRangeText` (P12-02); for one line it is the line content |

## 2. Design decisions (already made — do not change)

1. **Lines, not columns.** A thread covers whole lines. The public API takes two line numbers, never a `Range`: GitHub's review API has the same shape (`start_line`, `line`),
   and it avoids a public rule about columns. The only place a selection becomes lines is `CommentLineRange.FromSelection` (P12-08): `StartLineNumber` … `EndLineNumber`,
   minus the last line when the selection ends at column 1 of a later line (`EndColumn == 1 && EndLineNumber > StartLineNumber`), which is what a user gets by selecting
   whole lines with `Shift+Down`.
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
