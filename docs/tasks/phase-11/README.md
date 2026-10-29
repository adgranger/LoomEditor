# Phase 11 — read this before any card

Phase 11 adds **inline comment threads**: the review comment box of GitHub pull requests, under a code line of the editor or of either side of the diff editor
([phase-11-inline-comments.md](../../roadmap/phase-11-inline-comments.md)). The design is in [ADR 0014](../../adr/0014-inline-comments.md): **read it first**.
This page lists the conventions every phase 11 card relies on; a card never repeats them. The conventions of [phase-3/README.md](../phase-3/README.md) (sections 1, 3–6),
[phase-5/README.md](../phase-5/README.md) (sections 1–5), [phase-7/README.md](../phase-7/README.md) (sections 5–7), [phase-8/README.md](../phase-8/README.md) (sections 1, 2, 4),
[phase-9/README.md](../phase-9/README.md) (sections 3, 6) and [phase-10/README.md](../phase-10/README.md) (sections 1–6) still apply.

## This phase is not a port

Monaco has no comment widget in `src/vs/editor` (VS Code's Comments API is in `src/vs/workbench/contrib/comments`). Therefore, in every phase 11 card:

- **Sources: none (specified in this card).** There is no Monaco file to read; the card is the specification.
- **No Monaco header** on any new file, no `// Monaco:` test comments, no rule "port the tests first". Write the tests from the card's list, **before or with** the code (tests are still mandatory).
- No row in the "Map: Monaco → LoomEditor" table of `docs/PORTING.md`; the close card (P11-25) adds one short section "Phase 11 (not a port)".
- Two Monaco concepts are kept where they apply: the **view zone** (`ViewZone`, `ViewZones`, `IViewZoneChangeAccessor`, ADR 0012 decision 6) and the **content widget** (an element positioned over the editor and repositioned when it scrolls).

## Order of the cards

Take the cards in table order of [../README.md](../README.md) (each card's "Depends on" is satisfied by the cards above it). Steps:

- **A — Logic, no UI** (P11-01 … P11-08): the data model, the Markdown extensions, the toolbar as pure text edits, strings / relative time / initials, suggestion extraction / edit / diff, the thread view model.
- **B — Threads in the editor** (P11-09 … P11-11): tracking over a model with decorations and `Widget` view zones; the controller (with the apply-suggestion path) and the add command; the public API of the control.
- **C — The widget** (P11-12 … P11-21): the zone-widget layer and positioning, the height loop, focus and keys, the shared Markdown builder, the four parts of the widget (comments, composer, toolbar, suggestion block), accessibility, the gutter button.
- **D — Diff editor** (P11-22).
- **E — Prove it and close** (P11-23 … P11-25): Gallery demo and manual plan, the Native AOT self-check, close the phase.

Steps A and B need no UI (unit tested in `LoomEditor.Core.Tests` and `LoomEditor.View.Tests`). There is **no WinUI test project**: every WinUI card (C, D) has **measurable manual checks**
(pixels, counts, an event log, the diagnostics line of the zone host) for the maintainer in `docs/testing/phase-11-manual-tests.md` (P11-12 creates the file; each card adds a `##` section "P11-nn"; P11-23 completes it).
Agents do **not** run the Gallery UI: a WinUI card is "done" when it builds with 0 warnings, all tests pass and its manual section is written; its Result section must say the manual checks were not run.
**Recommended model: Opus** is on P11-12, P11-13 and P11-14 only (element over a zone, the height feedback loop, focus routing). If any card does not fit in one session, stop and report with a proposed split.

## Out of scope for phase 11

Multi-line thread ranges, reactions, comment edit / delete buttons, `@mentions`, attachments, avatar images (initials are drawn), localization (all strings are in `CommentStrings`),
original-side threads in the inline diff view, "commit suggestion", a comments panel, persistence, intra-line highlights in the suggestion diff, a WinUI test project.
Anything the manual plan finds is a follow-up card, not a change to a closed one.

## 1. Concepts (new in phase 11)

| Concept | LoomEditor |
|---|---|
| a review thread on a line | `CommentThread` (Core, public) with `Comment`s written by a `CommentAuthor` |
| the gap that holds the bubble | a `ViewZone` of the new kind `ViewZoneKind.Widget` (P11-09); the renderer paints nothing in it |
| the bubble | `CommentThreadWidget` (WinUI element in the new template part `PART_ZoneWidgetCanvas`, below the find widget and the hover), placed by `CommentZoneHost` (P11-12) from `ViewLayout.GetWhitespaceViewportData()` in the draw pass |
| the line follows edits | one model decoration per thread (`NeverGrowsWhenTypingAtEdges`), owned by `CommentThreadManager` |
| the Markdown toolbar | `MarkdownToolbar.Apply(action, text, selection, seed)` → `ToolbarEdit` (pure) |
| the "suggested change" | `SuggestionExtractor`, `SuggestionDiff`, `SuggestionEdit` (pure) |
| the state of one open box | `CommentThreadViewModel` (draft, preview, can submit) |
| one editor's threads | `CommentThreadsController`, created by the control, stored in `EditorContext.CommentThreads` (internal; `IEditorContext` is public and is not changed) |

## 2. Conventions

0. **Name clash to avoid.** `LoomEditor.View/Features/Comment/` already exists: it is *Toggle Line Comment* (`//` in code). Phase 11 never touches it; the new feature folder is `Features/CommentThreads/`. In code and docs, "comment" alone means a review comment only inside the phase 11 types.

1. **Namespaces.** Core: `LoomEditor.Core.Comments` (public types). View: `LoomEditor.View.Comments` (logic), `LoomEditor.View.Features.CommentThreads` (feature), `LoomEditor.View.Markdown`. WinUI: `LoomEditor.WinUI.Controls.Comments`, `LoomEditor.WinUI.Controls.Markdown`. Tests mirror `src/`.
2. **Visibility.** Public only what ADR 0014 decisions 2 and 3 list (`IEditorContext`, which is public, is never changed) (add them to `PublicAPI.Unshipped.txt` with `./eng/Update-PublicApi.ps1 src/LoomEditor.Core` / `src/LoomEditor.WinUI`). Everything else `internal`.
3. **Lines are 1-based model line numbers; columns 1-based; text offsets in the toolbar and suggestion code are 0-based UTF-16 indices** (it is plain string work, not the model). Do not convert.
4. **Strings.** A user-visible English string is a `const` in `CommentStrings` (P11-05 creates it; no card before P11-05 shows text). UIA names too.
5. **No reflection, no `x:Bind` / `{Binding}`, no new `Regex`, no JSON** in this phase (ADR 0014 decision 9); WinUI elements are built in code, like the hover and the completion list. Pure logic uses `string`, `ReadOnlySpan<char>`, `StringBuilder`.
6. **UI thread only** for everything that touches a model, a zone or an element; pure functions are thread-safe (say it in the `<remarks>`).
7. **The widget never mutates the model by itself**, except `Apply suggestion` (ADR 0014 decision 6). Submitting a comment raises an event; the host adds the comment.
8. **Tests.** `Method_Scenario_ExpectedResult` names. View tests use `tests/LoomEditor.View.Tests/TestHelpers`; Core tests `tests/LoomEditor.Core.Tests`. Randomized tests (if any) use `new Random(seed)` with the seed in the failure message.
9. **Manual checks.** The agent adds the lines to `docs/testing/phase-11-manual-tests.md` and does **not** run the Gallery unless the card says so. The temporary Gallery sample "Comment threads" created by P11-12 is extended by each WinUI card with the buttons its manual checks need (a few lines each) and is replaced by the real demo in P11-23.
10. **Cards of this phase have no "Monaco sources" table**: the section says `Sources: none (specified in this card)` and lists, when useful, the **existing LoomEditor files to read first**.
