# P8-14 — Suggest items: `SuggestItem` and `SuggestItems.ProvideAsync`

- **Status:** done
- **Depends on:** P8-13
- **Size:** M

## Goal

The editor can ask all completion providers registered for the language for their items at a position and gets one sorted list of `SuggestItem`s: each item knows its edit ranges (validated), its lower-cased label / sort text / filter text, and can be resolved (documentation, details) on demand.
This is the input of the completion model (P8-15).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/suggest/browser/suggest.ts` | 56–167 (`CompletionItem` class: ranges, validation, `resolve`), 185–200 (`CompletionOptions`, reduced), 247–335 (`provideSuggestionItems`), 338–398 (`defaultComparator`) | `SuggestItem`, `SuggestItemOptions`, `SuggestItems.ProvideAsync` |
| `src/vs/editor/contrib/suggest/test/browser/suggest.test.ts` | 59–119 (the `only from` test), 121–165 (`Ctrl+space completions stopped working with the latest Insiders, #97650`) | tests (the `sort - snippet *` tests need snippets: not ported) |

## Files to create or change

All in `src/LoomEditor.View/Suggest/` (namespace `LoomEditor.View.Suggest`, `internal`): `SuggestItem.cs`, `SuggestItemOptions.cs`, `SuggestItems.cs`, `SuggestItemComparer.cs`, `README.md`.

- `tests/LoomEditor.View.Tests/Suggest/SuggestItemTests.cs`, `SuggestItemsTests.cs` — create (`tests/LoomEditor.View.Tests/TestHelpers/TestProviders.cs` already exists since P8-09)

## Design decisions (already made — do not change)

