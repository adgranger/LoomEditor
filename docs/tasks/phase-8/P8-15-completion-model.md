# P8-15 — `CompletionModel`: the filtered and sorted list

- **Status:** todo
- **Depends on:** P8-14
- **Size:** M

## Goal

Given the items of a completion request and what the user has typed since, `CompletionModel` gives the list to show: items that match the typed word (fuzzy), best first, re-filtered incrementally when the user types more and from scratch when they type less.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/suggest/browser/completionModel.ts` | 14–283 | `CompletionModel`, its `LineContext` (renamed `CompletionLineContext`), `Refilter`, `_createCachedState`, the three comparers |
| `src/vs/editor/contrib/suggest/test/browser/completionModel.test.ts` | 13–336 | the test suite (`createSuggestItem` at 13–33, the 14 tests) |
| `src/vs/editor/contrib/suggest/browser/wordDistance.ts` | 17–19 | `WordDistance.None` (distance 0 always) |

## Files to create or change

- `src/LoomEditor.View/Suggest/CompletionModel.cs`, `CompletionLineContext.cs`, `SuggestModelOptions.cs` (holds the model's options, see below), `SnippetSuggestions.cs` — create
- `tests/LoomEditor.View.Tests/Suggest/CompletionModelTests.cs` — create; `tests/LoomEditor.View.Tests/Suggest/TestHelpers/SuggestItems.cs` — create (`CreateSuggestItem(...)`, the port of Monaco's `createSuggestItem`)

## Design decisions (already made — do not change)

- **`SuggestModelOptions`** (`sealed record`, the subset of Monaco's `InternalSuggestOptions` that these cards need): `bool FilterGraceful = true` (VS Code default), `bool MatchOnWordStartOnly = true` (default), `SnippetSuggestions Snippets = SnippetSuggestions.Inline`; `static Default`.
  `enum SnippetSuggestions { Top, Bottom, Inline }` (Monaco also has `none`: snippets do not exist in phase 8; the three orders are ported because the Monaco tests check them and `Snippet` is a kind). Not ported: `insertMode` (always `insert`), `localityBonus`, `shareSuggestSelections`, the `show*` kind switches, `showIcons`, `snippetsPreventQuickSuggestions`, `selectionMode`.
- **`CompletionLineContext(string LeadingLineContent, int CharacterCountDelta)`** (`readonly record struct`, Monaco's `LineContext` of this file).
- **`CompletionModel`**: constructor `CompletionModel(IReadOnlyList<SuggestItem> items, int column, CompletionLineContext lineContext, SuggestModelOptions options, FuzzyScoreOptions? fuzzyScoreOptions = null)`
  (`wordDistance` is dropped: `WordDistance.None`, so `item.Distance = 0`; `clipboardText` is dropped: no snippets; `fuzzyScoreOptions ?? FuzzyScoreOptions.Default`, with `FirstMatchCanBeWeak = !options.MatchOnWordStartOnly` applied by the **caller** (P8-16), not here).
  Members: `CompletionLineContext LineContext { get; set; }` (the setter ports Monaco's: when `LeadingLineContent` or `CharacterCountDelta` changed, `_refilterKind = old.CharacterCountDelta < new.CharacterCountDelta && _filteredItems is not null ? Refilter.Incr : Refilter.All`, then store),
  `IReadOnlyList<SuggestItem> Items` (calls `_ensureCachedState`; the **same list instance** is returned until the context changes: a test checks reference equality), `IReadOnlyDictionary<ICompletionItemProvider, IReadOnlyList<SuggestItem>> GetItemsByProvider()`, `ISet<ICompletionItemProvider> GetIncompleteProvider()`
  (providers whose first item's `Container.Incomplete` is true). `stats` (`pLabelLen`) is not ported (it only sizes the widget).
- **`_createCachedState`** is ported line by line: `itemsByProvider` rebuilt; `source = _refilterKind == All ? _items : _filteredItems`; the scorer: `(!options.FilterGraceful || source.Count > 2000) ? Fuzzy.Score : Fuzzy.ScoreGracefulAggressive` (`FuzzyScorer` delegate of P8-13: wrap `Fuzzy.Score` in a lambda with the delegate's signature);
  per item: skip `IsInvalid`; add to `itemsByProvider`; `overwriteBefore = item.Position.Column - item.EditStart.Column`; `wordLen = overwriteBefore + characterCountDelta - (item.Position.Column - _column)`; the word cache (`word`, `wordLow`) updates when `word.Length != wordLen` with `leadingLineContent.Substring(leadingLineContent.Length - wordLen)` (`wordLen == 0` → empty; JavaScript's `slice(-n)` with `n > length` returns the whole string: clamp `n` to the length);
  `item.Word = word`; `wordLen == 0` → `item.Score = FuzzyScore.Default`; else skip leading spaces / tabs of the word up to `overwriteBefore` (`wordPos`), `wordPos >= wordLen` → `FuzzyScore.Default`; else if `Completion.FilterText` is a non-empty string (Monaco `typeof filterText === 'string'`; a C# `null` or `""`... **careful**: Monaco treats `''` as a string, but `SuggestItem.FilterTextLow` is null for empty (P8-14): use `item.FilterTextLow is not null` as the test),
  `match = scorer(word, wordLow, wordPos, filterText, filterTextLow, 0, options)`; no match → skip the item; if `string.Equals(filterText, textLabel, StringComparison.OrdinalIgnoreCase)` (Monaco `compareIgnoreCase(...) === 0`) → `item.Score = match`, else `item.Score = Fuzzy.AnyScore(word, wordLow, wordPos, textLabel, labelLow, 0)` and `item.Score[0] = match[0]` (**`AnyScore` returns a new array**: mutate it freely);
  else score against the label (`item.Score = match`, no match → skip). Then `item.Idx = i`, `item.Distance = 0`, add to the target. Finally `_filteredItems = target` **stable-sorted** with the snippet comparer, `_refilterKind = Nothing`.
- **Comparers** (Monaco `_compareCompletionItems`, `…SnippetsUp`, `…SnippetsDown`): by `Score[0]` descending, then `Distance` ascending, then `Idx` ascending; the snippet variants first compare kinds when they differ (`Snippet` first for `Top`, last for `Bottom`). `Inline` = the plain one. Use `OrderBy`-style stable sort (Monaco's `sort` is stable) — **not** `List<T>.Sort`.
- **The three `#25495` tests compare scores** (`assert.ok(a.score < b.score)`). In JavaScript `<` on two arrays compares their **string forms**, which is an accident; the intent is spelled out in the test comments, so port the assertion on the first element, `Score[0]`:
  `top`: the promoted snippet `a` has a **lower** raw score than `b` (`a.Score[0] < b.Score[0]`); `bottom`: the non-snippet `a` has a lower raw score than the demoted snippet `b` (`a.Score[0] < b.Score[0]`); `inline`: the snippet `a` has a higher raw score than `b` (`a.Score[0] > b.Score[0]`).
  If one of these fails, the port of the scorer or the comparer is wrong: stop and report the values, never change the direction of the assertion.
