# P12-19 — Highlight the exact text of a thread

- **Status:** todo
- **Depends on:** P12-18
- **Size:** S

**Recommended model:** Sonnet

## Goal

The text an open exact thread covers gets GitHub's yellow tint, and only that text: on one line just the selected characters; on several lines a partial first line, whole
middle lines and a partial last line. Resolving the thread or emptying its text removes the tint; reopening brings it back. Whole-line threads look as in P12-06.

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decision 18). Read first: `src/LoomEditor.View/Comments/CommentThreadManager.cs` (`AnchorOptions`,
`RangeAnchorOptions`, `OptionsFor`, `UpdateOptions`), `src/LoomEditor.View/Layout/DecorationStyles.cs` (the `comment-thread-range` row), `src/LoomEditor.Rendering/TextViewRenderer.Decorations.cs`
(`DrawNormalDecorations`: how a non-whole-line class decoration is drawn over several lines — no change there), `tests/LoomEditor.Rendering.Tests/TextViewRendererDecorationsTests.cs`
(`CreateHarness`, `AddDecoration`, `Fills`, `Render_MultiLineDecoration_ContinuesToTheLineEnd`), `tests/LoomEditor.View.Tests/Layout/DecorationStylesTests.cs`,
`docs/tasks/phase-12/P12-06-range-highlight.md`.

## Files to create or change

- `src/LoomEditor.View/Comments/CommentThreadManager.cs` — change
- `src/LoomEditor.View/Layout/DecorationStyles.cs` — change: one row
- `tests/LoomEditor.View.Tests/Comments/CommentThreadManagerTests.cs` — change
- `tests/LoomEditor.View.Tests/Layout/DecorationStylesTests.cs` — change (one test)
- `tests/LoomEditor.Rendering.Tests/TextViewRendererDecorationsTests.cs` — change (two tests)

## Design decisions (already made — do not change)

- New `private static readonly ModelDecorationOptions TextAnchorOptions = ModelDecorationOptions.Register(new ModelDecorationOptionsData { Description = "comment-thread-text",
  Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges, ClassName = "comment-thread-text" })` — **not** `IsWholeLine`, no `ShouldFillLineOnLineBreak`.
- `OptionsFor(CommentThread t)` becomes:
  - `t.State != CommentThreadState.Open` → `AnchorOptions`;
  - `t.IsWholeLines` → `t.StartLineNumber < t.LineNumber ? RangeAnchorOptions : AnchorOptions` (P12-06, unchanged);
  - exact text: empty (`t.StartLineNumber == t.LineNumber && t.EndColumn <= t.StartColumn`) → `AnchorOptions`, else `TextAnchorOptions`.
- Re-evaluation is the existing `UpdateOptions`: after `SyncLines` (P12-18 passes every entry whose lines **or columns** changed, so a range that empties loses its tint) and
  on `StateChanged`. No new subscription.
- `DecorationStyles` row, right after `comment-thread-range`, with the comment `// LoomEditor's own class (phase 12): the exact text of a comment thread`:
  `["comment-thread-text"] = new("loom.commentThreadRangeBackground", "editor.rangeHighlightBorder"),`. No new theme color (same tint as the lines).
- No renderer change: `DrawNormalDecorations` already draws a multi-line class decoration with a partial first and last line. The Rendering tests below prove it for the new
  class. If a width differs from the expectation below, do **not** change the renderer: assert what it draws for `wordHighlight` on the same text and say so in the card's
  Result section.

## Tests

- [ ] Manager: `HighlightText_Open_UsesTextClass` (options read back with `TextModel.GetDecorationOptions(GetDecorationId(t))`, like P12-06), `HighlightText_SingleLine_IsHighlighted`
- [ ] `HighlightText_Resolved_Removed_ThenReopened_Restored`, `HighlightText_AllTextDeleted_Removed`, `HighlightText_SwitchOptions_KeepsTrackedRange`
- [ ] `HighlightLines_Unchanged` (a single whole-line thread still has no class; a 2-line whole-line thread still uses `comment-thread-range`)
- [ ] `DecorationStyles`: `Get_CommentThreadText_HasBackgroundAndBorderIds`
- [ ] Rendering: `Render_CommentThreadText_OneLine_FillsOnlyTheText` (`"abcdef"`, `Range(1, 2, 1, 5)`: one fill of `loom.commentThreadRangeBackground` at
  `contentLeft + CharacterWidth`, width `3 * CharacterWidth`)
- [ ] Rendering: `Render_CommentThreadText_ThreeLines_PartialFirstAndLastLines` (`"abcd\nefgh\nijkl"`, `Range(1, 3, 3, 2)`: three fills; line 1 starts at
  `contentLeft + 2 * CharacterWidth`, line 2 starts at `contentLeft`, line 3 starts at `contentLeft` with width `CharacterWidth`). The theme of `CreateHarness` must define
  `loom.commentThreadRangeBackground` (add it to the test theme JSON next to `editor.rangeHighlightBackground` if `Fills` needs it).
- [ ] All existing tests unchanged and green

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

A second decoration, a different color for exact text, a gutter or overview-ruler mark, any renderer change, theme JSON changes outside the tests.

## Suggested commit message

`feat(view): highlight the exact text of a comment thread`
