# P3-26 — Core navigation commands (`cursorLeft`, `cursorHome`, `cursorMove`…)

- **Status:** todo
- **Depends on:** P3-25
- **Size:** M

## Goal

Port Monaco's keyboard navigation commands of `coreCommands.ts` and register them in the editor's `CommandRegistry`
under Monaco's ids, so that `Commands.Execute("cursorLeft")` moves every cursor. Port the whole
`cursorMoveCommand.test.ts` (except the folding suite).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/browser/coreCommands.ts`. If a line is not exactly where given, search for the name.

| Lines | What to port |
|---|---|
| 38–49 | `abstract class CoreEditorCommand<T>` (`runCoreEditorCommand(viewModel, args)`) |
| 368–418 | `BaseMoveToCommand`, `MoveTo` (`_moveTo`), `MoveToSelect` (`_moveToSelect`) |
| 589–649 | `CursorMoveImpl`, `CursorMove` (`cursorMove`) |
| 651–893 | `PAGE_SIZE_MARKER`, `CursorMoveBasedCommand`, `CursorLeft` … `CursorPageDownSelect` (16 commands) |
| 985–1306 | `HomeCommand` (`cursorHome`, `cursorHomeSelect`), `LineStartCommand`, `EndCommand` (with `sticky`), `LineEndCommand`, `TopCommand`, `BottomCommand` (and their `…Select` variants) |
| 1775–1830 | `CancelSelection`, `RemoveSecondaryCursors` |
| 1917–1938 | `SetSelection` |
| `src/vs/editor/test/browser/controller/cursorMoveCommand.test.ts` | 16–510 | suites `Cursor move command test` (39 tests) and `Cursor move by blankline test` (8 tests), helpers 619–736 |

## Files to create or change

- `src/LoomEditor.View/Commands/CoreEditorCommand.cs` — create
- `src/LoomEditor.View/Commands/CoreNavigationCommands.cs` — create (`internal static partial class`; split by concern,
  e.g. `CoreNavigationCommands.Move.cs`, `.Home.cs`, `.Selection.cs`, to stay under ~400 lines each)
- `src/LoomEditor.View/Commands/CoreCommandRegistration.cs` — create: registers every core command id in a `CommandRegistry`
- `src/LoomEditor.View/Features/EditorContext.cs` — change: calls `CoreCommandRegistration.Register(Commands, ViewModel)`
- `tests/LoomEditor.View.Tests/Commands/CursorMoveCommandTests.cs` — create (the ported test file)

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `internal abstract class CoreEditorCommand<TArgs>` with `string Id { get; }` and
  `abstract void RunCoreEditorCommand(ViewModel viewModel, TArgs args)`. Argument records (all `internal sealed record`,
  each with `string? Source = null`): `BaseCommandArgs`, `MoveCommandArgs(Position Position, Position? ViewPosition = null, …)`,
  `CursorMoveCommandArgs(int? PageSize = null, …)`, `EndCommandArgs(bool Sticky = false, …)`,
  `SetSelectionCommandArgs(Selection Selection, …)`. `cursorMove` takes `CursorMoveArguments` (P3-25) plus `Source`.
- The command objects are `static readonly` fields of `CoreNavigationCommands` named like Monaco (`CursorLeft`,
  `CursorLeftSelect`, `CursorHome`, `CursorEnd`, `CursorTop`, `CursorBottom`, `CursorMove`, `MoveTo`, `MoveToSelect`,
  `CancelSelection`, `RemoveSecondaryCursors`, `SetSelection`…). Their `Id` is Monaco's id (`cursorLeft`, `_moveTo`…).
  They hold no state (safe as statics).
- `noHistory: true` uses the source `"api"` (Monaco's `TextEditorSelectionSource.PROGRAMMATIC`).
- `CoreCommandRegistration.Register(CommandRegistry registry, ViewModel viewModel)` registers, for each command **that
  has a keyboard use** (all of this card except `_moveTo`, `_moveToSelect`, `setSelection`, `cursorMove` which need
  arguments), an `EditorCommand(id, () => command.RunCoreEditorCommand(viewModel, new …Args()))`. It returns one
  `IDisposable` that unregisters them all. The `EditorContext` keeps it.
- **`editor.action.selectAll` is not registered here**: `SelectAllFeature` already owns that id (registering it twice
  throws). Its result is the same as Monaco's `selectAll` (one cursor selecting everything). Write this in a comment.
- `precondition` / `when` of the Monaco registrations are **not** ported here: keybindings and their `when` clauses
  are P3-39.
- Test port: the Monaco helpers `cursorEqual`, `moveTo`, `moveToEndOfLine`… (lines 619–736) become private helpers of
  the test class; `withTestCodeEditor(TEXT, {}, …)` → `TestCodeEditor.WithTestCodeEditor` (P3-20). The 9 viewport
  tests (lines 322–420) set `viewModel.getCompletelyVisibleViewRange = …`: use `CompletelyVisibleViewRangeOverride`
  (P3-25).

## Tests

- [ ] Port suite `Cursor move command test` (39 tests, lines 16–420)
- [ ] Port suite `Cursor move by blankline test` (8 tests, lines 422–510)
- [ ] **Not ported** (write the reason in the PORTING tests table): suite `Cursor move command - foldedLine unit`
      (8 tests): they need `setHiddenAreas` (folding, phase 5)
- [ ] New: `Commands.Execute("cursorDown")` on an `EditorContext` with 3 lines moves the primary cursor to line 2
- [ ] New: `Commands.Execute("removeSecondaryCursors")` with 3 cursors leaves the primary one

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `editor/browser/coreCommands.ts` (🔶 navigation commands, P3-26); phase 3 tests table
      row `cursorMoveCommand.test.ts` 47 / 55 (foldedLine suite: phase 5)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Column selection commands (P3-28), scroll commands (P3-38), mouse commands (P3-44), editing commands (P3-32 and later),
keybindings (P3-39).

## Suggested commit message

`feat(view): port the core navigation commands`
