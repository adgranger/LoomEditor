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
  language and provider changes refilter, retrigger or cancel it. Typing does not start a session yet (quick suggest and trigger
  characters are P8-17).
