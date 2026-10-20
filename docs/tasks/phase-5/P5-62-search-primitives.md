# P5-62 — Search primitives: `SearchParams`, `SearchData`, `FindMatch`, `Searcher`

- **Status:** todo
- **Depends on:** P5-61
- **Size:** M

## Goal

The building blocks of every text search exist in Core: a search request (`SearchParams`) turns into a compiled .NET
regex plus word separators (`SearchData`), and a `Searcher` walks the matches of one string exactly like Monaco's
`Searcher` (JavaScript `lastIndex` semantics, empty matches, whole-word checks). Nothing searches a model yet (P5-63).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `common/model/textModelSearch.ts` | 14 | `LIMIT_FIND_COUNT` (999) |
| same | 16–67 | `SearchParams` (`parseSearchRequest`) |
| same | 69–99 | `isMultilineRegexSource` |
| same | 101–110 | `createFindMatch` |
| same | 431–492 | `leftIsWordBounday`, `rightIsWordBounday`, `isValidMatch` |
| same | 494–555 | `Searcher` |
| `common/model.ts` | 662–676 | `FindMatch` |
| same | 1570–1590 | `SearchData` |
| `src/vs/base/common/strings.ts` | 86–91 | `escapeRegExpCharacters` |
| same | 202–240 | `RegExpOptions`, `createRegExp` (without `wholeWord`, see decisions) |

## Files to create or change

- `src/LoomEditor.Core/Model/Search/SearchParams.cs` — create (`SearchParams`, static `IsMultilineRegexSource`, `LimitFindCount`)
- `src/LoomEditor.Core/Model/Search/SearchData.cs`, `FindMatch.cs`, `Searcher.cs` — create
- `src/LoomEditor.Core/Model/Search/README.md` — create (what lives here, the regex mapping below)
- `src/LoomEditor.Core/Text/Strings.Regex.cs` — create (`partial class Strings`: `CreateRegExp`; `EscapeRegExpCharacters` already exists, P5-11)
- `tests/LoomEditor.Core.Tests/Model/Search/SearchParamsTests.cs`, `SearcherTests.cs` — create

## Design decisions (already made — do not change)

- Namespace `LoomEditor.Core.Model.Search` (ADR 0009, decision 2). Everything is `internal`.
- `FindMatch` is `internal sealed record FindMatch(Range Range, string?[]? Matches)`. A `null` entry in `Matches` is a
  group that did not take part in the match (JavaScript `undefined`). `createFindMatch` becomes
  `FindMatch.Create(Range range, Match match, bool captureMatches)`: it copies `match.Groups[0..Count-1]`, `null` when
  `Group.Success` is false.
- `SearchData` is `internal sealed class SearchData(Regex regex, WordCharacterClassifier? wordSeparators, string? simpleSearch)`
  with `Regex`, `WordSeparators`, `SimpleSearch` and `IsMultiline` (`regex.Options.HasFlag(RegexOptions.Multiline)`, Monaco's
  `regex.multiline`).
- **JavaScript flags → .NET options** (ADR 0009, decision 6). `Strings.CreateRegExp(string searchString, bool isRegex, bool matchCase, bool multiline)`:
  `RegexOptions.CultureInvariant`, `| IgnoreCase` when `!matchCase`, `| Multiline` when `multiline`. Never
  `ECMAScript`, never `Compiled` (no gain under Native AOT). The `g` flag has no .NET option: `Searcher` keeps its own
  index (below). The `u` flag has none either: see "behavior differences". `wholeWord` is not ported (no caller: find uses
  word separators, not `\b`). An empty `searchString` throws `ArgumentException` like Monaco; an invalid pattern throws
  `RegexParseException` (an `ArgumentException`): `ParseSearchRequest` catches `ArgumentException` and returns `null`.
- `Strings.EscapeRegExpCharacters` (P5-11) escapes **exactly** Monaco's set `\ { } * + ? | ^ $ . [ ] ( )` (not `Regex.Escape`, which
  also escapes `#` and spaces and would show `\ ` in the find box when `setSearchString` escapes a selection). (phase 5 README trap 4).
