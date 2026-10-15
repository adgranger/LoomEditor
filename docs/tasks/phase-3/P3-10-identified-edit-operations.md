# P3-10 — Identified edit operations and `ResultingSelection`

- **Status:** todo
- **Depends on:** P3-09
- **Size:** S

## Goal

The cursor edits the model with Monaco's richer edit operations: each one can carry an **identifier** (which cursor
and which command produced it), `forceMoveMarkers` and `isAutoWhitespaceEdit`, and the cursor state computer
receives the inverse operations **with their identifiers**. Undo and redo also tell the view which selections to
restore through `ResultingSelection` on the raw change event. Today `TextModel.PushEditOperations` only takes
`TextEdit` and drops all of this.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

If a function is not exactly at the given line, search for its name.

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model.ts` | 503–534 | `ISingleEditOperationIdentifier`, `IIdentifiedSingleEditOperation` (`identifier`, `isAutoWhitespaceEdit`, `_isTracked`, `forceMoveMarkers` from `ISingleEditOperation`) |
| same | 536–564 | `IValidEditOperation`, `ICursorStateComputer` |
| `src/vs/editor/common/model/textModel.ts` | 1295–1338 | `_validateEditOperation`, `_validateEditOperations` (keep identifier, forceMoveMarkers, isAutoWhitespaceEdit, _isTracked) |
| same | 1340–1349 | `pushEditOperations` (public entry point) |
| same | 1440–1480 | `_applyUndo`, `_applyRedo`, `_applyUndoRedoEdits` (the `resultingSelection` parameter) |
| same | 467–481 | `_emitContentChangedEvent` (sets `rawContentChangedEvent.resultingSelection` **before** notifying the view models) |
| `src/vs/editor/common/textModelEvents.ts` | search `class ModelRawContentChangedEvent` | `resultingSelection: Selection[] \| null` |
| `src/vs/editor/common/model/editStack.ts` | search `pushEditOperation` | the computer receives the inverse operations returned by `applyEdits(…, true)` |

## Files to create or change

- `src/LoomEditor.Core/Model/Edits/IdentifiedEditOperation.cs` — create: `internal readonly record struct IdentifiedEditOperation`
- `src/LoomEditor.Core/Model/TextModel.Undo.cs` — change: new internal `PushEditOperations` overload; undo/redo pass the selections to the event
- `src/LoomEditor.Core/Model/TextModel.Edits.cs` — change: validation keeps the annotations; raw events can carry `ResultingSelection`
- `src/LoomEditor.Core/Undo/EditStack.cs` — change: the computer gets `IReadOnlyList<ValidEditOperation>`
- `src/LoomEditor.Core/Model/Events/ModelRawContentChangedEventArgs.cs` — change: `ResultingSelection` property
- `src/LoomEditor.Core/PublicAPI.Unshipped.txt` — update (`ResultingSelection` is public)
- `tests/LoomEditor.Core.Tests/Model/TextModelIdentifiedEditTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `internal readonly record struct IdentifiedEditOperation(Range Range, string? Text, EditOperationIdentifier? Identifier = null, bool ForceMoveMarkers = false, bool IsAutoWhitespaceEdit = false, bool IsTracked = false)`.
  `EditOperationIdentifier` and `ValidEditOperation` already exist in `LoomEditor.Core/Model/Edits/` (phase 1): reuse them.
- New overload, used by the cursor:
  `internal IReadOnlyList<Selection>? PushEditOperations(IReadOnlyList<Selection>? beforeCursorState, IReadOnlyList<IdentifiedEditOperation> edits, Func<IReadOnlyList<ValidEditOperation>, IReadOnlyList<Selection>?>? cursorStateComputer)`.
  The existing public overload (with `TextEdit`) stays and becomes a thin wrapper: it converts each `TextEdit` to an
  `IdentifiedEditOperation` with no identifier, and its computer receives `TextEdit`s built from the
  `ValidEditOperation`s (as today). Public behavior does not change.
- Validation keeps `Identifier`, `ForceMoveMarkers`, `IsAutoWhitespaceEdit` and `IsTracked` in the
  `ValidAnnotatedEditOperation` (today they are lost: `TextModel.Edits.cs` `ValidateEditOperation`).
  `ForceMoveMarkers` reaches the interval tree through `AcceptReplace` (P3-08) and must now be the real value.
- `ValidEditOperation.Identifier` of the inverse operations is the identifier of the forward operation (Monaco keeps
  it so that the cursor can find which inverse edit belongs to which command).
- `public IReadOnlyList<Selection>? ResultingSelection { get; }` on `ModelRawContentChangedEventArgs` (internal
  setter or constructor parameter). It is set **before** the event is raised, as in Monaco's `_emitContentChangedEvent`.
  `Undo()` passes the `BeforeCursorState` of the undone step, `Redo()` passes the `AfterCursorState`; all other edits
  pass `null`. `Undo()` and `Redo()` keep returning the selections too (no public change).
- **No deferred `ContentChanged` emission.** In Monaco the view models are notified immediately (`_onDidChangeContentOrInjectedText`);
  only the public `onDidChangeContent` is deferred. The C# `RawContentChanged`/`ContentChanged` pair stays as it is.
  Write this in the XML doc of `PushEditOperations` (one sentence) so nobody "fixes" it later.

## Tests

- [ ] New: `PushEditOperations` with two operations whose identifiers are `(0,0)` and `(1,0)` → the computer receives
      two inverse operations with the same identifiers, in the order of the edits
- [ ] New: `IsAutoWhitespaceEdit` and `ForceMoveMarkers` survive validation (check through a tracked range: an insert
      at the end of a `NeverGrowsWhenTypingAtEdges` range with `ForceMoveMarkers = true` moves the end)
- [ ] New: `Undo()` raises `RawContentChanged` with `ResultingSelection` equal to the `beforeCursorState` given to
      `PushEditOperations`; `Redo()` with the selections returned by the computer; a plain `ApplyEdits` has `null`
- [ ] Existing phase 1 tests of `PushEditOperations`, `Undo`, `Redo` pass unchanged

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `PublicAPI.Unshipped.txt` of `LoomEditor.Core`: only `ModelRawContentChangedEventArgs.ResultingSelection` added
- [ ] `docs/PORTING.md`: update the `TextModel.Undo` behavior-difference row (undo/redo now also set `ResultingSelection`)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Trimming auto whitespace (P3-11), edit sources (`TextModelEditSource`), undo groups.

## Suggested commit message

`feat(core): accept identified edit operations and report the resulting selection`