- `<remarks>Threading: UI thread only.</remarks>`. Header: `// Ported from microsoft/vscode: src/vs/editor/contrib/suggest/browser/completionModel.ts`.

## Tests

- [ ] Port (`CompletionModelTests`; `CreateSuggestItem(label, overwriteBefore, kind = Property, incomplete = false, position = (1, 1), sortText = null, filterText = null)` is the port of `createSuggestItem` (the item's range is `[position.Column - overwriteBefore, position.Column]` on the position's line, `InsertText = label`, a container with `Incomplete = incomplete`, a `FakeCompletionProvider`);
      each test keeps its name in a comment `// Monaco: 'filtering - cached'`; Monaco's `defaultOptions` / `EditorOptions.suggest.defaultValue` become `SuggestModelOptions.Default`, `'top' | 'bottom' | 'inline'` become `SnippetSuggestions`; `WordDistance.None` disappears): `filtering - cached`, `complete/incomplete`, `Fuzzy matching of snippets stopped working with inline snippet suggestions #49895`,
      `proper current word when length=0, #16380`, `keep snippet sorting with prefix: top, #25495`, `keep snippet sorting with prefix: bottom, #25495`, `keep snippet sorting with prefix: inline, #25495`, `filterText seems ignored in autocompletion, #26874`, `Vscode 1.12 no longer obeys 'sortText' in completion items (from language server), #26096`
      (`items.Sort` with `SuggestItemComparer.Default`, a stable `OrderBy`), `Completion item sorting broken when using label details #153026` (plain labels; the label-details object of Monaco is `LabelDetail` here: also set `LabelDetail = "x"` on one item to keep the intent), `Score only filtered items when typing more, score all when typing less`,
      `Have more relaxed suggest matching algorithm #15419`, `Emmet suggestion not appearing at the top of the list in jsx files, #39518`
- [ ] New: `LineContext_Unchanged_KeepsCache`, `LineContext_Longer_RefiltersOnlyFilteredItems` (count how many times the scorer is called through item `Idx`/`Word` side effects: after narrowing from 5 to 3 items, typing more scores at most 3 items — assert via a scorer counter by constructing the model through an internal constructor overload that takes a `FuzzyScorer`; add that overload (`internal CompletionModel(…, FuzzyScorer scorer)`)),
      `LineContext_Shorter_RefiltersAll`, `InvalidItem_Skipped`, `GetItemsByProvider_GroupsByProvider`, `GetIncompleteProvider_OnlyIncomplete`, `FilterText_UsedForMatching_LabelForHighlights` (item with `filterText = "Map"`, label `Map - java.util`, typed `M`: match on filter text, `Score.Length > 2` from the label's `AnyScore`), `GracefulFilter_Off_NoTypoMatch` (`FilterGraceful = false`: `rlut` does not match `result`),
      `OverLongSource_UsesPlainScorer` (more than 2000 items: use the plain scorer; check with the scorer-counting overload that the graceful one is not used — pass a counting scorer for each kind through the overload: the choice is made by a `static` method `ChooseScorer(options, count)` that you test directly), `WordLongerThanLine_DoesNotThrow` (`wordLen` larger than the line)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green (no ported assertion weakened)
- [ ] `docs/PORTING.md` map: `completionModel.ts` (not ported: `stats`, `WordDistance`, clipboard, `snippetSuggestions: none`) and the 13 ported tests with counts
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The trigger / cancel logic (P8-16), the list selection state (P8-18), word distance, `suggest.localityBonus`.

## Suggested commit message

`feat(view): port the completion model`
