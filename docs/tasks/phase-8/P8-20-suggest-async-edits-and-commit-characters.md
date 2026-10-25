# P8-20 — Suggest: additional edits that arrive after the accept, and commit characters

- **Status:** done
- **Depends on:** P8-19
- **Size:** M

## Goal

Two behaviors of Monaco's accept that language servers rely on: (1) an item whose `additionalTextEdits` (for example the `using` / `import` line) only arrive when the item is resolved gets them applied right after the accept, unless the user's typing made that unsafe;
(2) typing a commit character (for example `(` or `.`) while an item is selected accepts the item first, then types the character.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/suggest/browser/suggestController.ts` | 400–443 (the asynchronous additional edits branch of `_insertSuggestion`) | `SuggestController.Accept` async branch |
| `src/vs/editor/contrib/suggest/browser/suggestCommitCharacters.ts` | 11–77 | `CommitCharacterController` |
| `src/vs/editor/browser/widget/codeEditor/codeEditorWidget.ts` | search `onWillType` / `_onWillType` | when the event is raised (before the typed text is applied) |

## Files to create or change

- `src/LoomEditor.View/Suggest/SuggestController.Accept.cs` — change: the async branch; `Accept(SuggestItem item, bool noAfterUndoStop = false)`
- `src/LoomEditor.View/Suggest/CommitCharacterController.cs` — create
- `src/LoomEditor.View/Suggest/SuggestController.cs` — change: creates and disposes the commit character controller
- `src/LoomEditor.View/Suggest/SuggestModelOptions.cs` — change: `bool AcceptSuggestionOnCommitCharacter = true`
- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — change: `internal event EventHandler<string>? WillType;`
- `tests/LoomEditor.View.Tests/Suggest/SuggestControllerAsyncEditsTests.cs`, `CommitCharacterControllerTests.cs` — create; `tests/LoomEditor.View.Tests/Layout/ViewModelTypeEventTests.cs` — change

## Design decisions (already made — do not change)

- **`ViewModel.WillType`** (Monaco `onWillType`): raised by `Type(string text, string? source)` **before** the edit (and before `ExecuteCursorEdit` is entered, so a handler may run its own edit); same call as P8-17's `DidType`. `Paste`, `Cut`, `CompositionType` do not raise it.
- **`Accept(SuggestItem item, bool noAfterUndoStop = false)`**: P8-19's steps with one change: the final `model.PushStackElement()` (step 7) is skipped when `noAfterUndoStop` is true. The asynchronous branch runs **instead of nothing** when `item.Completion.AdditionalTextEdits` was null **and** `!item.IsResolved`
  (Monaco: `else if (!isResolved)`; an item with sync edits, or an already resolved item, never takes this branch). In step 5 of P8-19, `isResolved` and the presence of edits are read **before** anything runs.
- **The async branch** (after the main edit, `oldFlags = noAfterUndoStop`): create `cts = new CancellationTokenSource()`; `position = null` (a `Position?`: the earliest end of any later change); `didType = false`.
  1. subscribe to `model.ContentChanged`: `e.IsFlush` → `cts.Cancel()` and unsubscribe; else for every `change` in `e.Changes`: `thisPosition = change.Range.GetEndPosition()`; `if (position is null || thisPosition.IsBefore(position)) position = thisPosition` (the `Position.IsBefore` of Core).
  2. subscribe **once** to `viewModel.WillType`: unsubscribe, `didType = true`, and `if (!noAfterUndoStop) model.PushStackElement()` (Monaco pushes the undo stop that the accept skipped, before the user's typing).
  3. `task = item.BeginResolve(cts.Token)` (non-null here; if null, nothing to do and clean up); `UiContinuation.Then(task, dispatcher, resolved => { item.CompleteResolve(resolved); … }, onError: _ => cleanup, token: cts.Token)`. In the callback: `edits = item.Completion.AdditionalTextEdits`; if null or empty → cleanup, done;
     if `position is { } p && edits.Any(edit => p.IsBefore(edit.Range.GetStartPosition()))` → **skip** (the user typed before an edit's start: Monaco `return false`); else `if (didType) model.PushStackElement()`; `vm.ExecuteEdits("suggest.additionalTextEdits.async", edits.Select(e => new IdentifiedEditOperation(e.Range, e.Text, ForceMoveMarkers: true)).ToArray(), _ => null)`; `if (didType || !noAfterUndoStop) model.PushStackElement()`; announce `AccessibilityMessages.SuggestAdditionalEdits(item.TextLabel, edits.Count)`; cleanup.
  4. `cleanup`: unsubscribe both listeners, dispose `cts`. The `cts` is also cancelled by `SuggestController.Dispose`; keep a list of pending async accepts in the controller to cancel and clean them up.
  Edit ranges come from the server for the document **as it was when the item was built**; the check above is Monaco's only protection. Document it in the XML doc.
- **`CommitCharacterController`** (port of the class): `CommitCharacterController(ViewModel viewModel, SuggestController controller, bool enabled)`; it listens to `controller.Model.DidSuggest` (no items → `Reset()`), `Model.DidCancel` (`Reset()`), `controller.List.SelectionChanged` (`OnItem(item)`) and the list becoming empty / hidden (`Reset()`), and to `viewModel.WillType`.
  `OnItem(item)`: no item or `item.Completion.CommitCharacters` null / empty → `Reset()`; the same item as the active one → nothing; else `_active = (new HashSet<char>(first char of each non-empty commit character...), item)` (`CommitCharacters` is `IReadOnlyList<char>`: every char is a commit char).
  `OnWillType(text)`: when `_active` is set, the list is **not frozen** (`!controller.List.IsFrozen`), `controller.Model.State != Idle`, `text.Length > 0`, the **last** UTF-16 unit of `text` is in the set and `options.AcceptSuggestionOnCommitCharacter` → `controller.Accept(_active.Item, noAfterUndoStop: true)` (Monaco passes `InsertFlags.NoAfterUndoStop`); the typed character is then inserted by the normal typing code that continues after the event.
  `Reset()`: `_active = null`. Initial item: when the controller shows the list the selection event fires first (P8-19 `OnDidSuggest` calls `List.Reset`, which raises `SelectionChanged`), so no separate "onDidShow" is needed; but a `Reset` of the list that keeps the **same** item does not raise it, so `OnItem` is also called with `List.SelectedItem` right after every `DidSuggest` handled by the controller (call it from the controller's `OnDidSuggest` through an internal method `CommitCharacterController.Refresh()`).
- `<remarks>Threading: UI thread only.</remarks>`. Headers as in P8-19; `CommitCharacterController` has `// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestCommitCharacters.ts`.

