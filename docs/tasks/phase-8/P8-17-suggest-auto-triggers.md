# P8-17 — Automatic triggers: quick suggest while typing and trigger characters

- **Status:** done
- **Depends on:** P8-16
- **Size:** M

## Goal

Typing a word opens the completion list by itself (after 10 ms, not inside comments or strings), and typing a character that a provider declared as a trigger character (for example `.`) asks that provider at once, like VS Code.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/suggest/browser/suggestModel.ts` | 221–314 (`_updateTriggerCharacters`, `checkTriggerCharacter`), 345–376 (the quick suggest branch of `_onCursorChange`), 387–456 (`_doTriggerQuickSuggest`, without the inline completions part), 316–320 (`cancel` cancels the quick suggest timer) | the two triggers |
| `src/vs/editor/common/config/editorOptions.ts` | search `quickSuggestions`, `quickSuggestionsDelay`, `suggestOnTriggerCharacters` (defaults: `{ other: 'on', comments: 'off', strings: 'off' }`, `10`, `true`) | the options |
| `src/vs/editor/contrib/suggest/test/browser/suggestModel.test.ts` | 332–350 (`trigger - on type`), 351–400 (`#17400`), 402–478 (`#21484: Trigger character always force a new completion session`), 479–528 (`#29353` 1/2 and 2/2), 646–689 (`Mac press and hold…`, skip: composition), 719–768, 769–789, 790–860, 929–985 (`registerCompletionItemProvider with letters as trigger characters block other completion items to show up #127815`), 986–1042 (`Unexpected suggest scoring #167242`), 1313–1339 (the quick suggestion option tests: only the `off` / `on` string shorthands that map to this card's options) | tests (see the list below; the test titles are authoritative) |

## Files to create or change

- `src/LoomEditor.View/Suggest/SuggestModel.AutoTrigger.cs` — create (`partial`)
- `src/LoomEditor.View/Suggest/SuggestModelOptions.cs` — change: new members below
- `src/LoomEditor.View/Suggest/QuickSuggestionsOptions.cs` — create
- `src/LoomEditor.View/Suggest/SuggestModel.cs` — change: `Cancel` cancels the quick suggest timer; `OnCursorChange` calls the real `TriggerQuickSuggestIfTyping`; the constructor and the provider-change handler call `UpdateTriggerCharacters`
- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — change: `internal event EventHandler<string>? DidType;` raised at the end of `Type(text, source)` (see decisions)
- `tests/LoomEditor.View.Tests/Suggest/SuggestModelAutoTriggerTests.cs` — create
- `tests/LoomEditor.View.Tests/Layout/ViewModelTypeEventTests.cs` — create (or change an existing `ViewModel` test file that fits)

## Design decisions (already made — do not change)

