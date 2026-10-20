# P5-84 — Indentation folding ranges with `#region` markers and off-side rule

- **Status:** todo
- **Depends on:** P5-83
- **Size:** M

## Goal

`computeRanges` finds the folding ranges of a model from indentation, from the `folding.markers` of the language
configuration (`#region` / `#endregion`…) and from the `offSide` rule (Python, YAML: empty lines belong to the block
above). It is the only range provider of phase 5 (ADR 0009, decision 9). The limit of 5 000 ranges is applied as in
Monaco.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/model/utils.ts` | 13–35 | `computeIndentLevel` |
| `contrib/folding/browser/folding.ts` | 48–52, 61–64 | `RangeProvider`, `FoldingLimitReporter` |
| `contrib/folding/browser/indentRangeProvider.ts` | 14–36 | `MAX_FOLDING_REGIONS_FOR_INDENT_DEFAULT`, `ID_INDENT_PROVIDER`, `IndentRangeProvider` |
| same | 38–113 | `RangesCollector` (`insertFirst`, `toIndentRanges` with the limit) |
| same | 115–225 | `PreviousRegion`, `foldingRangesLimitDefault`, `computeRanges` |
| `contrib/folding/test/browser/indentRangeProvider.test.ts` | all | 26 tests |
| `contrib/folding/test/browser/indentFold.test.ts` | all | 1 test |
| `contrib/folding/test/browser/foldingRanges.test.ts` | 36–118 | the three tests that use `computeRanges` / `RangesCollector` |

## Files to create or change

- `src/LoomEditor.Core/Model/TextModelUtils.cs` — create: `ComputeIndentLevel(string line, int tabSize)` (header: `common/model/utils.ts`)
- `src/LoomEditor.View/Features/Folding/IRangeProvider.cs` — create: `IRangeProvider`, `IFoldingLimitReporter`
- `src/LoomEditor.View/Features/Folding/IndentRangeProvider.cs` — create: `IndentRangeProvider`, `RangesCollector`, `ComputeRanges`
- `tests/LoomEditor.Core.Tests/Model/TextModelUtilsTests.cs` — create
- `tests/LoomEditor.View.Tests/Features/Folding/IndentRangeProviderTests.cs`, `IndentFoldTests.cs` — create
- `tests/LoomEditor.View.Tests/Features/Folding/FoldingRangesTests.cs` — change: the three remaining tests

## Design decisions (already made — do not change)

- The provider is **synchronous**: `IRangeProvider { string Id { get; } FoldingRegions? Compute(); }` + `IDisposable`
  (Monaco returns a `Promise` and takes a `CancellationToken`; the indentation provider resolves at once). The syntax
  provider of phase 8 can make it asynchronous.
- `IndentRangeProvider(TextModel model, LanguageConfigurationRegistry registry, IFoldingLimitReporter limitReporter)`:
  `Compute()` reads `registry.GetLanguageConfiguration(model.Tokenization.GetLanguageId()).FoldingRules` (step B:
  `FoldingRules.OffSide`, `FoldingRules.Markers` with `Start` / `End` as `Regex`) and calls `ComputeRanges`.
- `ComputeRanges(TextModel model, bool offSide, FoldingMarkers? markers = null, IFoldingLimitReporter? limitReporter = null)`;
  the default reporter has `Limit = 5000` (`MAX_FOLDING_REGIONS_FOR_INDENT_DEFAULT`) and ignores updates.
- Markers (phase-5 README, traps 1 and 2): when `markers.Start.Options == markers.End.Options`, Monaco's combined
  pattern becomes `new Regex($"({markers.Start})|(?:{markers.End})", markers.Start.Options)` (`Regex.ToString()` is the
  source); otherwise the two regexes are used one after the other. `isStartMatch = !!m[1]` is
  `m.Groups[1].Success && m.Groups[1].Length > 0` (an empty capture is falsy in JavaScript). No `lastIndex`: use
  `Match(lineContent)` / `IsMatch(lineContent)` from index 0.
- `RangesCollector` keeps `List<int>` for `_startIndexes`, `_endIndexes`, `_indentOccurrences` (`_indentOccurrences[indent] || 0`
  → grow the list with zeros); `toIndentRanges` builds `uint[]` of the exact length.
- `ComputeIndentLevel` returns -1 for a line of whitespace only, like Monaco.
- The tests write Monaco's JavaScript marker regexes as .NET regexes with the same text (`^\s*#region\b`, `^#region$`, …).

## Tests

- [ ] Port: `Indentation Folding` (indentRangeProvider.test.ts) › `Fold one level`, `Fold two levels`,
      `Fold three levels`, `Fold decreasing indent`, `Fold Java`, `Fold Javadoc`, `Fold Whitespace Java`,
      `Fold Whitespace Python`, `Fold Tabs`
- [ ] Port: `Folding with regions` › `Inside region, indented`, `Inside region, not indented`, `Empty Regions`,
      `Nested Regions`, `Nested Regions 2`, `Incomplete Regions`, `Incomplete Regions 2`, `Indented region before`,
      `Indented region before 2`, `Indented region in-between`, `Indented region after`, `With off-side`,
      `Nested with off-side`, `Issue 35981`, `Misspelled Markers`, `Issue 79359`, `Markers with stateful flags`
      (JavaScript's `g` flag has no .NET equivalent and is dropped; `i` becomes `RegexOptions.IgnoreCase`, so the
      "different options" path is taken exactly where Monaco's flags differ; a comment says so)
- [ ] Port: `Indentation Folding` (indentFold.test.ts) › `Limit by indent`
- [ ] Port: `FoldingRanges` › `test max folding regions`, `findRange`, `setCollapsed`
- [ ] New (Core): `ComputeIndentLevel_TabsAndSpaces_CountsVisibleColumns`, `ComputeIndentLevel_WhitespaceOnly_ReturnsMinusOne`
- [ ] New: `IndentRangeProvider_UsesTheLanguageFoldingRules` (a language whose configuration has markers: the region is found)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: rows `contrib/folding/browser/indentRangeProvider.ts` (✅ P5-84), `common/model/utils.ts`
      (`computeIndentLevel` only); "Tests ported": `indentRangeProvider.test.ts` 26 / 26, `indentFold.test.ts` 1 / 1,
      `foldingRanges.test.ts` 7 / 7
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`syntaxRangeProvider.ts` and `syntaxFold.test.ts` (phase 8). The `RangesLimitReporter` of the controller (P5-88).

## Suggested commit message

`feat(folding): compute folding ranges from indentation and markers`
