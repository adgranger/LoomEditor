# P5-63 — `TextModelSearch`: find all, next and previous matches in a model

- **Status:** todo
- **Depends on:** P5-62
- **Size:** M

## Goal

`TextModelSearch.FindMatches / FindNextMatch / FindPreviousMatch` search a `TextModel` line by line or, for multiline
regexes, over the text joined with `\n`, with the CRLF offset fix-up of `LineFeedCounter`. The 36 remaining tests of
Monaco's `textModelSearch.test.ts` pass.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/model/textModelSearch.ts` | 112–159 | `LineFeedCounter` |
| same | 161–429 | `TextModelSearch` (`findMatches`, `_getMultilineMatchRange`, `_doFindMatchesMultiline`, `_doFindMatchesLineByLine`, `_findMatchesInLine`, `findNextMatch`, `_doFindNextMatchMultiline`, `_doFindNextMatchLineByLine`, `_findFirstMatchInLine`, `findPreviousMatch`, `_doFindPreviousMatchMultiline`, `_doFindPreviousMatchLineByLine`, `_findLastMatchInLine`) |
| `src/vs/editor/test/common/model/textModelSearch.test.ts` | 18–76 | helpers `assertFindMatch`, `_assertFindMatches`, `assertFindMatches`, `regularText` |

## Files to create or change

- `src/LoomEditor.Core/Model/Search/TextModelSearch.cs` — create (static class; `LineFeedCounter` as a private nested class)
- `src/LoomEditor.Core/Model/TextModel.Search.cs` — create with only `internal Range GetFullModelRange()` (Monaco
  `textModel.ts` 1168–1172) if it does not exist yet; P5-64 adds the public-facing search methods
- `tests/LoomEditor.Core.Tests/Model/Search/TextModelSearchTests.cs` — create (one class for the suite; if it passes
  ~400 lines, split it into `TextModelSearchTests.Find.cs` / `.NextPrevious.cs` partials)
- `tests/LoomEditor.Core.Tests/Model/Search/TestHelpers/SearchAssert.cs` — create (`FindMatch` / `FindMatches` comparisons:
  range equality and `Matches` sequence equality, since a record compares arrays by reference)

## Design decisions (already made — do not change)

- `TextModel` API used: `GetOffsetAt`, `GetPositionAt`, `GetValueInRange(range, EndOfLinePreference.LF)`, `Eol`,
  `LineCount`, `GetLineContent`, `GetLineMaxColumn`. Use them as Monaco does; no new model API besides `GetFullModelRange`.
- `substring` → `Substring(start, end - start)` (phase 3 trap 6). The line text is cut **before** searching, so a
  pattern never sees what is outside the range (Monaco does the same).
- `m.index` / `m[0].length` → `m.Index` / `m.Length`.
- `LineFeedCounter.findLineFeedCountBeforeOffset`: `(max - min) / 2 >> 0` is an integer division of non-negative values.
- `_doFindPreviousMatchMultiline` uses `10 * LimitFindCount` like Monaco.
- Recursion ("try again from the top") stays recursion, like Monaco.

## Tests

`textModelSearch.test.ts`, suite `TextModelSearch` — port all of these (36), with `// Monaco:` names:

- [ ] `Simple find`, `Case sensitive find`, `Whole words find`, `/^/ find`, `/$/ find`, `/.*/ find`, `/^$/ find`
- [ ] `multiline find 1`, `multiline find 2`, `multiline find 3`, `multiline find 4`, `multiline find with line beginning regex`,
      `matching empty lines using boundary expression`, `matching lines starting with A and ending with B`,
      `multiline find with line ending regex`, `issue #4836 - ^.*$`, `multiline find for non-regex string`
- [ ] `issue #3623: Match whole word does not work for not latin characters`, `issue #27459: Match whole words regression`,
      `issue #27594: Search results disappear`
- [ ] `findNextMatch without regex`, `findNextMatch with beginning boundary regex`,
      `findNextMatch with beginning boundary regex and line has repetitive beginnings`,
      `findNextMatch with beginning boundary multiline regex and line has repetitive beginnings`, `findNextMatch with ending boundary regex`
- [ ] `findMatches with capturing matches`, `findMatches multiline with capturing matches`, `findNextMatch with capturing matches`,
      `findNextMatch multiline with capturing matches`, `findPreviousMatch with capturing matches`, `findPreviousMatch multiline with capturing matches`
- [ ] `\n matches \r\n`, `\r can never be found`, `issue #53415. \W should match line break.`
- [ ] `issue #74715. \d* finds empty string and stops searching.`, `issue #100134. Zero-length matches should properly step over surrogate pairs`
- [ ] **Not ported:** `Simple find using unicode escape sequences` — `\u{0066}` is JavaScript-only syntax; .NET rejects it
      (ADR 0009, decision 6). Write the reason in the PORTING tests table.
- `createTextModel(text)` → `new TextModel(text)`; `model.setEOL(EndOfLineSequence.CRLF)` → `model.SetEol(EndOfLineSequence.CRLF)`.
  `assertFindMatches` runs every case on an LF and on a CRLF model, as in Monaco.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `textModelSearch.ts` ✅ (P5-62, P5-63); tests table `textModelSearch.test.ts` 41 / 42 with the reason
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`TextModel.FindMatches` with search scopes and the piece tree fast path (P5-64).

## Suggested commit message

`feat(core): port TextModelSearch with its tests`