- **Options.** `SuggestModelOptions` gains `QuickSuggestionsOptions QuickSuggestions = QuickSuggestionsOptions.Default`, `TimeSpan QuickSuggestionsDelay = 10 ms`, `bool SuggestOnTriggerCharacters = true`. `sealed record QuickSuggestionsOptions(QuickSuggestionValue Other = On, QuickSuggestionValue Comments = Off, QuickSuggestionValue Strings = Off)`,
  `enum QuickSuggestionValue { On, Inline, Off }` (`Inline` behaves as `Off`: there is no ghost text; Monaco `offWhenInlineCompletions` is not ported); helpers `bool IsAllOff` and `QuickSuggestionValue ValueFor(StandardTokenType type)` (`Comment` → `Comments`, `String` → `Strings`, `RegEx` → `Other` like Monaco's `QuickSuggestionsOptions.valueFor`; every other type → `Other`; `StandardTokenType` is the existing `LoomEditor.Core.Tokens` enum).
- **`ViewModel.DidType`** (Monaco `onDidType`): `internal event EventHandler<string>? DidType;` raised **after** the edit by `Type(string text, string? source)` (once per call, with `text`), also when the typed text was swallowed by an auto-closing pair or overtype (Monaco fires for every typed text). `CompositionType`, `Paste`, `Cut`, commands: no event.
  The cursor event of the same keystroke comes first, then `DidType` (Monaco order: `viewModel.type` then `_onDidType.fire`).
- **Quick suggest** — `TriggerQuickSuggestIfTyping()` (the empty method of P8-16) becomes Monaco's `_doTriggerQuickSuggest`: `Cancel()` first (this cancels the timer and any session), then `_triggerQuickSuggest.CancelAndSet(…, options.QuickSuggestionsDelay)` with a `TimeoutTimer` of the existing `Features/Scheduling/` code on `context.Scheduler`; when it fires:
  `_triggerState is not null` → return; `!LineContext.ShouldAutoTrigger(model, position)` → return; the editor has no focus → return (Monaco `hasWidgetFocus`; use the existing focus flag of the view model: `ViewModel.SetHasFocus` exists, find the getter with `grep -n "HasFocus" src/LoomEditor.View/Layout/ViewModel.Cursors.cs`);
  `options.QuickSuggestions.IsAllOff` → return; if not "all on": `lineTokens = model.Tokenization.GetLineTokens(position.LineNumber)`, `tokenType = lineTokens.GetStandardTokenType(lineTokens.FindTokenIndexAtOffset(Math.Max(position.Column - 1 - 1, 0)))` (the token **before** the caret: find the exact members with `grep -n "FindTokenIndexAtOffset\|GetStandardTokenType" src/LoomEditor.Core/Tokens/LineTokens.cs`),
  `ValueFor(tokenType) is Off or Inline` → return; `!providers.Has(languageId)` → return; `Trigger(new SuggestTriggerOptions(Auto: true))`. (Not ported: inline completions waiting, `canShowQuickSuggest`, `tokenizeIfCheap`.)
  `Cancel()` additionally calls `_triggerQuickSuggest.Cancel()` first (Monaco line 317). `Dispose()` disposes the timer.
- **Trigger characters** — `UpdateTriggerCharacters()` (called by the constructor, and by the provider registry `Changed` handler **before** `UpdateActiveSuggestSession`, like Monaco): clears the previous `DidType` subscription; returns when `options.SuggestOnTriggerCharacters` is false, the editor is read-only (the same read-only source `ViewModel.Type` checks: `ViewModel.Cursors.cs` line ≈ 225 `_cursor.Context.CursorConfig.ReadOnly`;
  expose it as an internal property `ViewModel.IsReadOnly` if there is none) or there are no providers; builds `Dictionary<string, HashSet<ICompletionItemProvider>> supportsByTriggerCharacter` from `providers.All(languageId)` and each provider's `TriggerCharacters ?? []`; subscribes to `viewModel.DidType` with `CheckTriggerCharacter(text)`:
  `LineContext.ShouldAutoTrigger(model, position)` → return (quick suggest handles it: "don't trigger by trigger characters when this is a case for quick suggest"); `lastChar`: the last UTF-16 unit of `text`, or the last **two** units when they are a surrogate pair; `supports = map.GetValueOrDefault(lastChar)`; none → return;
  `providerItemsToReuse` = for each `(provider, items)` of `_completionModel?.GetItemsByProvider()` whose provider is **not** in `supports`; then `Trigger(new SuggestTriggerOptions(Auto: true, TriggerKind: TriggerCharacter, TriggerCharacter: lastChar, Retrigger: _completionModel is not null, ProviderFilter: supports, ProviderItemsToReuse: providerItemsToReuse))`.
  `DidType` text empty → return (Monaco's composition-end path is not ported).
- The `language changed` handler also calls `UpdateTriggerCharacters()` (Monaco lines 163–166).
- `<remarks>` unchanged. Header unchanged.

## Tests

- [x] New (`ViewModelTypeEventTests`): `Type_RaisesDidTypeOncePerCall` (with the text), `Type_AutoClosedPair_StillRaises`, `Paste_DoesNotRaise`, `DidType_RaisedAfterTheCursorEvent` (order)
- [x] Port (`SuggestModelAutoTriggerTests`, same conventions as P8-16; typing is `SuggestTestEditor.Type`, time is `Scheduler.AdvanceBy`; the harness sets `ViewModel.SetHasFocus(true)`): `trigger - on type` (332), `#17400: Keep filtering suggestModel.ts after space` (351, the full test now), `#21484: Trigger character always force a new completion session` (402),
      `Intellisense Completion doesn't respect space after equal sign (.html file), #29353 [1/2]` and `[2/2]` (479, 504), `Text changes for completion CodeAction are affected by the completion #39893` (719) **only if** it does not need code actions (else list it as not ported), `Completion unexpectedly triggers on second keypress of an edit group in a snippet #43523` (769) **not ported** (snippets),
      `Fails to render completion details #47988` (790, only the completion part), `registerCompletionItemProvider with letters as trigger characters block other completion items to show up #127815` (929), `Unexpected suggest scoring #167242` (986), `Mac press and hold accent character insertion does not update suggestions, #35269` **not ported** (composition).
      Any test of P8-16's "Moved to P8-17" note goes here. For each test not ported say why in the PORTING entry.
- [x] New: `QuickSuggest_FiresAfterTheDelay` (nothing at 9 ms, `DidTrigger` at 10 ms, `Auto`), `QuickSuggest_RestartsOnEachKeystroke` (typing twice within 10 ms triggers once), `QuickSuggest_NotInsideAComment` and `QuickSuggest_NotInsideAString` (a language with tokens: use `"csharp"` and wait for the tokenization like `TokenizationTests` helpers do, or a stub `ITokenizationSupport` through `TestEditorServices`; if tokens cannot be produced in a unit test, test `QuickSuggestionsOptions.ValueFor` directly and the callback through an internal seam `Func<Position, StandardTokenType> TokenTypeAt` you add to the model's constructor with the default reading the model),
      `QuickSuggest_NotOnANumber` (typing `1`), `QuickSuggest_NotWithoutProviderForTheLanguage`, `QuickSuggest_NotWithoutFocus`, `QuickSuggest_AllOff_Disabled`, `Backspace_DoesNotTriggerQuickSuggest` (deleting moves the cursor left: the condition `prev.ContainsRange(current) || prev.End <= current.Position` of P8-16 is false),
      `QuickSuggest_Inline_BehavesAsOff`, `QuickSuggest_Cancel_StopsTheTimer`, `TriggerCharacter_TriggersTheProviderAtOnce` (`.` after an identifier: `DidTrigger` with the trigger character in the provider's context, no delay), `TriggerCharacter_OtherProviderItemsReused` (a second provider without the character is not called again),
      `TriggerCharacter_InMiddleOfQuickSuggestCase_Ignored` (typing a letter is not a trigger character even if declared: `ShouldAutoTrigger` wins), `TriggerCharacter_SurrogatePairLastChar` (two units), `TriggerCharacter_ReadOnly_Ignored`, `TriggerCharacter_Disabled_Ignored` (`SuggestOnTriggerCharacters = false`), `TriggerCharacters_UpdatedWhenProvidersChange` (register a provider with `:` while idle, type `:`)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: the ported parts of `suggestModel.ts`, `ViewModel.DidType` (`codeEditorWidget.ts` `onDidType`), the ported tests with counts and a line for each test **not** ported with the reason
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Applying a chosen item (P8-19), `editor.suggest` kind filters, snippet-mode suppression, composition handling, `wordBasedSuggestions`.

## Suggested commit message

`feat(view): trigger completion on typing and on trigger characters`
