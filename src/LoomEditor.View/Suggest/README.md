# Suggest

The completion logic, without any UI.

- `SuggestItems.ProvideAsync` asks the registered `ICompletionItemProvider`s group by group (most specific first, the first
  group with results wins), fills the default range and sort text, and returns `SuggestItem`s sorted by `SuggestItemComparer`
  (sort text, label, kind; ordinal; stable).
- `SuggestItem` validates the edit ranges (`IsInvalid`), keeps the lower-cased texts and the scoring state, and resolves
  lazily (`BeginResolve` / `CompleteResolve`).
- Not ported: snippets, clipboard, durations, the kind filter (`editor.suggest.show*`), word based completions.
- `CompletionModel` filters and sorts the list (P8-15).
- `SuggestModel` is one completion session (P8-16): `Trigger` asks the providers and raises `DidSuggest`; typing, cursor moves, edits,
  language and provider changes refilter, retrigger or cancel it. Typing starts one (P8-17): quick suggest after
  `QuickSuggestionsDelay` (not inside comments or strings by default) and the trigger characters of the providers (`ViewModel.DidType`).
- `SuggestSelector` picks the initially selected item (Monaco `Memory.select`; no recency memory: `suggestSelection` is always `first`);
  `SuggestListState` is the pure state of the list: items, selection, a window of at most 12 rows (P8-18).
- `SuggestController` (P8-19) connects the model, the list state and an `ISuggestPresenter`, resolves the selected item and accepts an item
  (`Accept`: prefix replaced, sync additional edits, caret after the text, one undo step).
