# P8-19 — `SuggestController`: show the list, move in it, accept an item

- **Status:** todo
- **Depends on:** P8-18
- **Size:** M

## Goal

A controller connects the suggest model (P8-16/17), the list state (P8-18) and a presenter: it opens the list when the model suggests, closes it when the session ends or the editor loses focus, moves the selection, resolves the selected item, and **accepts** an item: the typed prefix is replaced by the item's text, additional edits are applied, the caret lands after the text, all in one undo step.
Async additional edits and commit characters are P8-20; commands, keys and context keys are P8-21; the WinUI list is P8-22.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/suggest/browser/suggestController.ts` | 237–296 (`onDidTrigger`, `onDidSuggest`, `onDidCancel`, blur), 308–400 (`_insertSuggestion`: undo stops, overwrite info, sync additional edits, the main insertion), 573–596 (`getOverwriteInfo`, `_alertCompletionItem`), 598–607 (`triggerSuggest`) | `SuggestController` |
| `src/vs/editor/contrib/suggest/browser/suggestWidget.ts` | the focus handler `_onListFocus` that resolves the focused item (search `resolve(`) | resolving on selection |

## Files to create or change

- `src/LoomEditor.View/Suggest/ISuggestPresenter.cs`, `NullSuggestPresenter.cs`, `SuggestController.cs`, `SuggestController.Accept.cs` — create
- `src/LoomEditor.View/Suggest/SuggestListState.cs` — change: `public void NotifyItemUpdated()` (raises `Changed`)
- `src/LoomEditor.View/Features/EditorContext.cs` — change: `internal ISuggestPresenter SuggestPresenter { get; set; } = NullSuggestPresenter.Instance;`; `EditorContextExtensions.cs` — change: `GetSuggestPresenter`
- `src/LoomEditor.View/Accessibility/AccessibilityMessages.cs` — change: `SuggestAdditionalEdits(string label, int count)`
- `tests/LoomEditor.View.Tests/Suggest/SuggestControllerTests.cs`, `SuggestControllerAcceptTests.cs` — create; `tests/LoomEditor.View.Tests/Suggest/TestHelpers/FakeSuggestPresenter.cs` — create; `tests/LoomEditor.View.Tests/Accessibility/AccessibilityMessagesTests.cs` — change

## Design decisions (already made — do not change)

- **`ISuggestPresenter`** (`internal interface`): `bool IsVisible { get; }`, `void Show(SuggestListState state, Position anchor)` (the position of the typed word start: `item.EditStart` of the selected item, or the cursor; the widget places itself under / over it), `void Hide()`.
  The presenter reads the state and subscribes to its `Changed` itself; `Show` is called once per `DidSuggest` that has items (again for each new `DidSuggest`: the presenter must accept being shown while visible). `NullSuggestPresenter`: never visible.
- **`SuggestController(IEditorContext context, SuggestModelOptions options) : IDisposable`** owns `public SuggestModel Model`, `public SuggestListState List`, and the resolve `CancellationTokenSource`. It reads the presenter with `context.GetSuggestPresenter()` **at use time**. Like the hover controller it does nothing when the context is not an `EditorContext`.
  Events subscribed (unsubscribed in `Dispose`, which also disposes the model): `Model.DidSuggest`, `Model.DidCancel`, `Model.DidTrigger` (nothing to do: Monaco's `showTriggered` draws a "loading" state: not ported), `List.SelectionChanged`, and the view model's `FocusChanged` (see below).
- **`OnDidSuggest(e)`**: `e.TriggerOptions.Shy` → return; `items = e.CompletionModel.Items`; `index = SuggestSelector.SelectInitialIndex(items)`; `Model.State == Idle` → return (Monaco: "selecting an item can pump out selection/cursor change events which can cancel suggest halfway");
  `List.Reset(items, index, e.IsFrozen)`; if `items.Count == 0` → `presenter.Hide()` else `presenter.Show(List, anchor)` with `anchor = List.SelectedItem!.EditStart`. (Monaco's `selectionMode`, `noFocus`, selectors with memories: not ported: the item is always selected.)
- **`OnDidCancel(e)`**: `!e.Retrigger` → `List.Clear()`, `presenter.Hide()`, cancel the resolve token (`e.Retrigger` keeps the list on screen while the retrigger runs: Monaco). **Blur**: when the view model reports focus lost (`ViewModel.FocusChanged` with the focus flag false: use the existing getter; see P8-17 for `HasFocus`) → `Model.Cancel()`.
  (The completion list never takes the keyboard focus, so the editor losing focus always means the user left.)
- **Selection and resolving**: `List.SelectionChanged` → cancel the previous resolve token, create a new one; for the new item (non-null): `Task<CompletionItem?>? task = item.BeginResolve(token)`; if not null `UiContinuation.Then(task, dispatcher, resolved => { item.CompleteResolve(resolved); List.NotifyItemUpdated(); }, token: token)`
  (the widget re-draws the selected row's detail and documentation). Faulted → nothing. (Monaco resolves on focus, `_onListFocus`.)
- **Commands of the controller** (the feature of P8-21 calls them; each is a no-op when the list is not visible unless said): `void Trigger()` → `Model.Trigger(new SuggestTriggerOptions(Auto: false))` (Monaco `triggerSuggest`; with an open list it retriggers); `void Cancel()` → `Model.Cancel()`; `bool IsVisible => presenter.IsVisible && !List.IsEmpty`;
  `bool SelectNext()`, `SelectPrevious()`, `SelectNextPage()`, `SelectPreviousPage()`, `SelectFirst()`, `SelectLast()` (each calls the `List` method and returns its result); `bool AcceptSelected()` (below); `bool Accept(SuggestItem item)` (below).
- **`Accept(SuggestItem item)` → `bool`** — port of `_insertSuggestion` for a plain-text item (steps in this order; any early `return false` leaves the document untouched):
  1. `vm = context.GetViewModel()`; `model = context.Model`; `cursor = context.Selection.GetPosition()`; if `cursor.LineNumber != item.Position.LineNumber` (the session should have been cancelled; be defensive) → `Model.Cancel(); return false`. A read-only editor (`ViewModel.ExecuteEdits` already ignores edits and raises `ReadOnlyEditAttempt`): `Model.Cancel(); return false`.
  2. `model.PushStackElement()` (Monaco: undo stop before).
  3. **Overwrite info** (port of `getOverwriteInfo` with `insertMode = insert`): `columnDelta = cursor.Column - item.Position.Column`; `overwriteBefore = item.Position.Column - item.EditStart.Column + columnDelta`; `overwriteAfter = item.EditInsertEnd.Column - item.Position.Column` (`LineSuffix` handling of typed-over suffix characters is not ported).
  4. `Model.Cancel()` (stops the session before the edits so that they do not refilter or retrigger; Monaco does it before the sync additional edits and again before the insertion).
  5. **Sync additional edits**: `edits = item.Completion.AdditionalTextEdits` non-null and non-empty → each `TextEdit` → `new IdentifiedEditOperation(range, text, ForceMoveMarkers: true)` (Monaco `EditOperation.replaceMove`; the record is `internal` in `LoomEditor.Core.Model.Edits`); an edit that starts on `item.Position.LineNumber` after `item.Position.Column` is shifted right by `columnDelta` (start column + `columnDelta`; end column + `columnDelta` unless the range spans several lines: Monaco lines 358–366);
     `vm.ExecuteEdits("suggest.additionalTextEdits", ops, _ => null)`. Announce `AccessibilityMessages.SuggestAdditionalEdits(item.TextLabel, edits.Count)` through `context.GetAnnouncer()`.
  6. **Main edit**: re-read the cursor (`cursor2 = context.Selection.GetPosition()`: the additional edits may have moved it); `startColumn = Math.Max(1, cursor2.Column - overwriteBefore)`; `endColumn = Math.Min(model.GetLineMaxColumn(cursor2.LineNumber), cursor2.Column + overwriteAfter)`;
     `range = new Range(cursor2.LineNumber, startColumn, cursor2.LineNumber, endColumn)`; `vm.ExecuteEdits("suggest", [new IdentifiedEditOperation(range, item.Completion.InsertText)], undoEdits => [Selection.FromPositions(undoEdits[0].Range.GetEndPosition())])` (a collapsed selection after the inserted text; `ValidEditOperation.Range` of an undo edit is the range the inserted text now occupies: check with the first test).
     Only the **primary** cursor is edited; other cursors stay where they are (behavior difference: Monaco's snippet insertion edits every cursor; document it).
  7. `model.PushStackElement()` (undo stop after). Return `true`. The inserted text is **not re-indented**: `insertText` with line breaks is inserted as is (Monaco's `adjustWhitespace` belongs to snippets; behavior difference).
  Not ported: `item.completion.command`, memory (`memorize`), `OvertypingCapturer`, `SnippetController2`, telemetry, `_onWillInsertSuggestItem`, the alternatives (`SuggestAlternatives`).
- **`AcceptSelected()`**: `List.SelectedItem is { } item` → `Accept(item)`, else `false`. (Monaco's `acceptSelectedSuggestion(keepAlternativeSuggestions, alternativeOverwriteConfig)` flags are not ported.)
- `<remarks>Threading: UI thread only.</remarks>` on both. Header: `// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestController.ts (…)`.
- `AccessibilityMessages.SuggestAdditionalEdits(label, count)` = `"Accepting '{label}' made {count} additional edits"` (Monaco `aria.alert.snippet`; count with `CultureInfo.InvariantCulture`).

## Tests

- [ ] New (`SuggestControllerTests`; `SuggestTestEditor` of P8-16 + `FakeSuggestPresenter` (records `Show(anchor)` / `Hide()` calls, settable `IsVisible` follows them) assigned to `context.SuggestPresenter`; providers are `FakeCompletionProvider`s): `Suggest_ShowsTheList_AndSelectsTheFirst`, `Suggest_Preselect_SelectsThatItem`, `Suggest_NoItems_HidesPresenter`, `Suggest_Shy_DoesNotShow`,
      `Cancel_HidesPresenter_ClearsList`, `Retrigger_KeepsThePresenterVisible` (a `DidCancel(retrigger: true)` does not hide), `Blur_Cancels` (`viewModel.SetHasFocus(false)`), `Trigger_Manual_StartsAManualSession`, `Select_MovesReturnTheListResult` (`SelectNext` etc.), `SelectionChange_ResolvesTheItem` (`CanResolve` provider: after `Flush()` the item `IsResolved` and the presenter's list state got `Changed`),
      `SelectionChange_CancelsThePreviousResolve` (the first resolve token is cancelled), `ResolveFaulted_Ignored`, `Dispose_UnsubscribesAndCancels`, `NonEditorContext_DoesNothing`, `IsVisible_FollowsPresenterAndList`
- [ ] New (`SuggestControllerAcceptTests`; text `"let value = fo"` etc.): `Accept_ReplacesTheTypedPrefix` (typed `fo`, item `foo` with range `[11,13]`: result `"let value = foo"`, caret at the end), `Accept_InsertsAtEmptyWord` (no prefix), `Accept_UsesCurrentCursor_WhenTypedAfterRequest` (the user typed one more character after the request: `columnDelta` extends the overwritten range),
      `Accept_OverwriteAfter_UsesTheInsertEnd` (a range whose insert end is after the cursor replaces the suffix), `Accept_InsertText_DiffersFromLabel`, `Accept_AdditionalEdits_AppliedBeforeCursorShift` (an edit inserting a `using` line at line 1: the main text lands at the shifted position, caret after it),
      `Accept_AdditionalEdit_OnSameLineAfterCursor_IsShifted` (columnDelta shifting), `Accept_IsOneUndoStep` (one `Undo()` restores the text before the accept, additional edits included: find the undo API with `grep -n "public .* Undo" src/LoomEditor.Core/Model/TextModel.Undo.cs`), `Accept_AnnouncesAdditionalEdits` (`RecordingAnnouncer`), `Accept_CancelsTheSession` (`State` is `Idle`, presenter hidden),
      `Accept_WrongLine_ReturnsFalse_NoEdit`, `Accept_ReadOnly_ReturnsFalse`, `Accept_OnlyPrimaryCursor` (two cursors: only the primary gets text), `AcceptSelected_NoSelection_False`, `Accept_MultiLineInsertText_NotReindented`
- [ ] New (`AccessibilityMessagesTests`): `SuggestAdditionalEdits_Text`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `suggestController.ts` (the parts above; not ported: memory, alternatives, overtyping capturer, snippets, item commands, `selectionMode`, multi-cursor insertion, `LineSuffix`, insert / replace toggle) with the behavior differences listed in the decisions
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Async additional edits, commit characters (P8-20); `editor.action.*` commands, key bindings, context keys (P8-21); the WinUI list (P8-22).

## Suggested commit message

`feat(view): add the suggest controller (list, selection, accept)`