- **`SuggestItem`** (`sealed class`; Monaco `CompletionItem` of `suggest.ts`, renamed so it does not clash with `LoomEditor.Core.Languages.Features.CompletionItem`):
  constructor `SuggestItem(Position position, CompletionItem completion, CompletionList container, ICompletionItemProvider provider)`; read-only members `Position`, `Completion`, `Container`, `Provider`, `EditStart`, `EditInsertEnd`, `EditReplaceEnd` (`Position`s), `TextLabel` (`completion.Label`), `LabelLow` (`ToLowerInvariant()`),
  `SortTextLow`, `FilterTextLow` (null when the text is null **or empty**: Monaco `completion.sortText && …`), `IsInvalid`; mutable scoring state (Monaco's public fields): `int[] Score = FuzzyScore.Default`, `int Distance`, `int Idx`, `string? Word`.
  `Completion.Range` null is **not** allowed here (the caller fills the default range first: see `ProvideAsync`); a null `Range` is an `ArgumentException`.
  Ranges (port of the constructor): with `completion.Range` = `CompletionRange(insert, replace)`: `EditStart = (insert.StartLineNumber, insert.StartColumn)`, `EditInsertEnd = (insert.EndLineNumber, insert.EndColumn)`, `EditReplaceEnd = (replace.EndLineNumber, replace.EndColumn)`.
  `IsInvalid = string.IsNullOrEmpty(textLabel) || insert spans several lines || replace spans several lines || insert.StartLineNumber != position.LineNumber || replace.StartLineNumber != position.LineNumber || insert.StartColumn != replace.StartColumn` (for a single range, insert = replace so the last test is false).
- **Resolving** (Monaco `resolve`, adapted: the merge into the shared item must happen on the UI thread): `bool IsResolved` (true when the provider cannot resolve — `!provider.CanResolve` — or after `CompleteResolve`);
  `Task<CompletionItem?>? BeginResolve(CancellationToken token)`: returns `null` when `IsResolved`; otherwise starts `provider.ResolveCompletionItemAsync(Completion, token)` **once** (the pending task is cached and returned again to a second caller; if `token` is cancelled before the task finished, the cache is cleared and `IsResolved` stays false so a later call restarts it: Monaco's `onCancellationRequested` reset;
  a task that faults or is cancelled clears the cache too); `void CompleteResolve(CompletionItem? resolved)`: UI thread; `if (resolved is not null) Completion.MergeFrom(resolved)`, `IsResolved = true`, drops the cache. Callers use `UiContinuation.Then(item.BeginResolve(token)!, dispatcher, item.CompleteResolve, token: token)`.
- **`SuggestItemOptions`** (`sealed record`, Monaco `CompletionOptions` reduced): `bool ShowDeprecated = true`, `IReadOnlySet<ICompletionItemProvider> ProviderFilter = empty`, `IReadOnlyDictionary<ICompletionItemProvider, IReadOnlyList<SuggestItem>> ProviderItemsToReuse = empty`; `static Default`. Not ported: `snippetSortOrder`, `kindFilter` (the `editor.suggest.show*` options), the snippet provider.
- **`SuggestItems.ProvideAsync`**: `public static Task<IReadOnlyList<SuggestItem>> ProvideAsync(LanguageFeatureRegistry<ICompletionItemProvider> registry, TextModel model, string languageId, Position position, SuggestItemOptions options, CompletionContext context, CancellationToken token)`.
  Called on the UI thread; it reads the model **synchronously, before the first provider call** (`position = model.ValidatePosition(position)`; `word = model.GetWordAtPosition(position)`; `defaultReplace = word is null ? Range.FromPositions(position) : new Range(line, word.StartColumn, line, word.EndColumn)`; `defaultRange = new CompletionRange(Insert: defaultReplace with end = position, Replace: defaultReplace)`) and never touches the model afterwards.
  Algorithm (port of the function, minus snippets, clipboard, durations and disposables): `groups = registry.OrderedGroups(languageId)`; for each group in order: for each provider of the group: if `options.ProviderItemsToReuse` has it, add those items (`didAddResult |= count > 0`); else skip it when `options.ProviderFilter.Count > 0 && !ProviderFilter.Contains(provider)`;
  else call `provider.ProvideCompletionItemsAsync(model, position, context, token)` (a **synchronous throw** counts as no result); wait for **all** providers of the group, then: a faulted task is ignored (Monaco `onUnexpectedExternalError`); for each returned list, each `suggestion`: skip when `!options.ShowDeprecated` and it has the `Deprecated` tag;
  `suggestion.Range ??= defaultRange`; `suggestion.SortText = string.IsNullOrEmpty(SortText) ? Label : SortText` (Monaco: "fill in default sortText when missing"); add `new SuggestItem(position, suggestion, list, provider)`; `didAddResult = true`. If the group added any result, or the token is cancelled, **stop** (later groups are not asked); otherwise go on with the next group.
  When the token is cancelled the returned task is **canceled** (`TrySetCanceled(token)`). The final list is sorted with `SuggestItemComparer.Default` (stable: `List<T>.Sort` is not stable; use `OrderBy(x => x, comparer)` or sort an array of `(item, index)`; Monaco's `Array.prototype.sort` is stable).
  No `await`: build it with a `TaskCompletionSource<IReadOnlyList<SuggestItem>>` (`TaskCreationOptions.RunContinuationsAsynchronously`) and `ContinueWith` per group (`TaskScheduler.Default`); the code that creates items runs on a pool thread after the providers completed, which is safe because it only reads the provider's own result objects and the immutable `position` / `defaultRange`.
- **`SuggestItemComparer`** (Monaco `defaultComparator`): `IComparer<SuggestItem>`; `a.SortTextLow` and `b.SortTextLow` both non-null and different → `string.CompareOrdinal` of them; else `string.CompareOrdinal(a.TextLabel, b.TextLabel)` if different; else `a.Completion.Kind - b.Completion.Kind` (the enum values). **Ordinal** comparison everywhere (JavaScript compares UTF-16 code units).
- `<remarks>Threading: SuggestItem: UI thread only. ProvideAsync: called on the UI thread, the task completes on any thread.</remarks>`. Header: `// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggest.ts (…)`.

## Tests

- [x] Port (`SuggestItemsTests`, test names in a comment; Monaco's `registry.register({ pattern, scheme }, foo)` becomes `registry.Register("plaintext", foo)`; the model text is `"FOO\nbar\nBAR\nfoo"`; `FakeCompletionProvider` of `TestHelpers`): `only from` (provider filter: only the given provider is asked and its item comes back), `Ctrl+space completions stopped working with the latest Insiders, #97650`
      (two items with `Insert (0,0,0,0)`... Monaco's ranges use line 0; use valid ones: line 1, insert `[1,1,1,1]`, replace `[1,1,1,10]`, and for the second replace `[1,2,1,10]`: the first is valid, the second invalid because `insert.StartColumn != replace.StartColumn`)
- [x] New (`SuggestItemTests`): `Constructor_Computes_Positions_AndLowerCases`, `Constructor_SingleRange_InsertEqualsReplace`, `IsInvalid_EmptyLabel`, `IsInvalid_MultiLineRange`, `IsInvalid_RangeOnAnotherLine`, `IsInvalid_Valid_False`, `Constructor_NullRange_Throws`, `SortTextLow_Empty_IsNull`,
      `BeginResolve_ProviderCannotResolve_Null_AndIsResolved`, `BeginResolve_StartsOnce_ReturnsSameTask`, `BeginResolve_Cancelled_Restartable`, `BeginResolve_Faulted_Restartable`, `CompleteResolve_MergesAndMarksResolved`, `CompleteResolve_Null_MarksResolved`
- [x] New (`SuggestItemsTests`): `ProvideAsync_DefaultRange_IsTheWordAtPosition` (word `foo` at column 2 of `foo`: replace = the word, insert = start..position), `ProvideAsync_DefaultRange_NoWord_EmptyAtPosition`, `ProvideAsync_FillsSortText`, `ProvideAsync_SortsBySortTextThenLabelThenKind`,
      `ProvideAsync_StopsAtFirstGroupWithResults` (a `Property` provider newer than a `*` provider: the `*` one is not called), `ProvideAsync_EmptyGroup_GoesOnToTheNext`, `ProvideAsync_SameGroupProviders_BothAsked`, `ProvideAsync_FaultedProvider_OthersKept`, `ProvideAsync_ThrowingProvider_OthersKept`,
      `ProvideAsync_NullList_Ignored`, `ProvideAsync_Deprecated_Skipped_WhenNotShown`, `ProvideAsync_ReusedItems_NotRequested` (provider in `ProviderItemsToReuse`: not called, its items are returned), `ProvideAsync_Cancelled_TaskIsCanceled`, `ProvideAsync_PositionIsValidated` (a column beyond the line is clamped),
      `ProvideAsync_PassesContext` (trigger kind and character reach the provider), `ProvideAsync_NoProviders_EmptyList`, `ProvideAsync_StableSort` (equal items keep provider order)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: `suggest.ts` (the parts above; not ported: snippets, clipboard, durations, kind filter, `_executeCompletionItemProvider`) and the two ported tests (the `sort - snippet *` ones listed as not ported: snippets)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Filtering and scoring (P8-15), the trigger logic (P8-16, P8-17), applying an item (P8-19), word based completions.

## Suggested commit message

`feat(view): add suggest items and the provider fan-out`