## Tests

- [x] New (`ViewModelTypeEventTests`): `Type_RaisesWillTypeBeforeTheEdit` (the text is not in the model yet inside the handler), `Type_WillTypeHandlerCanEdit` (a handler that edits does not break the typing: the typed character still lands), `Paste_DoesNotRaiseWillType`
- [x] New (`SuggestControllerAsyncEditsTests`; a `CanResolve` provider whose `ResolveCompletionItemAsync` returns a pending task the test completes with additional edits):
      `AsyncEdits_AppliedAfterResolve` (the `using`-style edit at line 1 appears after `Flush()`), `AsyncEdits_ChangeBeforeEditStart_Skipped` (the user types at line 1 before the resolve completes: not applied), `AsyncEdits_TypingAfterTheEdit_Applied` (the user types further down: applied), `AsyncEdits_Flush_Cancelled` (`Model.SetValue(...)` while pending),
      `AsyncEdits_NoEdits_NothingApplied`, `AsyncEdits_ResolveFaulted_NothingApplied_NoLeak` (listeners removed: later typing does not throw), `AsyncEdits_UndoStops` (typing before the resolve completes: two undo steps — accept; then the edits after the typing — assert by undoing twice), `AsyncEdits_NotUsedWhenAlreadyResolved`, `AsyncEdits_NotUsedWhenSyncEditsExist`,
      `AsyncEdits_Dispose_CancelsPending`, `Accept_NoAfterUndoStop_DoesNotPushTheFinalStop` (assert through the undo behavior: accept then type `x` and undo once: both are undone together when `noAfterUndoStop` is true)
- [x] New (`CommitCharacterControllerTests`; `SuggestTestEditor` + a provider returning items with `CommitCharacters = ['(', '.']`): `TypeCommitCharacter_AcceptsThenTypesIt` (`"fo"` + `(` → `"foo("`, one undo step for the accept + character), `TypeOtherCharacter_DoesNotAccept`, `NoCommitCharacters_NoAccept`, `Frozen_DoesNotAccept`, `SessionIdle_DoesNotAccept`,
      `Disabled_NoAccept` (`AcceptSuggestionOnCommitCharacter = false`), `SelectionChange_UsesTheNewItemsCharacters`, `DidCancel_Resets`, `SurrogateLastUnit_Ignored` (typed text ending with a low surrogate is not a commit char), `Dispose_Unsubscribes`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: the async branch of `_insertSuggestion`, `suggestCommitCharacters.ts`, `ViewModel.WillType` (`onWillType`)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Commands and keybindings (P8-21), `acceptSuggestionOnEnter: smart`, the WinUI list.

## Suggested commit message

`feat(view): apply late additional edits and accept on commit characters`
