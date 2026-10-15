# P3-14 — `IEditCommand` and `CommandExecutor`

- **Status:** todo
- **Depends on:** P3-13
- **Size:** M

## Goal

In Monaco, every edit made by the cursor is an `ICommand`: it adds edit operations to a builder, may track selections,
and then computes the cursor state from the inverse edits. `CommandExecutor` runs one command per cursor in a single
undoable model edit, resolves conflicts between cursors ("loser" cursors whose edits overlap), and returns the new
selections. Port both. Monaco's `ICommand` is named `IEditCommand` in C# (ADR 0006, decision 1).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/editorCommon.ts` | 20–49 | `IEditOperationBuilder` (`addEditOperation`, `addTrackedEditOperation`, `trackSelection`) |
| same | 51–65 | `ICursorStateComputerData` (`getInverseEditOperations`, `getTrackedSelection`) |
| same | 67–92 | `ICommand` (`insertsAutoWhitespace`, `getEditOperations`, `computeCursorState`) |
| `src/vs/editor/common/cursor/cursor.ts` | 750–765 | `IExecContext`, `ICommandData`, `ICommandsData` |
| same | 767–1042 | `class CommandExecutor`: `executeCommands`, `_innerExecuteCommands`, `_arrayIsEmpty`, `_getEditOperations`, `_getEditOperationsFromCommand` (the builder and `trackSelection`), `_getLoserCursorMap` |
| `src/vs/editor/test/browser/testCommand.ts` | 17–82 | `testCommand`, `getEditOperation` (test helpers) |

## Files to create or change

- `src/LoomEditor.View/Cursors/Commands/IEditCommand.cs` — complete it (P3-12 created it empty)
- `src/LoomEditor.View/Cursors/Commands/IEditOperationBuilder.cs`, `ICursorStateComputerData.cs` — create
- `src/LoomEditor.View/Cursors/CommandExecutor.cs` — create (split into `CommandExecutor.LoserCursors.cs` if > ~400 lines)
- `tests/LoomEditor.View.Tests/Cursors/TestHelpers/CommandTester.cs` — create
- `tests/LoomEditor.View.Tests/Cursors/CommandExecutorTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- Interfaces (namespace `LoomEditor.View.Cursors.Commands`):
  ```csharp
  internal interface IEditOperationBuilder
  {
      void AddEditOperation(Range range, string? text, bool forceMoveMarkers = false);
      void AddTrackedEditOperation(Range range, string? text, bool forceMoveMarkers = false);
      string TrackSelection(Selection selection, bool? trackPreviousOnEmpty = null);
  }
  internal interface ICursorStateComputerData
  {
      IReadOnlyList<ValidEditOperation> GetInverseEditOperations();
      Selection GetTrackedSelection(string id);
  }
  internal interface IEditCommand
  {
      bool InsertsAutoWhitespace { get; }   // Monaco: optional `insertsAutoWhitespace`
      void GetEditOperations(TextModel model, IEditOperationBuilder builder);
      Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper);
  }
  ```
  `trackPreviousOnEmpty` is `bool?` because Monaco distinguishes "not given" from `false` (lines 940–960):
  for an empty selection, `true` → `GrowsOnlyWhenTypingBefore`, `false` → `GrowsOnlyWhenTypingAfter`, `null` →
  `GrowsOnlyWhenTypingBefore` if the column is the line's max column, else `GrowsOnlyWhenTypingAfter`; a non-empty
  selection → `NeverGrowsWhenTypingAtEdges`. `TrackSelection` returns the **index** of the tracked range as a string
  (`l.toString()`), not the model's range id; `GetTrackedSelection(id)` parses it back.
- `internal static class CommandExecutor` with
  `static IReadOnlyList<Selection>? ExecuteCommands(TextModel model, IReadOnlyList<Selection> selectionsBefore, IReadOnlyList<IEditCommand?> commands)`.
  It edits the model with the internal `TextModel.PushEditOperations(beforeCursorState, IReadOnlyList<IdentifiedEditOperation>, computer)` of P3-10,
  and tracks selections with `TextModel.SetTrackedRange` / `GetTrackedRange` of P3-08. The `editReason` parameter is
  not ported.
- Operation identifiers: `new EditOperationIdentifier(Major: commandIndex, Minor: operationIndexInCommand)` exactly
  as Monaco (`major: majorIdentifier, minor: operationMinor++`). `IsAutoWhitespaceEdit` = the command's
  `InsertsAutoWhitespace`.
- `_getLoserCursorMap` (lines 994–1041) removes items from the list it iterates and rewinds the indices; it sorts with
  a comparator: use a **stable** sort (conventions §4.3). The loser map is keyed by `string` in Monaco
  (`loserCursorsMap[op.identifier.major.toString()]`): use a `HashSet<int>` of majors and say so in a comment.
  "If cursor 0 lost, all commands are ignored" (around line 801): keep it.
- Selections after: losers are removed from the result **in descending index order** (Monaco `splice`). The result
  list may be `selectionsBefore` itself in Monaco (mutated in place): in C# always build a new `List<Selection>`.
- Null commands (`commands[i] == null`) produce no operation and keep `selectionsBefore[i]`, as in Monaco.
- `CommandTester` (test helper, port of `testCommand.ts`):
  - `TestCommand(string[] lines, Selection selection, Func<Selection, IEditCommand> commandFactory, string[] expectedLines, Selection expectedSelection, TextModelOptions? options = null)`:
    creates `new TextModel(string.Join('\n', lines), options)`, runs `CommandExecutor.ExecuteCommands(model, [selection], [command])`,
    asserts `model.GetLinesContent()` and the returned selection (compare with `==`).
    Monaco runs it through a view model; with one cursor and one command the result is the same. Write this in the
    helper's XML doc. Later cards replace nothing: they keep using this helper.
  - `GetEditOperation(TextModel model, IEditCommand command)` returns the list of `(Range Range, string? Text, bool ForceMoveMarkers)`
    added by the command (both add methods collect; `TrackSelection` returns `""`), as in lines 56–82.

## Tests

No Monaco test targets `CommandExecutor` directly (it is tested through every command). New tests, with a tiny
test-only command `InsertTextCommand(Range range, string text)` in `TestHelpers/`:

- [ ] New: two cursors, two commands inserting `"x"` at `(1,1)` and `(1,3)` of `"abcd"` → `"xabxcd"`, one undo step,
      selections from `ComputeCursorState`
- [ ] New: two commands whose ranges overlap → the second cursor is a loser: its edit is dropped and its selection is
      removed from the result (port the reasoning of lines 994–1041 into the test comment)
- [ ] New: a command that calls `TrackSelection` gets back the selection moved by the edits of the other cursor
- [ ] New: tracked ranges are released after `ExecuteCommands` (no tracked range left in the model: expose an
      `internal int TrackedRangeCount` on `TextModel` for the test, or check with `GetTrackedRange(id) == null`)
- [ ] New: a `null` command keeps its selection; all `null` → returns `null` and the model is unchanged
- [ ] New: `GetEditOperation` returns the operations in the order they were added

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map rows `editorCommon.ts` (`ICommand` → `IEditCommand`, builder, computer data) and
      `cursor.ts` (`CommandExecutor`, P3-14); behavior difference "`ICommand` is named `IEditCommand`"
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The concrete commands (P3-15 and later), `CursorsController` (P3-18, P3-19).

## Suggested commit message

`feat(view): port Monaco's edit commands and CommandExecutor`