- `getMapForWordSeparators(ws, [])` → `WordCharacterClassifierCache.Get(ws, [])` (exists, phase 3).
- The case test of `parseSearchRequest` (`searchString.toLowerCase() !== searchString.toUpperCase()`) uses
  `ToLowerInvariant` / `ToUpperInvariant`.
- **`Searcher` and `lastIndex`.** Monaco moves `regex.lastIndex`; here `Searcher` has a private `int _lastIndex`:
  - `Reset(int lastIndex)` sets it and resets `_prevMatchStartIndex = -1`, `_prevMatchLength = 0`.
  - `Next(string text)` → `Match?`: if `_lastIndex > text.Length` (JavaScript `exec` fails and resets `lastIndex`), set
    `_lastIndex = 0` and return `null`. Otherwise `Match m = regex.Match(text, _lastIndex)` — the `(string, int startat)`
    overload, **never** `(string, int beginning, int length)`: like JavaScript, lookbehinds and `^` still see the text before
    `startat`. On failure set `_lastIndex = 0` and return `null`; on success set `_lastIndex = m.Index + m.Length`.
  - Empty match equal to the previous one: advance `_lastIndex` by 2 when `Strings.GetNextCodePoint(text, len, _lastIndex) > 0xFFFF`,
    else by 1, and loop (Monaco lines 530–543). Never use `Match.NextMatch`.
- `isValidMatch` and its two helpers become `static` methods of `Searcher` (`Searcher.IsValidMatch`). `charCodeAt` out of
  range cannot happen there (guarded by the length checks); keep the order of the checks.
- `Searcher` exposes `WordSeparators` (Monaco's public `_wordSeparators`, read by the piece tree in P5-64).

## Behavior differences (write them in the XML docs and in `docs/PORTING.md`)

- Regex syntax is .NET's: `\u{…}`, `\p{…}` names and other JavaScript-only syntax behave as .NET does; `\d`, `\w`, `\b`
  are Unicode-aware in .NET (ASCII in JavaScript without `u`); without the `u` flag, `.` and classes match one UTF-16
  unit, not one code point. Mixed named/unnamed groups are numbered the .NET way (unnamed first).
- Case-insensitive matching uses .NET's invariant case folding instead of JavaScript's simple case folding.

## Tests

`src/vs/editor/test/common/model/textModelSearch.test.ts`, suite `TextModelSearch` (the rest is P5-63):

- [ ] Port: `parseSearchRequest invalid`, `parseSearchRequest non regex`, `parseSearchRequest regex`, `isMultilineRegexSource`,
      `isMultilineRegexSource correctly identifies multiline patterns`. `deepStrictEqual(actual.regex, /foo/giu)` becomes
      two asserts: `actual.Regex.ToString()` equals the JavaScript source (`"foo"`, `"foo\\\\n"`…) and `actual.Regex.Options`
      equals the mapped options (`g` and `u` map to nothing, `i` → `IgnoreCase`, `m` → `Multiline`, always `CultureInvariant`).
- [ ] New: `Searcher_EmptyMatchBeforeSurrogatePair_AdvancesByTwo`, `Searcher_SameNonEmptyMatchTwice_ReturnsNull`,
      `Searcher_WordSeparators_SkipsMatchInsideWord`, `Searcher_LookbehindSeesTextBeforeLastIndex`,
      `CreateRegExp_InvalidPattern_ThrowsArgumentException`, `EscapeRegExpCharacters_KeepsSpacesAndHash`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `common/model/textModelSearch.ts` (🔶 primitives, P5-62), `model.ts` (+ `FindMatch`,
      `SearchData`), `strings.ts` (+ `escapeRegExpCharacters`, `createRegExp`); the two behavior differences above
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`TextModelSearch` (P5-63), the piece tree search and `TextModel.FindMatches` (P5-64).

## Suggested commit message

`feat(core): add search parameters, search data and the regex searcher`
