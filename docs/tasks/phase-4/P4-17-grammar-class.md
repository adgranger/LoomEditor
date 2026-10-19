# P4-17 — Grammar (4/6): the `Grammar` class, injections and rule registry

- **Status:** done
- **Depends on:** P4-16
- **Size:** M

## Goal

`Grammar` exists without its tokenization loop: it clones and initializes the raw grammar (`$self`, `$base`),
registers rules, creates scanners through `IOnigLib`, collects injections (its own and those given by the repository),
and resolves external grammars.

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/grammar/grammar.ts` | 17–37 | `createGrammar` |
| textmate: `src/grammar/grammar.ts` | 49–96 | `Injection`, `collectInjections`, `nameMatcher`, `scopesAreMatching` |
| textmate: `src/grammar/grammar.ts` | 98–276 | `Grammar`: constructor, `themeProvider`, `dispose`, `createOnigScanner`, `createOnigString`, `getMetadataForScope`, `_collectInjections`, `getInjections`, `registerRule`, `getRule`, `getExternalGrammar` |
| textmate: `src/grammar/grammar.ts` | 414–425 | `initGrammar` |
| textmate: `src/main.ts` | 33–49 | `IEmbeddedLanguagesMap`, `ITokenTypeMap`, `IGrammarConfiguration` |

## Files to create or change

- `src/LoomEditor.TextMate/Grammar/Grammar.cs` (+ `Grammar.Injections.cs`) — create
- `src/LoomEditor.TextMate/Grammar/GrammarConfiguration.cs` — create
- `tests/LoomEditor.TextMate.Tests/TestHelpers/TestGrammars.cs` — create (inline JSON → raw grammar → `Grammar`, with
  `OnigTestLib` and a fake `IGrammarRepository` / `IThemeProvider`)
- `tests/LoomEditor.TextMate.Tests/Grammar/GrammarTests.cs` — create

## Design decisions (already made — do not change)

- `initGrammar` deep-clones the raw grammar (`RawGrammar.DeepClone`, trap 7) before adding `$self` / `$base`.
- `_ruleId2desc` is a `List<Rule?>` indexed by rule id; `_externalGrammars` and `_grammarRepository` lookups are
  single-threaded dictionaries (remarks: "Threading: one thread", ADR 0008 decision 5).
- `Grammar` is `IDisposable`: it disposes its rules (and so their scanners).
- `tokenizeLine`, `tokenizeLine2` and `_tokenize` are **not** part of this card (P4-19); do not add stubs that
  throw — leave the methods out.

## Tests

No upstream unit tests. New:

- [x] `CreateGrammar_InitializesSelfAndBase`
- [x] `CreateGrammar_DoesNotMutateTheGivenRawGrammar`
- [x] `GetInjections_CollectsGrammarAndRepositoryInjections_InPriorityOrder`
- [x] `GetExternalGrammar_LoadsFromRepositoryOnce`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Tokenization (P4-18, P4-19), the registry (P4-20).

## Suggested commit message

`feat(textmate): port the grammar class and injections`
