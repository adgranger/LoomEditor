# Suggest

The completion logic, without any UI.

- `SuggestItems.ProvideAsync` asks the registered `ICompletionItemProvider`s group by group (most specific first, the first
  group with results wins), fills the default range and sort text, and returns `SuggestItem`s sorted by `SuggestItemComparer`
  (sort text, label, kind; ordinal; stable).
- `SuggestItem` validates the edit ranges (`IsInvalid`), keeps the lower-cased texts and the scoring state, and resolves
  lazily (`BeginResolve` / `CompleteResolve`).
- Not ported: snippets, clipboard, durations, the kind filter (`editor.suggest.show*`), word based completions.
- The model that filters the list is P8-15.
