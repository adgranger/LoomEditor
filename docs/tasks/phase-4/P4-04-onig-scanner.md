# P4-04 — `OnigScanner`, `OnigString` and vscode-textmate's `IOnigLib`

- **Status:** todo
- **Depends on:** P4-03
- **Size:** M

## Goal

The tokenizer's view of the regex engine exists: `IOnigLib` (create a scanner from a list of patterns, create a
string), `IOnigScanner.FindNextMatchSync(string, startPosition, options)` with vscode-oniguruma's algorithm, and the
vscode-oniguruma test suite passes against it.

## Sources

| File | Lines | What to port |
|---|---|---|
| textmate: `src/onigLib.ts` | 7–56 | `IOnigLib`, `IOnigCaptureIndex`, `IOnigMatch`, `FindOption`, `OnigScanner`, `OnigString`, `disposeOnigString` |
| oniguruma: `src/onig.cc` | 135–157 | `searchOnigRegExp` (per-regex cache: string id, position, option) |
| oniguruma: `src/onig.cc` | 164–209, 211–228, 231–272 | `createOnigScanner`, `freeOnigScanner`, `toOnigOption`, `findNextOnigScannerMatch` — **only the per-regex branch** (250–271) |
| oniguruma: `src/index.ts` | 8–26, 174–246, 248–344 | `FindOption` values, `OnigString` (id, content; no UTF-8 buffers), `OnigScanner` (constructor, `findNextMatchSync` overloads, `_findNextMatchSync`) |
| oniguruma: `src/test/index.test.ts` | 21–130 | the 12 tests |

## Files to create or change

- `src/LoomEditor.TextMate/Oniguruma/IOnigLib.cs`, `IOnigScanner.cs`, `IOnigString.cs`, `OnigMatch.cs` (`IOnigMatch` + `IOnigCaptureIndex` as `readonly record struct OnigCaptureIndex(int Start, int End)` with `Length`), `FindOption.cs` — create
- `src/LoomEditor.TextMate/Oniguruma/OnigScanner.cs`, `OnigString.cs` — create
- `src/LoomEditor.TextMate/Oniguruma/IRegexEngine.cs` — change: `IRegexEngine : IOnigLib`; `OnigurumaEngine` implements it
- `tests/LoomEditor.TextMate.Tests/TestHelpers/OnigTestLib.cs` — create (port of textmate `src/tests/onigLibs.ts` 16–19)
- `tests/LoomEditor.TextMate.Tests/Oniguruma/OnigScannerTests.cs` — create

## Design decisions (already made — do not change)

- ADR 0007, decision 3: always the per-regex loop, earliest start wins, **strict `<`** (lowest index wins a tie), stop
  when a match starts at `startPosition`. Cache per regex: `(stringId, position, option, matched, region)`; reuse when
  the id and option are equal and `lastPosition <= position` and (no match last time, or the last match starts at
  `>= position`); never for a regex with `\G`.
- `OnigString.Id` comes from an `Interlocked.Increment` on a static counter (the only static mutable field allowed here:
  ids must be unique across threads). An `OnigString` holds the .NET `string` (no native copy: subjects are pinned per
  search).
- `FindOption`: `None = 0, NotBeginString = 1, NotEndString = 2, NotBeginPosition = 4, DebugCall = 8` → the Oniguruma
  options `ONIG_OPTION_NOT_BEGIN_STRING`, `NOT_END_STRING`, `NOT_BEGIN_POSITION` (values from the 6.9.10 header).
  `DebugCall` is accepted and ignored.
- A scanner is single-threaded (remarks), owns its `OnigRegex`es and frees them in `Dispose`.
- `findNextMatchSync(string, …)` with a plain string creates and drops a temporary `OnigString` (upstream 290–311).

## Tests

- [ ] Port: vscode-oniguruma `'simple1'`, `'simple2'`, `'unicode1'`, `'unicode2'`, `'unicode3'`, `'unicode4'`,
      `'out of bounds'`, `'regex with \G'`, `'kkos/oniguruma#192'`, `'FindOption.FIND_NOT_BEGIN_POSITION'`,
      `'FindOption.FIND_NOT_BEGIN_STRING'`, `'FindOption.FIND_NOT_END_STRING'` (use the exact names from the file)
- [ ] New: `FindNextMatch_TwoPatternsSameStart_LowestIndexWins`
- [ ] New: `FindNextMatch_CachedResult_IsReusedForALaterPosition` (same string, two calls; check the result, not the cache internals)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map updated; "Behavior differences": UTF-16 encoding (no UTF-8 mapping), no regset branch
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Anything that uses the scanner (rules, P4-11).

## Suggested commit message

`feat(textmate): port vscode-oniguruma's scanner`
