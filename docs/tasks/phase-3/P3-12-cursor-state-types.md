# P3-12 — Cursor state types and the identity coordinates converter

- **Status:** done
- **Depends on:** P3-11
- **Size:** S

## Goal

Create the small value types the whole cursor is built from: the state of one cursor (`SingleCursorState`), the pair
model/view state (`CursorState` and its partial forms), the kinds of edits (`EditOperationType`), the reasons of a
cursor change, the result of an edit operation, the "simple model" the move operations read, and the identity
coordinates converter (view = model until word wrap, ADR 0005 decision 6).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursorCommon.ts` | 22–28 | `IColumnSelectData` |
| same | 34–41 | `const enum EditOperationType` (Other = 0, DeletingLeft = 2, DeletingRight = 3, TypingOther = 4, TypingFirstSpace = 5, TypingConsecutiveSpace = 6 — there is **no** 1) |
| same | 253–267 | `interface ICursorSimpleModel` |
| same | 269–331 | `PartialCursorState`, `CursorState`, `PartialModelCursorState`, `PartialViewCursorState` |
| same | 333–400 | `const enum SelectionStartKind`, `class SingleCursorState` |
| same | 402–435 | `class EditOperationResult` (with `followUp`), `isQuote` |
| `src/vs/editor/common/cursorEvents.ts` | 12–41 | `const enum CursorChangeReason` |
| `src/vs/editor/common/coordinatesConverter.ts` (the card said `viewModel/coordinatesConverter.ts`, which does not exist) | 10–105 | `interface ICoordinatesConverter`, `class IdentityCoordinatesConverter` |

## Files to create or change

- `src/LoomEditor.View/Cursors/EditOperationType.cs`, `SelectionStartKind.cs`, `CursorChangeReason.cs`,
  `ColumnSelectData.cs`, `SingleCursorState.cs`, `CursorState.cs` (the four state classes), `EditOperationResult.cs`,
  `ICursorSimpleModel.cs`, `TextModelCursorSimpleModel.cs` — create
- `src/LoomEditor.View/Cursors/Commands/IEditCommand.cs` — create **only the empty interface declaration** with a
  `// Completed in P3-14` comment, because `EditOperationResult` references it
- `src/LoomEditor.View/Layout/ICoordinatesConverter.cs`, `IdentityCoordinatesConverter.cs` — create
- `src/LoomEditor.View/Cursors/README.md` — update: what is there now
- `tests/LoomEditor.View.Tests/Cursors/SingleCursorStateTests.cs`, `tests/LoomEditor.View.Tests/Layout/IdentityCoordinatesConverterTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first (API map, JavaScript traps).
- `SingleCursorState` is an `internal sealed class` (not a record: its equality is custom). Constructor
  `(Range selectionStart, SelectionStartKind selectionStartKind, int selectionStartLeftoverVisibleColumns, Position position, int leftoverVisibleColumns)`;
  read-only properties with the same names in PascalCase plus `Selection Selection` computed in the constructor
  (`_computeSelection`, lines 393–399: `new Selection(selectionStart.Start, position)` when the start range is empty
  or `position` is not before-or-equal the start; otherwise `new Selection(selectionStart.End, position)`).
  Methods `bool HasSelection()`, `SingleCursorState Move(bool inSelectionMode, int lineNumber, int column, int leftoverVisibleColumns)`,
  and `bool Equals(SingleCursorState other)` as in lines 357–365 (it does **not** compare `Selection`). Implement
  `IEquatable<SingleCursorState>` + `Equals(object)` + `GetHashCode` consistently with that.
- The states: `internal abstract class PartialCursorState { public abstract SingleCursorState? ModelState { get; } public abstract SingleCursorState? ViewState { get; } }`
  with three sealed subclasses: `CursorState` (both non-null; `Equals(CursorState)` compares view then model state),
  `PartialModelCursorState` (`ViewState` is `null`), `PartialViewCursorState` (`ModelState` is `null`).
  Static factories on `CursorState`: `FromModelState`, `FromViewState`, `FromModelSelection(Selection)`,
  `FromModelSelections(IReadOnlyList<Selection>)` (lines 274–298).
- `EditOperationResult`: `internal sealed class` with `EditOperationType Type`, `IReadOnlyList<IEditCommand?> Commands`,
  `bool ShouldPushStackElementBefore`, `bool ShouldPushStackElementAfter`,
  `Func<IReadOnlyList<Selection>, EditOperationResult?>? FollowUp`.
- `ColumnSelectData` = `internal readonly record struct ColumnSelectData(bool IsReal, int FromViewLineNumber, int FromViewVisualColumn, int ToViewLineNumber, int ToViewVisualColumn)` (check the field names at lines 22–28).
- `ICursorSimpleModel` has Monaco's 8 members (`GetLineCount`, `GetLineContent`, `GetLineMinColumn`, `GetLineMaxColumn`,
  `GetLineFirstNonWhitespaceColumn`, `GetLineLastNonWhitespaceColumn`, `NormalizePosition(Position, PositionAffinity)`,
  `GetLineIndentColumn`). `TextModel` lives in Core and cannot implement a View interface, so add
  `internal sealed class TextModelCursorSimpleModel(TextModel model) : ICursorSimpleModel` that forwards each call
  (`PositionAffinity`, `NormalizePosition`, `GetLineIndentColumn` come from P3-09).
- `ICoordinatesConverter` has the members of lines 10–30. `IdentityCoordinatesConverter(TextModel model)` validates with
  `model.ValidatePosition` / `ValidateRange` exactly as Monaco; `ValidateViewPosition(view, expectedModel)` returns the
  validated **expected model** position (it ignores the view position — that is Monaco's behavior). Optional
  parameters (`affinity`, `allowZeroLineNumber`, `belowHiddenRanges`) are kept as C# optional parameters and ignored.
- `IsQuote(char ch)` is a static method of a small `internal static class CursorCommon`.

## Tests

- [x] New: `SingleCursorState.Selection` for start `(1,5)-(1,5)` and position `(1,2)` is `(1,5) → (1,2)`; for a word
      start `(1,3)-(1,7)` and position `(1,1)` it is `(1,7) → (1,1)` (the start range's end is kept when moving before it)
- [x] New: `Move(inSelectionMode: true, …)` keeps `SelectionStart` and kind; `Move(false, …)` collapses with kind `Simple`
- [x] New: two states that differ only in `LeftoverVisibleColumns` are not equal; `HasSelection()` is true when only
      the selection start range is non-empty
- [x] New: `CursorState.FromModelSelections` keeps the order and the direction of each selection
- [x] New: `IdentityCoordinatesConverter` clamps `(10, 50)` to the last valid position of a 3-line model, and
      `ModelPositionIsVisible((4,1))` is false

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `editor/common/cursorCommon.ts` (🔶 types, P3-12), `cursorEvents.ts` (`CursorChangeReason`),
      `viewModel/coordinatesConverter.ts` (identity converter only)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`CursorConfiguration` (P3-13), commands (P3-14), the cursor itself (P3-16).

## Suggested commit message

`feat(view): add the cursor state types and the identity coordinates converter`
