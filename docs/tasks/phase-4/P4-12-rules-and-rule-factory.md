# P4-12 — Rules (2/2): the rule classes, `RuleFactory` and `parseInclude`

- **Status:** done
- **Depends on:** P4-11
- **Size:** L — keep it under 400 lines with partial files (`RuleFactory.Patterns.cs`)

## Goal

Raw rules become compiled rule objects: match rules, begin/end rules, begin/while rules, include-only rules and
capture rules, with the factory that resolves `include`s (`$self`, `$base`, `#name`, `scope`, `scope#name`).

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/rule.ts` | 31–94 | `IRuleRegistry`, `IGrammarRegistry`, `IRuleFactoryHelper`, abstract `Rule` (`getName`, `getContentName` with capture replacement), `ICompilePatternsResult` |
| textmate: `src/rule.ts` | 96–379 | `CaptureRule`, `MatchRule`, `IncludeOnlyRule`, `BeginEndRule` (no `end` → `'￿'`, `getEndWithResolvedBackReferences`, `_getCachedCompiledPatterns` with `applyEndPatternLast`), `BeginWhileRule` (`compileWhile`, `compileWhileAG`) |
| textmate: `src/rule.ts` | 381–573 | `RuleFactory` (`createCaptureRule`, `getCompiledRuleId`, `_compileCaptures`, `_compilePatterns`) |
| textmate: `src/grammar/grammarDependencies.ts` | 240–295 | `IncludeReference`, `IncludeReferenceKind`, `BaseReference`, `SelfReference`, `RelativeReference`, `TopLevelReference`, `TopLevelRepositoryReference`, `parseInclude` |

## Files to create or change

- `src/LoomEditor.TextMate/Grammar/Rule.cs`, `CaptureRule.cs`, `MatchRule.cs`, `IncludeOnlyRule.cs`, `BeginEndRule.cs`, `BeginWhileRule.cs` — create
- `src/LoomEditor.TextMate/Grammar/RuleFactory.cs` (+ `RuleFactory.Patterns.cs`), `IRuleRegistry.cs` (the three interfaces) — create
- `src/LoomEditor.TextMate/Grammar/IncludeReference.cs` — create
- `tests/LoomEditor.TextMate.Tests/Grammar/RuleFactoryTests.cs` — create (with a fake `IRuleFactoryHelper` in the test file)

## Design decisions (already made — do not change)

- `Rule` subclasses keep upstream's fields and method names (PascalCase). `dispose` → `IDisposable` (disposes the
  `RegExpSourceList`s).
- `getCompiledRuleId` writes the id into `RawRule.Id` (the raw rule is a clone owned by the grammar: trap 7).
- `_compileCaptures` iterates captures in numeric key order (P4-06 already sorts them) and builds an array whose
  length is the largest key + 1, with `null` holes (trap 7 of phase 3: keep the `null` checks).
- `IncludeReference` is a small class hierarchy with a `Kind` property, like upstream.
- A rule whose `include` cannot be resolved is skipped, as upstream (no exception).

## Tests

No upstream unit tests (vscode-textmate tests these through tokenization). New:

- [x] `ParseInclude_AllForms` (`$self`, `$base`, `#name`, `source.js`, `source.js#name`)
- [x] `GetCompiledRuleId_SameRawRule_ReturnsSameId`
- [x] `CompilePatterns_IncludeOfRepositoryRule_IsResolved`, `…_UnknownInclude_IsSkipped`
- [x] `BeginEndRule_NoEnd_UsesFFFF`
- [x] `BeginEndRule_ApplyEndPatternLast_PutsEndLast`
- [x] `CompileCaptures_HolesAreNull`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Collecting external grammar dependencies (`ScopeDependencyProcessor`, P4-13).

## Suggested commit message

`feat(textmate): port the TextMate rule classes and rule factory`
