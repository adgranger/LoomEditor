# P8-16 — `SuggestModel`: trigger, cancel, refilter and retrigger (the state machine)

- **Status:** todo
- **Depends on:** P8-15
- **Size:** M
- **Recommended model:** Opus (an asynchronous state machine with many races: cancellation, retriggers while a request is in flight, stale results. Monaco's behavior is the spec but its tests run on a full editor. On failure — a ported scenario that cannot be made to pass, or a hang in a test — stop, do not commit, report the scenario, the state sequence you observed and what you tried)

## Goal

`SuggestModel` owns one completion session: `Trigger` asks the providers for items, builds a `CompletionModel` and raises `DidSuggest`; typing, moving the cursor, editing the text and changing providers update, retrigger or cancel the session exactly as Monaco's `suggestModel.ts` does.
The automatic triggers (typing a word, trigger characters) are P8-17; this card is the session itself, driven by `Trigger` calls and by edits.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/suggest/browser/suggestModel.ts` | 38–63 (event and options types), 65–102 (`LineContext`), 104–109 (`State`), 134–211 (constructor and listeners, **only** the ones listed in the decisions), 316–333 (`cancel`, `clear`), 335–343 (`_updateActiveSuggestSession`), 345–376 (`_onCursorChange`, without the quick suggest part), 525–533 (`_refilterCompletionItems`), 535–656 (`trigger`), 719–844 (`_onNewContext`) | the model |
| `src/vs/editor/contrib/suggest/test/browser/suggestModel.test.ts` | 89–170 (`Context - shouldAutoTrigger`), the oracle scenarios listed in Tests | tests |

## Files to create or change

- `src/LoomEditor.View/Suggest/SuggestModel.cs`, `SuggestModel.Context.cs` (`LineContext`), `SuggestTriggerOptions.cs`, `SuggestState.cs`, `SuggestEvents.cs` — create
- `src/LoomEditor.View/Suggest/README.md` — change
- `tests/LoomEditor.View.Tests/Suggest/SuggestModelTests.cs`, `SuggestModelContextTests.cs` — create
- `tests/LoomEditor.View.Tests/Suggest/TestHelpers/SuggestTestEditor.cs` — create (README §6)

## Design decisions (already made — do not change)

- **Types.** `enum SuggestState { Idle, Manual, Auto }`. `sealed record SuggestTriggerOptions(bool Auto = false, bool Shy = false, bool Refilter = false, bool Retrigger = false, CompletionTriggerKind? TriggerKind = null, string? TriggerCharacter = null, IReadOnlySet<ICompletionItemProvider>? ProviderFilter = null, IReadOnlyDictionary<ICompletionItemProvider, IReadOnlyList<SuggestItem>>? ProviderItemsToReuse = null)`
  (Monaco `SuggestTriggerOptions` + `completionOptions`; `clipboardText` dropped). Events (plain `EventHandler<T>`, UI thread): `DidCancel` (`SuggestCancelEventArgs(bool Retrigger)`), `DidTrigger` (`SuggestTriggerEventArgs(bool Auto, bool Shy, Position Position)`), `DidSuggest` (`SuggestEventArgs(CompletionModel CompletionModel, SuggestTriggerOptions TriggerOptions, bool IsFrozen)`).
- **`LineContext`** (the class of `suggestModel.ts`, **not** `CompletionLineContext`): `LineContext(TextModel model, Position position, SuggestTriggerOptions options)` computes `LineNumber`, `Column`, `LeadingLineContent = model.GetLineContent(line)[..(column - 1)]`, `LeadingWord = model.GetWordUntilPosition(position)` (the model member of Core, `WordAtPosition`), `TriggerOptions`.
  `static bool ShouldAutoTrigger(TextModel model, Position position)` (Monaco `shouldAutoTrigger`, minus `tokenizeIfCheap`): `word = model.GetWordAtPosition(position)`; null → false; `word.EndColumn != position.Column && word.StartColumn + 1 != position.Column` → false (the cursor is in the middle of a word and not right after its first character);
  a word that is a number (`!double.IsNaN`-style: Monaco `!isNaN(Number(word.word))`; in C# `double.TryParse(word.Word, NumberStyles.Float, CultureInfo.InvariantCulture, out _)` is true → return false); else true.
- **`SuggestModel(IEditorContext context, SuggestModelOptions options)`** (`internal sealed class : IDisposable`, UI thread only). It gets everything through the extensions of P8-05 and the existing ones: `context.Model`, `context.Selection` (the primary cursor position is `Selection.GetPosition()`), `GetViewModel()` (for the cursor events), `GetLanguageFeatures()?.CompletionProviders`, `GetUiDispatcher()`, `GetLanguageId()`, `context.Scheduler`.
  If the context is not an `EditorContext` (no view model / language features) the model never triggers. Fields as Monaco: `SuggestTriggerOptions? _triggerState`, `CancellationTokenSource? _requestToken`, `LineContext? _context`, `Selection _currentSelection`, `CompletionModel? _completionModel`.
- **Listeners** (all unsubscribed in `Dispose`, which also calls `Cancel()`): `viewModel.EventDispatcher.OnEvent` for a `CursorStateChangedEvent` → `OnCursorChange(e)` (the existing event that `EditorContext.OnViewModelEvent` already reads; `e.Source`, `e.Reason`, `e.Selections`);
  `context.Model.ContentChanged` → `if (_triggerState is not null) RefilterCompletionItems()`; the model's language change event (find it: `grep -n "LanguageChanged" src/LoomEditor.Core/Model/*.cs`) → `Cancel()`; `CompletionProviders.Changed` → `UpdateActiveSuggestSession()`.
  Not ported: `onDidChangeModel` (the model never changes), composition start/end (`editorIsComposing`: LoomEditor has no such flag; documented as a behavior difference: IME composition can refilter), configuration changes, `_updateTriggerCharacters` (P8-17).
  **Order of the two events of one keystroke**: Monaco gets `onDidChangeModelContent` before `onDidChangeCursorSelection`; LoomEditor's view model raises the outgoing cursor event after the model event too (the model event is raised while applying edits, the cursor event when the cursor controller finishes): the tests of this card type a character with `ViewModel.Type` and rely on this order; if it is the reverse, **stop and report** (do not reorder events in other code).
- **`State`**: `_triggerState is null` → `Idle`; `!Auto` → `Manual`; else `Auto`.
- **`Cancel(bool retrigger = false)`** — `_triggerQuickSuggest` does not exist yet (P8-17): in this card the method does: if `_triggerState is not null`: cancel and null `_requestToken`, `_triggerState = null`, `_completionModel = null`, `_context = null`, raise `DidCancel(retrigger)`. (P8-17 adds the quick suggest timer cancel at the top, the card says so.)
- **`UpdateActiveSuggestSession`**: when `_triggerState` is not null: if there is no provider for the language (`!providers.Has(languageId)`) → `Cancel()`, else `Trigger(new SuggestTriggerOptions(Auto: _triggerState.Auto, Retrigger: true))`.
- **`OnCursorChange(CursorStateChangedEvent e)`** (Monaco `_onCursorChange`): `prev = _currentSelection; _currentSelection = e.Selections[0]` (the primary); early `Cancel(); return;` when `!_currentSelection.IsEmpty || (e.Reason is not NotSet and not Explicit) || (e.Source is not "keyboard" and not "deleteLeft")`;
  then: `_triggerState is null && e.Reason == NotSet` → if `prev.ContainsRange(_currentSelection) || prev.GetEndPosition().IsBeforeOrEqual(_currentSelection.GetPosition())` call `TriggerQuickSuggestIfTyping()` — **an empty private method in this card** with the comment `// P8-17: DoTriggerQuickSuggest`;
  `else if (_triggerState is not null && e.Reason == Explicit)` → `RefilterCompletionItems()`. (`Selection.ContainsRange`, `GetEndPosition`, `IsBeforeOrEqual` exist in Core; use the existing names.)
- **`RefilterCompletionItems()`**: `ctx = new LineContext(model, position, _triggerState with { Refilter = true })`; `OnNewContext(ctx)`.
- **`Trigger(SuggestTriggerOptions options)`** — port of lines 535–656, minus clipboard, snippets sort order, word distance, telemetry, `_waitForInlineCompletions`, `canShowQuickSuggest`:
  1. `position = context.Selection.GetPosition()`; `ctx = new LineContext(model, position, options)`; `Cancel(options.Retrigger)`; `_triggerState = options`; raise `DidTrigger(options.Auto, options.Shy, position)`; `_context = ctx`.
  2. `CompletionContext suggestCtx = options.TriggerCharacter is { } c ? new(TriggerCharacter, c) : new(options.TriggerKind ?? Invoke)`.
  3. `_requestToken = new CancellationTokenSource()`; `task = SuggestItems.ProvideAsync(providers, model, languageId, position, new SuggestItemOptions(ShowDeprecated: true, ProviderFilter: options.ProviderFilter ?? empty, ProviderItemsToReuse: options.ProviderItemsToReuse ?? empty), suggestCtx, _requestToken.Token)`.
  4. `UiContinuation.Then(task, dispatcher, token, items => …)` where the callback: `if (_triggerState is null) return;` (cancelled or cancelled-and-restarted: the `token` check of `Then` covers the cancelled token; this check covers `Cancel()` calls that did not cancel this token); `ctx2 = new LineContext(model, current position, options)`;
     `fuzzy = new FuzzyScoreOptions(FirstMatchCanBeWeak: !modelOptions.MatchOnWordStartOnly, BoostFullMatch: true)`; `_completionModel = new CompletionModel(items, _context!.Column, new CompletionLineContext(ctx2.LeadingLineContent, ctx2.Column - _context.Column), modelOptions, fuzzy)`; `OnNewContext(ctx2)`.
     `onError` does nothing (Monaco `onUnexpectedError`: logged) but also calls `Cancel()` so the model is not left "triggered" without a request. A **cancelled** task (the `ProvideAsync` task is cancelled when the token is) never reaches the callbacks.
- **`OnNewContext(LineContext ctx)`** — a line-by-line port of lines 719–844 (read it: the order of the early returns is the behavior): no `_context` → return; other line → `Cancel()`; leading whitespace of the line start differs (`GetLeadingWhitespace(ctx.LeadingLineContent) != GetLeadingWhitespace(_context.LeadingLineContent)`) → `Cancel()`;
  `ctx.Column < _context.Column` (moved left): `ctx.LeadingWord.Word` non-empty → `Trigger(new(Auto: _context.TriggerOptions.Auto, Retrigger: true))` else `Cancel()`; return; no `_completionModel` → return; started a new word (`ctx.LeadingWord.Word.Length != 0 && ctx.LeadingWord.StartColumn > _context.LeadingWord.StartColumn`):
  `if (LineContext.ShouldAutoTrigger(model, position) && _context is not null)` → `Trigger(new(Auto: _context.TriggerOptions.Auto, Retrigger: true, ProviderItemsToReuse: _completionModel.GetItemsByProvider()))`; return;
  `ctx.Column > _context.Column && _completionModel.GetIncompleteProvider().Count > 0 && ctx.LeadingWord.Word.Length != 0` → retrigger with `TriggerKind = TriggerForIncompleteCompletions`, `Retrigger = true`, `ProviderFilter` = the incomplete providers, `ProviderItemsToReuse` = the items of the others (Monaco lines 783–800);
  else update: `old = _completionModel.LineContext`; `isFrozen = false`; `_completionModel.LineContext = new(ctx.LeadingLineContent, ctx.Column - _context.Column)`; if `_completionModel.Items.Count == 0`: `shouldAutoTrigger = ShouldAutoTrigger(…)`; if `_context is null` → `Cancel(); return`; if `shouldAutoTrigger && _context.LeadingWord.EndColumn < ctx.LeadingWord.StartColumn` → `Trigger(new(Auto: _context.TriggerOptions.Auto, Retrigger: true)); return`;
  if `!_context.TriggerOptions.Auto` (manual): restore `_completionModel.LineContext = old`, `isFrozen = _completionModel.Items.Count > 0`; `isFrozen && ctx.LeadingWord.Word.Length == 0` → `Cancel(); return`; else (auto, nothing left) → `Cancel(); return`. Finally raise `DidSuggest(_completionModel, ctx.TriggerOptions, isFrozen)`.
  `GetLeadingWhitespace(string)`: the leading run of spaces and tabs (Monaco `strings.getLeadingWhitespace`).
  **Re-entrancy**: `OnNewContext` can call `Trigger`/`Cancel`, which raise events whose handlers may call back; keep the same sequencing as Monaco (no extra guards), and re-read `_context` after any call that may cancel (Monaco does so at the lines noted).
- `<remarks>Threading: UI thread only; the provider tasks complete anywhere and come back through the dispatcher (ADR 0011 decision 4).</remarks>`. Header: `// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestModel.ts`.
- **Test harness** `SuggestTestEditor` (`internal sealed class : IDisposable`): `Context` (`EditorContext` created with the constructor that takes the scheduler **and** the dispatcher: look at the internal constructors; create one if only the 4-parameter one exists, passing the `ManualUiDispatcher` as the 3rd argument), `Scheduler` (`ManualEditorScheduler`), `Dispatcher` (`ManualUiDispatcher`), `Model`, `Providers` (the context's `LanguageFeatures.CompletionProviders`);
  `SetText(string text, string languageId = "plaintext")`, `MoveCaret(int line, int column)` (`SetSelection` through the context with reason `Explicit`), `Type(string text)` (`context.GetViewModel()!.Type(text, "keyboard")` for each character, one call per character), `Backspace()` (`ViewModel` command `deleteLeft` through `context.Commands.Execute("deleteLeft")`),
  `Flush()` = `Dispatcher.RunAll()` then `Scheduler.AdvanceBy(TimeSpan.Zero)` repeated until nothing is pending (guard with a loop limit of 100).
  `SuggestModelRecorder` (same file): subscribes to the three events and keeps `List<string> Log` with lines like `"trigger auto=false"`, `"suggest items=3 frozen=false"`, `"cancel retrigger=false"`.

## Tests

- [ ] Port (`SuggestModelContextTests`; `// Monaco: 'Context - shouldAutoTrigger'`): `Context - shouldAutoTrigger` (the assertions of lines 161–171 on a `"bar\nfoo\n"`-style model; port with `LineContext.ShouldAutoTrigger(model, new Position(l, c))`); `shouldAutoTrigger at embedded language boundaries` is **not** ported (needs embedded languages).
- [ ] Port (`SuggestModelTests`; each test keeps its Monaco name in a comment and its **sequence of events** as the assertion: Monaco's `assertEvent(model.onDidTrigger, …)` helpers become checks on `SuggestModelRecorder.Log`; the editor is a `SuggestTestEditor`; Monaco's `registerCompletionItemProvider` is `Providers.Register(languageId, provider)` with a `FakeCompletionProvider` returning the same items):
      `events - cancel/trigger` (277), `events - suggest/empty` (310), `#17400: Keep filtering suggestModel.ts after space` (351, **only the parts that do not rely on quick suggest**: replace `editor.trigger('keyboard','type')` + automatic trigger by an explicit `Trigger(new SuggestTriggerOptions(Auto: true))` before typing), `Incomplete suggestion results cause re-triggering when typing w/o further context, #28400 (1/2)` (529) and `(2/2)` (567),
      `Trigger character is provided in suggest context` (611, via an explicit `Trigger` with `TriggerCharacter`), `Backspace should not always cancel code completion, #36491` (690), `Trigger (full) completions when (incomplete) completions are already active #99504` (861), `Completion list closes unexpectedly when typing a digit after a word separator #169390` (1043), `Set refilter-flag, keep triggerKind` (1116).
      Read each Monaco test; where it needs automatic triggering (typing starts the session), call `Trigger` explicitly at the point where Monaco's typing would have started it and keep the rest; **write in the comment of the test which line you replaced**. A Monaco test that cannot be expressed without quick suggest is moved to P8-17 (say so in this card's "Moved to P8-17" note and in P8-17's list).
- [ ] New (`SuggestModelTests`): `Trigger_RaisesTriggerThenSuggest` (a provider with 3 items), `Trigger_NoProviders_SuggestWithNoItems_ThenCancelForAuto` (auto trigger with an empty result cancels; manual keeps nothing: assert what Monaco's `_onNewContext` does for an empty initial model: `Items.Count == 0`, `Auto` → `Cancel`),
      `Trigger_StaleResult_AfterCancel_Dropped` (`Pending()` provider, `Cancel()`, complete the provider, `Flush()`: no `DidSuggest`), `Trigger_SecondTriggerCancelsFirstRequest` (the first provider call's token is cancelled), `Trigger_ProviderThrows_Cancels`, `State_FollowsTheSession` (`Idle` → `Manual` → `Auto` → `Idle`),
      `TypeMoreCharacters_Refilters_WithoutNewRequest` (one provider call for three typed characters), `CursorLeft_OnWord_Retriggers`, `CursorLeft_OffWord_Cancels`, `NewLine_Cancels`, `SelectionNonEmpty_Cancels`, `MouseMove_SourceMouse_Cancels` (a cursor event with source `"mouse"` cancels),
      `ContentChanged_WhileIdle_DoesNothing`, `LanguageChanged_Cancels`, `ProviderUnregistered_Cancels` and `ProviderRegistered_WhileActive_Retriggers`, `Dispose_CancelsAndUnsubscribes`, `LeadingWhitespaceChange_Cancels` (outdent while active), `ManualSession_FreezesWhenNothingMatches` (typing a non-matching character keeps the old list: `isFrozen`), `ManualSession_FrozenThenNotOnWord_Cancels`,
      `IncompleteProvider_TypingRetriggers_OnlyThatProvider` (the other provider's items are reused: it is not called again), `NewWord_RetriggersReusingItems`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green, and run `dotnet test --project tests/LoomEditor.View.Tests -c Release` **five times in a row** to show the new asynchronous tests are not flaky; report the result
- [ ] `docs/PORTING.md` map: `suggestModel.ts` (what is ported, what is not: quick suggest and trigger characters → P8-17; composition, inline completions, clipboard, telemetry, snippets, `onDidChangeModel`) and the ported tests with counts
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Quick suggest on typing and trigger characters (P8-17), applying an item (P8-19), the widget (P8-22), `suggestOnTriggerCharacters` / `quickSuggestions` options (P8-17).

## Suggested commit message

`feat(view): port the suggest model session logic`
