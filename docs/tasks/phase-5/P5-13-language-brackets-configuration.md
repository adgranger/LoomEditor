# P5-13 — `LanguageBracketsConfiguration`

- **Status:** todo
- **Depends on:** P5-12
- **Size:** S

## Goal

Core has `LanguageBracketsConfiguration`: the opening and closing bracket kinds of a language, which ones close
which, which are colorized, and a regex that finds all of them. The bracket pair tree (step C) and the indentation
line processor (P5-16) use it.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/languages/supports/languageBracketsConfiguration.ts` | 15–105 | `LanguageBracketsConfiguration`, `filterValidBrackets` |
| same | 107–168 | `BracketKind`, `BracketKindBase`, `OpeningBracketKind`, `ClosingBracketKind` |

## Files to create or change

- `src/LoomEditor.Core/Languages/Supports/LanguageBracketsConfiguration.cs` — create
- `src/LoomEditor.Core/Languages/Supports/BracketKind.cs` — create (`BracketKind`, `OpeningBracketKind`, `ClosingBracketKind`)
- `tests/LoomEditor.Core.Tests/Languages/Supports/LanguageBracketsConfigurationTests.cs` — create

## Design decisions (already made — do not change)

- `LanguageBracketsConfiguration(string languageId, LanguageConfiguration config)`; members `LanguageId`,
  `OpeningBrackets`, `ClosingBrackets` (`IReadOnlyList<…>`, in first-seen order), `GetOpeningBracketInfo(string)`,
  `GetClosingBracketInfo(string)` (→ `null` when missing), `GetBracketInfo(string)` → `BracketKind?`,
  `GetBracketRegExp()` → `BracketsUtils.CreateBracketOrRegExp(opening keys, then closing keys)` (P5-11; Monaco's
  `options` parameter is dropped, see P5-11).
- `CachedFunction` and the two `Map`s → `OrderedDictionary<string, …>` (insertion order is observable through
  `OpeningBrackets` / `ClosingBrackets` and the regex).
- `BracketKind` is an **abstract class** (Monaco's `BracketKindBase` and its union type `BracketKind`) with
  `BracketText`, `LanguageId`, `abstract bool IsOpeningBracket`, and an internal `Config` reference.
  `OpeningBracketKind.OpenedBrackets`, `ClosingBracketKind.OpeningBrackets` are `IReadOnlyList<…>` built as sets
  (add only if not already present, by reference): Monaco iterates its `Set`s in insertion order.
  `Closes(OpeningBracketKind other)` and `ClosesColorized(…)` return `false` when `other.Config` is another
  configuration (reference comparison), then `Contains`. `GetOpeningBrackets()` returns the list.
- Colorized pairs: `config.ColorizedBracketPairs` if not null (even empty), otherwise the brackets without `<`…`>`
  (Monaco issue #132476). Pairs with an empty open or close are dropped (`filterValidBrackets`).

## Tests

No upstream unit tests (the bracket pair tests of step C exercise it). New:

- [ ] `ColorizedPairs_DefaultToTheBracketsWithoutAngleBrackets`
- [ ] `ColorizedPairs_EmptyList_ColorizesNothing`
- [ ] `ClosingBracket_ClosesOnlyItsOpeningBrackets`
- [ ] `Closes_BracketOfAnotherConfiguration_IsFalse`
- [ ] `EmptyOpenOrClose_IsIgnored`
- [ ] `GetBracketRegExp_MatchesEveryBracketIgnoringCase` (`begin`/`END`)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (`languageBracketsConfiguration.ts`)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Customized brackets from the editor settings (`editor.language.brackets`, not ported, P5-14), the bracket pair
tree (step C).

## Suggested commit message

`feat(core): port the language brackets configuration`
