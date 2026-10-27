# P9-07 — Text abstraction and range mappings: `LineRangeMapping`, `RangeMapping`, `LinesDiff`

- **Status:** done
- **Depends on:** P9-06
- **Size:** M

## Goal

The result types of a diff exist: `RangeMapping` (character level), `LineRangeMapping` and `DetailedLineRangeMapping` (line level with the inner character changes), the conversion from the first to the second, and `LinesDiff` / `MovedText` / the computer interface.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/diff/rangeMapping.ts` | 19–173 | `LineRangeMapping` (`inverse`, `clip`, `join`, `flip`, `toRangeMapping`, `toRangeMapping2`, `join`, `toString`...: port **every** member) |
| same | 174–195 | `normalizePosition`, `isValidLineNumber` |
| same | 196–242 | `DetailedLineRangeMapping` (`fromRangeMappings`, `inverse`, `flip`, `withInnerChangesFromLineRanges`) |
| same | 243–321 | `RangeMapping` (`fromEdit`, `fromEditJoin`, `assertSorted`, `flip`, `join`, `toTextEdit`, `toString`...: every member that does not need Monaco's `StringEdit`/`TextEdit` classes — see decisions) |
| same | 322–397 | `lineRangeMappingFromRangeMappings`, `getLineRangeMapping` |
| `src/vs/editor/common/diff/linesDiffComputer.ts` | 8–59 | `ILinesDiffComputer`, `ILinesDiffComputerOptions`, `LinesDiff`, `MovedText` |
| `src/vs/editor/common/core/text/abstractText.ts` | 15–109 | `AbstractText`, `LineBasedText`, `ArrayText` (not `StringText`) |
| `src/vs/editor/test/node/diffing/defaultLinesDiffComputer.test.ts` | 31–end | suite `lineRangeMapping` (all tests) |

## Files to create or change

- `src/LoomEditor.Core/Diff/RangeMapping.cs`, `LineRangeMapping.cs`, `DetailedLineRangeMapping.cs`, `LineRangeMappingConversions.cs` (the two static functions), `LinesDiff.cs` (with `MovedText`), `ILinesDiffComputer.cs` (with `LinesDiffComputerOptions`) — create
- `src/LoomEditor.Core/Diff/AbstractText.cs` (`AbstractText`, `LineBasedText`, `ArrayText`) — create
- `tests/LoomEditor.Core.Tests/Diff/RangeMappingTests.cs`, `LineRangeMappingTests.cs` — create

## Design decisions (already made — do not change)

- Classes are `sealed` where Monaco never subclasses; `DetailedLineRangeMapping : LineRangeMapping` (so `LineRangeMapping` is not sealed). `LineRange original`, `LineRange modified`, `IReadOnlyList<RangeMapping>? InnerChanges` (null = unknown).
- `ILinesDiffComputerOptions` becomes `sealed record LinesDiffComputerOptions(bool IgnoreTrimWhitespace, int MaxComputationTimeMs, bool ComputeMoves, bool ExtendToSubwords = false)`; `ILinesDiffComputer.ComputeDiff(string[] originalLines, string[] modifiedLines, LinesDiffComputerOptions options)`. `LinesDiff(IReadOnlyList<DetailedLineRangeMapping> Changes, IReadOnlyList<MovedText> Moves, bool HitTimeout)`; `MovedText(LineRangeMapping LineRangeMapping, IReadOnlyList<DetailedLineRangeMapping> Changes)` with `Flip()`.
- **Not ported**: everything that needs `TextEdit` / `StringEdit` / `TextReplacement` of Monaco (`RangeMapping.toTextEdit`, `fromEdit`, `fromEditJoin`, the `AbstractText.getValueOfRange` is **kept**), `lineRangeMappingFromChange` (`IChange`, legacy), `StringText`. A member that cannot be ported for this reason is listed in the XML doc of its class (`Not ported: …`).
  The existing `LoomEditor.Core.Text.TextEdit` is **not** the same thing: do not use it here.
- `AbstractText`: `abstract string GetValueOfRange(Range range)`, `TextLength Length`, `LineCount`, `GetLineLength(int lineNumber)`, `GetLineAt`… port what lines 15–109 declare (use the existing `TextLength` of `src/LoomEditor.Core/Text/TextLength.cs`; read it first and add members only if the port needs them).
- `LineRangeMapping.ToString()` format `{[2,3)->[2,2)}` and `RangeMapping.ToString()` format are used by tests and fixtures: copy exactly.
- `<remarks>Threading: immutable.</remarks>`

## Tests

- [x] Port: suite `lineRangeMapping` of `defaultLinesDiffComputer.test.ts`, tests `Simple`, `Empty Lines`, `Whitespace` ... (all of them; they use `getLineRangeMapping(new RangeMapping(...), new ArrayText([...]), new ArrayText([...])).toString()`).
- [x] New: `Inverse_FullCoverage`, `Inverse_Empty`, `Clip_*`, `Join_*`, `Flip_SwapsOriginalAndModified`, `LineRangeMappingFromRangeMappings_GroupsByLine`, `RangeMapping_AssertSorted_Unsorted_Asserts`, `ArrayText_GetValueOfRange_MultiLine`, `ArrayText_Length`.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map updated: `rangeMapping.ts`, `linesDiffComputer.ts`, `abstractText.ts` (+ the "Not ported" list)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`DefaultLinesDiffComputer` (P9-08), moved line computation (P9-09), `documentDiffProvider.ts` (P9-11).

## Suggested commit message

`feat(core): port the diff range mappings and LinesDiff`
