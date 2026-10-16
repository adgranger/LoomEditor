# P3-19 — `CursorsController` (2/2): executing edits and commands

- **Status:** done
- **Depends on:** P3-18
- **Size:** S

## Goal

Give the controller its editing path: run an `EditOperationResult` (undo stops before/after, the commands through
`CommandExecutor`, the follow-up), run one or several `IEditCommand`s, and apply raw edits with a cursor state
computer (`executeEdits`). Typing, paste, cut and composition are added by the cards that port their operations
(P3-33, P3-35, P3-36, P3-49): they all go through the methods of this card.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/cursor/cursor.ts`. If a function is not exactly at the given line, search for its name.

| Lines | What to port |
|---|---|
| 354–396 | `_executeEditOperation` (see the auto-closing cut below) |
| 475–515 | `executeEdits` |
| 517–540 | `_executeEdit` (read-only check, stop/start tracking, `_isHandling`, emit + reveal) |
| 624–646 | `executeCommand`, `executeCommands` |

## Files to create or change

- `src/LoomEditor.View/Cursors/CursorsController.Edits.cs` — create (partial)
- `tests/LoomEditor.View.Tests/Cursors/CursorsControllerEditTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- Signatures (the `TextModelEditSource` / `editReason` parameters of Monaco are not ported):
  - `void ExecuteEdits(ViewModelEventsCollector eventsCollector, string? source, IReadOnlyList<IdentifiedEditOperation> edits, Func<IReadOnlyList<ValidEditOperation>, IReadOnlyList<Selection>?> cursorStateComputer)`
  - `void ExecuteCommand(ViewModelEventsCollector eventsCollector, IEditCommand command, string? source)`
  - `void ExecuteCommands(ViewModelEventsCollector eventsCollector, IReadOnlyList<IEditCommand?> commands, string? source)`
  - `internal void ExecuteEditOperation(EditOperationResult? opResult)` (Monaco `_executeEditOperation`, made internal
    so that the operation cards can call it) and `internal void ExecuteEdit(Action callback, ViewModelEventsCollector eventsCollector, string? source, CursorChangeReason reason = CursorChangeReason.NotSet)`
    (Monaco `_executeEdit`).
- `_executeEditOperation`: port it line by line, but the block "Check for auto-closing closed characters"
  (`BaseTypeWithAutoClosingCommand`) is a phase 4–5 cut point: write the comment, no code.
- `executeEdits`: `source === 'snippet'` and `_findAutoClosingPairs` are a phase 4–5 cut point (snippets): never true
  in phase 3; keep the `autoClosingIndices == null` path only. `_isHandling = true` is set **inside** the computer
  when it returns selections, then reset after `PushEditOperations`, exactly as Monaco.
- `_executeEdit` returns immediately when `Context.CursorConfig.ReadOnly` is true (the view model emits the
  `ReadOnlyEditAttemptEvent`, P3-20).
- Exceptions inside the callback: Monaco calls `onUnexpectedError(err)` and continues. In C#, **do not catch**: let the
  exception propagate (a bug in a port must fail the tests). Document this in `docs/PORTING.md` → "Behavior differences".
  The `finally`-like part (`_isHandling = false`, `StartTrackingSelections`) must still run: use `try/finally`.

## Tests

The Monaco tests come with the operations (P3-25 and later). New tests with the `CursorTestContext` of P3-16 and the
`InsertTextCommand` test helper of P3-14:

- [x] New: `ExecuteCommands` with one command per cursor (3 cursors) inserts at each cursor in **one** undo step and
      moves the cursors; one `CursorStateChangedEvent` (reason `NotSet`) and one reveal request are collected
- [x] New: `ExecuteCommand` kills the secondary cursors first
- [x] New: `ExecuteEditOperation` with `ShouldPushStackElementBefore = true` creates a separate undo step;
      with a `FollowUp`, the follow-up runs with the new selections
- [x] New: `ExecuteEdits` with a computer that returns `[(1,5)]` → the cursor is at `(1,5)` and was **not** recovered
      from markers
- [x] New: with `ReadOnly = true` in the configuration, `ExecuteCommands` changes nothing and emits nothing
- [x] New: an exception thrown by a command propagates, and the cursors still track their selections afterwards

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row `cursor.ts` (P3-19); behavior difference "exceptions in cursor edits are not swallowed"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`type`, `compositionType`, `startComposition`, `endComposition`, `paste`, `cut`, `CompositionState` (later cards).

## Suggested commit message

`feat(view): execute edits and commands in CursorsController`

## Deviations decided while working on the card

- The model delivered `RawContentChanged` immediately, unlike Monaco (`DidChangeContentEmitter`), so `executeEdits`
  could not keep the cursor from recovering from markers. With the maintainer's agreement the model now defers and
  merges the content events of `PushEditOperations` and of undo/redo, and delivers them with the resulting selections
  (`TextModel.DeferredEmit.cs`, two new Core tests).
- The "exception thrown by a command" test uses a command whose `ComputeCursorState` throws: `CommandExecutor` swallows
  exceptions of `GetEditOperations`, like Monaco (P3-14).
