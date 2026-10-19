# P4-11 — Rules (1/2): `RegExpSource`, `RegExpSourceList`, `CompiledRule`

- **Status:** done
- **Depends on:** P4-10
- **Size:** M

## Goal

The regex side of vscode-textmate rules exists: a rule's regex source with its `\A` / `\G` anchor variants and back
references, a list of sources compiled into one scanner, and the compiled rule that finds the next match.

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/rule.ts` | 10–29 | `HAS_BACK_REFERENCES`, `BACK_REFERENCING_END`, `ruleIdSymbol` (as `RawRule.Id`, P4-06), `endRuleId = -1`, `whileRuleId = -2`, `ruleIdFromNumber`, `ruleIdToNumber` |
| textmate: `src/rule.ts` | 575–735 | `IRegExpSourceAnchorCache`, `RegExpSource` (constructor with the `\z` rewrite and `hasAnchor`, `clone`, `setSource`, `resolveBackReferences`, `_buildAnchorCache`, `resolveAnchors`) |
| textmate: `src/rule.ts` | 737–899 | `IRegExpSourceListAnchorCache`, `RegExpSourceList` (`push`, `unshift`, `length`, `setSource`, `compile`, `compileAG`, `_resolveAnchors`, `dispose`), `CompiledRule`, `IFindNextMatchResult` |
| textmate: `src/debug.ts` | 5–9 | `DebugFlags` (always off), `UseOnigurumaFindOptions = false` |

## Files to create or change

- `src/LoomEditor.TextMate/Grammar/RuleId.cs`, `RegExpSource.cs`, `RegExpSourceList.cs`, `CompiledRule.cs`, `DebugFlags.cs` — create
- `tests/LoomEditor.TextMate.Tests/Grammar/RegExpSourceTests.cs` — create

## Design decisions (already made — do not change)

- `RuleId` is a `readonly record struct RuleId(int Value)` (upstream's branded number), with `End = -1`, `While = -2`.
- `HAS_BACK_REFERENCES` / `BACK_REFERENCING_END` become `[GeneratedRegex]`es with `[0-9]` (trap 4).
  `escapeRegExpCharacters` is the one of P4-05.
- The anchor cache keeps the four strings (`A0_G0`, `A0_G1`, `A1_G0`, `A1_G1`); a disallowed anchor becomes `￿`, as upstream.
- `CompiledRule` owns its `IOnigScanner` and disposes it; `RegExpSourceList` caches one compiled rule per anchor
  variant and disposes them in `Dispose` (upstream `dispose` / `_disposeCaches`).
- `findNextMatchSync` is always called with `FindOption.None` (`UseOnigurumaFindOptions = false`).

## Tests

No upstream unit tests for these classes. New:

- [x] `RegExpSource_BackslashZ_IsRewritten` (`\z` → `$(?!\n)(?<!\n)`)
- [x] `RegExpSource_ResolveBackReferences_EscapesCapturedText`
- [x] `RegExpSource_ResolveAnchors_DisallowedAnchorsBecomeFFFF` (the four variants)
- [x] `RegExpSourceList_CompileAG_CachesOneScannerPerVariant`
- [x] `CompiledRule_FindNextMatch_ReturnsRuleIdOfMatchedPattern`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The `Rule` classes and `RuleFactory` (P4-12).

## Suggested commit message

`feat(textmate): port regex sources and compiled rules`
