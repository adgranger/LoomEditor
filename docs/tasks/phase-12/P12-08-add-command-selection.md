# P12-08 — The add command uses a multi-line selection

- **Status:** done
- **Depends on:** P12-07
- **Size:** S

**Recommended model:** Sonnet

## Goal

`loom.commentThreads.add` asks for a thread on the **lines of the primary selection** when it spans several lines, and on the cursor line otherwise. The rule that turns a
selection into lines lives in one pure helper, also used by the gutter button (P12-12).

## Sources

Sources: none (specified in this card; [phase-12/README.md](README.md) decision 1). Read first: `src/LoomEditor.View/Features/CommentThreads/CommentThreadsFeature.cs`,
`src/LoomEditor.Core/Text/Selection.cs` (`IsEmpty`, `PositionLineNumber`, `Range`: a `Range` whose constructor orders the two positions), `IEditorContext.Selection`,
`tests/LoomEditor.View.Tests/Features/CommentThreads/CommentThreadsFeatureTests.cs`.

## Files to create or change

- `src/LoomEditor.View/Comments/CommentLineRange.cs` — create
- `src/LoomEditor.View/Features/CommentThreads/CommentThreadsFeature.cs` — change
- `src/LoomEditor.View/Features/CommentThreads/CommentThreadsCommands.cs` — change: XML doc of `Add` ("on the lines of the selection, or the cursor line")
- `tests/LoomEditor.View.Tests/Comments/CommentLineRangeTests.cs` — create
- `tests/LoomEditor.View.Tests/Features/CommentThreads/CommentThreadsFeatureTests.cs` — change

## Design decisions (already made — do not change)

- `internal static class CommentLineRange` (namespace `LoomEditor.View.Comments`, `<remarks>Threading: pure and thread-safe.</remarks>`) with
  `public static (int StartLineNumber, int LineNumber) FromSelection(Selection selection)`:
  - empty selection → `(PositionLineNumber, PositionLineNumber)`;
  - otherwise, with `Range r = selection.Range`: `start = r.StartLineNumber`, `end = r.EndLineNumber`, and when `r.EndColumn == 1 && end > start` → `end - 1` (whole lines selected with
    `Shift+Down` end at column 1 of the next line). Direction does not matter.
- The feature's command: `var (start, line) = CommentLineRange.FromSelection(context.Selection); controller?.RequestAdd(start, line);`.
- The command never adds a thread (unchanged, ADR 0014): the host answers `AddCommentRequested`.

## Tests

- [x] Helper: `FromSelection_Empty_CursorLine`, `FromSelection_SingleLineNonEmpty_ThatLine`, `FromSelection_ThreeLines_StartToEnd`, `FromSelection_EndsAtColumnOne_ExcludesLastLine`,
  `FromSelection_TwoLinesEndingAtColumnOne_IsOneLine`, `FromSelection_RightToLeft_SameAsLeftToRight`
- [x] Feature: `AddCommand_MultiLineSelection_RequestsRange`, `AddCommand_EmptySelection_RequestsCursorLine` (the existing single-line test keeps passing)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Secondary cursors (only the primary selection counts), a default key, the gutter button (P12-12).

## Suggested commit message

`feat(view): request a comment thread on the lines of a multi-line selection`
