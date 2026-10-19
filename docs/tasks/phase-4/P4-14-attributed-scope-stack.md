# P4-14 — Grammar (1/6): `AttributedScopeStack` and `BalancedBracketSelectors`

- **Status:** todo
- **Depends on:** P4-13
- **Size:** M

## Goal

The scope stack of a token (scope names + the encoded metadata merged from the theme and the basic scope attributes)
exists, with the balanced-bracket selectors that set the bracket bit.

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/grammar/grammar.ts` | 39–47 | `IThemeProvider`, `IGrammarRepository` |
| textmate: `src/grammar/grammar.ts` | 427–600 | `AttributedScopeStack` (`fromExtension`, `createRoot`, `createRootAndLookUpScopeName`, `scopeName`, `toString`, `equals`, `mergeAttributes`, `pushAttributed`, `_pushAttributed`, `getScopeNames`, `getExtensionIfDefined`), `AttributedScopeStackFrame` |
| textmate: `src/grammar/grammar.ts` | 906–956 | `TokenTypeMatcher`, `BalancedBracketSelectors` (`matchesAlways`, `matchesNever`, `match`) |

## Files to create or change

- `src/LoomEditor.TextMate/Grammar/AttributedScopeStack.cs`, `BalancedBracketSelectors.cs`, `IGrammarRepository.cs` (both interfaces) — create
- `tests/LoomEditor.TextMate.Tests/Grammar/AttributedScopeStackTests.cs` — create

## Design decisions (already made — do not change)

- Immutable linked list, as upstream (`Parent`, `ScopePath`, `TokenAttributes`).
- `mergeAttributes` ignores font attributes (ADR 0008 decision 3): only font style, foreground, background.
- `equals` is structural (static `Equals(a, b)` walking both stacks), like upstream; do not override
  `object.Equals` with it.

## Tests

No upstream unit tests. New:

- [ ] `PushAttributed_MergesThemeColorsOfTheDeepestMatch`
- [ ] `PushAttributed_ScopeWithSpaces_PushesEachSegment`
- [ ] `GetExtensionIfDefined_ReturnsFramesAboveBase`
- [ ] `BalancedBracketSelectors_UnbalancedWins_AndStarMatchesAlways`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`StateStackImpl` (P4-15).

## Suggested commit message

`feat(textmate): port the attributed scope stack`
