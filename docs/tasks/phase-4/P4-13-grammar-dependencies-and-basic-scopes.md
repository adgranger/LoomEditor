# P4-13 — Grammar dependencies and basic scope attributes

- **Status:** done
- **Depends on:** P4-12
- **Size:** M

## Goal

Two helpers of the grammar exist: the processor that finds which external grammars a grammar needs (so the registry
can load them), and the provider that maps a scope name to its language id and standard token type (embedded
languages, `tokenTypes`).

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/grammar/grammarDependencies.ts` | 10–238 | `AbsoluteRuleReference`, `TopLevelRuleReference`, `TopLevelRepositoryRuleReference`, `ExternalReferenceCollector`, `ScopeDependencyProcessor` (`processQueue`), `collectReferencesOfReference`, `Context`, `ContextWithRepository`, `collectExternalReferencesInTopLevelRepositoryRule`, `…InTopLevelRule`, `…InRules` |
| textmate: `src/grammar/basicScopesAttributeProvider.ts` | 10–111 | `BasicScopeAttributes`, `BasicScopeAttributesProvider` (`getDefaultAttributes`, `getBasicScopeAttributes`, `_scopeToLanguage`, `_toStandardTokenType`), `ScopeMatcher<T>` |

## Files to create or change

- `src/LoomEditor.TextMate/Grammar/GrammarDependencies.cs` (+ `GrammarDependencies.Collect.cs`) — create
- `src/LoomEditor.TextMate/Grammar/BasicScopeAttributesProvider.cs`, `ScopeMatcher.cs` — create
- `tests/LoomEditor.TextMate.Tests/Grammar/GrammarDependenciesTests.cs`, `BasicScopeAttributesProviderTests.cs` — create

## Design decisions (already made — do not change)

- `ScopeDependencyProcessor` takes an `IGrammarRepository`-like lookup (`Func<string, RawGrammar?>`), not the async
  registry: ADR 0008 decision 3.
- `seenFullScopeRequests` / `seenPartialScopeRequests` are `HashSet<string>`; `Q` is a `List`, processed in order
  (trap 3).
- `_toStandardTokenType` uses upstream's regex (`\b(comment|string|regex|meta\.embedded)\b`) as a `[GeneratedRegex]`.
- `ScopeMatcher` builds its regex from the escaped scope names (`escapeRegExpCharacters`) like upstream, as a runtime
  `Regex` created once per provider (this is not a hot path: results are cached in `CachedFn`), with
  `RegexOptions.CultureInvariant`.

## Tests

No upstream unit tests. New:

- [x] `ScopeDependencyProcessor_CollectsExternalScopesAndRepositoryRules` (grammar A includes `source.b#rule` and `source.c`)
- [x] `ScopeDependencyProcessor_SelfAndBaseAreNotExternal`
- [x] `BasicScopeAttributes_EmbeddedLanguage_LongestScopeWins` (`source.css` vs `source.css.embedded`)
- [x] `BasicScopeAttributes_StandardTokenType_FromScopeName` and `…_FromTokenTypesMap`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The registry that uses the processor (P4-20).

## Suggested commit message

`feat(textmate): port grammar dependencies and basic scope attributes`
