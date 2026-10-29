# P11-07 — Suggestion blocks (2/2): the red / green diff rows

- **Status:** todo
- **Depends on:** P11-05, P11-06
- **Size:** S

## Goal

`SuggestionDiff.Compute` gives the rows of the small "Suggested change" diff (`-` old line, `+` new lines, red / green in the UI) and a plain-text description for screen readers.

## Sources

Sources: none (specified in this card; ADR 0014 decision 6). Read first: `SuggestionBlock.cs` (P11-06), `CommentStrings.cs` (P11-05).

## Files to create or change

- `src/LoomEditor.View/Comments/SuggestionDiff.cs` — create (`SuggestionDiffRowKind`, `SuggestionDiffRow`, `SuggestionDiffResult`, `SuggestionDiff`)
- `tests/LoomEditor.View.Tests/Comments/SuggestionDiffTests.cs` — create

## Design decisions (already made — do not change)

- `enum SuggestionDiffRowKind { Context, Removed, Added }`; `record SuggestionDiffRow(SuggestionDiffRowKind Kind, string Text)` with `char Marker` (`' '`, `'-'`, `'+'`).
- `sealed record SuggestionDiffResult(IReadOnlyList<SuggestionDiffRow> Rows, bool IsNoOp, bool IsDeletion)`; `string Describe()` (see below).
- `static SuggestionDiffResult SuggestionDiff.Compute(string oldLine, SuggestionBlock block)` — the old text is **one** line (the thread's line), the new text is `block.Lines`:
  - Deletion (empty block): rows = `[Removed(oldLine)]`, `IsDeletion = true`.
  - No-op (`block.Lines` is exactly `[oldLine]`): rows = `[Context(oldLine)]`, `IsNoOp = true` (the UI says "No changes").
  - Otherwise, when some new line equals `oldLine` (the **first** such line): rows = `Added` for the lines before it, `Context(oldLine)`, `Added` for the lines after it (the user added lines around the old one).
  - Otherwise: rows = `Removed(oldLine)` followed by one `Added` per new line.
  - Lines are compared ordinally. Trailing whitespace counts.
- `Describe()` (the UIA text), built only from `CommentStrings` pieces (P11-05): `"Removed: <old>. Added: <new 1>; <new 2>."`; a result without a removed row omits that part; a deletion gives `"Removed: <old>. Line deleted."`; a no-op gives `CommentStrings.NoChanges + "."`. An empty line is written `(empty)`.
- No intra-line (character) highlighting, no use of `DefaultLinesDiffComputer` (a single old line does not need it).

## Tests

- [ ] New: `Compute_OneNewLine_RemovedThenAdded`, `Compute_ManyNewLines_RemovedThenAllAdded`, `Compute_Deletion_OnlyRemoved`, `Compute_SameLine_IsNoOp`, `Compute_NewLineAroundOld_KeepsContext`, `Compute_WhitespaceOnlyChange_IsNotNoOp`, `Describe_Replacement`, `Describe_Deletion`, `Describe_NoOp`, `Describe_EmptyLine_SaysEmpty`, `Marker_MatchesKind`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing the rows (P11-19), intra-line highlights, multi-line old text.

## Suggested commit message

`feat(view): compute the rows of the suggested change diff`
