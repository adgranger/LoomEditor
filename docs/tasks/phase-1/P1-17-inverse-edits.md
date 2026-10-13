# P1-17 — Edit operation types and pure edit helpers

- **Status:** done
- **Depends on:** P1-16
- **Size:** M

## Goal

The edit-operation types used by `ApplyEdits`, plus the helpers that can be tested without applying anything:
computing where each edit's text ends up (the ranges needed for undo), and merging several edits into one.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts` | 16–32 | `IValidatedEditOperation`, `IReverseSingleEditOperation` |
| same | 434–477 | `_toSingleEditOperation` |
| same | 532–611 | `_getInverseEditRange`, `_getInverseEditRanges` |
| same | 613–628 | `_sortOpsAscending`, `_sortOpsDescending` |
| `src/vs/editor/common/model.ts` | 536–554 | `IValidEditOperation` |
| same | 1514–1523 | `ValidAnnotatedEditOperation` |
| same | 1603–1620 | `ApplyEditsResult`, `IInternalModelContentChange` |
| `src/vs/editor/common/model/mirrorTextModel.ts` | 12–29 | `IModelContentChange` |
| `src/vs/editor/test/common/model/linesTextBuffer/linesTextBuffer.test.ts` | 13–267 | suite `PieceTreeTextBuffer._getInverseEdits` (17 tests) |
| same | 269–525 | suite `PieceTreeTextBuffer._toSingleEditOperation` (12 tests) |

## Files to create or change

`src/LoomEditor.Core/Model/Edits/` (namespace `LoomEditor.Core.Model.Edits`), all `internal` except where noted:
- `EditOperationIdentifier.cs` — `readonly record struct EditOperationIdentifier(int Major, int Minor)` (Monaco `ISingleEditOperationIdentifier`)
- `ValidAnnotatedEditOperation.cs` — `sealed record` with Monaco's six fields (`IsTracked` for `_isTracked`)
- `ValidatedEditOperation.cs` — `sealed class` (Monaco `IValidatedEditOperation`; mutable like Monaco: `SortIndex` is assigned later)
- `ValidEditOperation.cs` — `sealed record ValidEditOperation(EditOperationIdentifier? Identifier, Range Range, string Text)`
- `ApplyEditsResult.cs` — `sealed record ApplyEditsResult(IReadOnlyList<ValidEditOperation>? ReverseEdits, IReadOnlyList<InternalModelContentChange> Changes, IReadOnlyList<int>? TrimAutoWhitespaceLineNumbers)`
- `src/LoomEditor.Core/Model/ModelContentChange.cs` — **public** `sealed record ModelContentChange(Range Range, int RangeOffset, int RangeLength, string Text)`
- `src/LoomEditor.Core/Model/Edits/InternalModelContentChange.cs` — `sealed record InternalModelContentChange(ModelContentChange Change, bool ForceMoveMarkers)`
- `src/LoomEditor.Core/Model/PieceTree/PieceTreeTextBuffer.EditHelpers.cs` — `ToSingleEditOperation`, `GetInverseEditRange(s)`, the two sort comparers (static where Monaco is static)
- `tests/LoomEditor.Core.Tests/Model/PieceTree/PieceTreeTextBufferInverseEditsTests.cs`, `PieceTreeTextBufferToSingleEditOperationTests.cs`
- Run `./eng/Update-PublicApi.ps1 src/LoomEditor.Core` (for `ModelContentChange`)

## Design decisions (already made — do not change)

- Monaco's `text: string | null` on `ValidAnnotatedEditOperation` stays `string?`.
- Monaco's `_getInverseEditRanges` and `_toSingleEditOperation` are underscore-prefixed only because they are "internal
  for tests" in TypeScript; in C# they are plain `internal` members (tests see internals).
- Records with list members do not compare lists by value: in tests, compare field by field (write a small
  `AssertEditOperation` helper) instead of `Assert.Equal` on whole records.

## Tests

- [x] `PieceTreeTextBuffer._getInverseEdits` — 17 tests (`single insert`, `Bug 19872: Undo is funky`, …, `many edits`)
- [x] `PieceTreeTextBuffer._toSingleEditOperation` — 12 tests (`one edit op is unchanged`, …, `advanced simplified`)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`ApplyEdits` itself (P1-18).

## Suggested commit message

`feat(core): add edit operation types and inverse edit helpers`
