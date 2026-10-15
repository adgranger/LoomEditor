# P3-01 — Strings: code points, surrogates, full-width and emoji helpers

- **Status:** todo
- **Depends on:** P2-23
- **Size:** M

## Goal

Port the helpers of Monaco's `base/common/strings.ts` that the cursor needs, **except** grapheme clusters (P3-02):
surrogate pairs, code point iteration, common prefix/suffix, full-width and emoji detection, leading whitespace, and
"how far does Backspace delete" (`getLeftDeleteOffset`). Read `docs/adr/0006-editing-and-text-input.md` (decision 7)
first.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/base/common/strings.ts`. Line numbers are at the pinned commit. If a function is not exactly at the
given line, search for its name: the **name** is authoritative.

| Lines | What to port |
|---|---|
| 259–261 | `splitLines` (split on `\r\n`, `\r`, `\n`) |
| 298–310 | `getLeadingWhitespace(str, start = 0, end = str.length)` |
| 312–320 | `lastNonWhitespaceIndex(str, startIndex = str.length - 1)` |
| 432–442 | `isAsciiDigit`, `isLowerAsciiLetter`, `isUpperAsciiLetter` |
| 466–498 | `commonPrefixLength`, `commonSuffixLength` |
| 503–547 | `isHighSurrogate`, `isLowSurrogate`, `computeCodePoint`, `getNextCodePoint`, `getPrevCodePoint` |
| 549–584 | `class CodePointIterator` |
| 704–764 | `isFullWidthCharacter`, `isEmojiImprecise` |
| 1168–1239 | `getLeftDeleteOffset`, `getOffsetBeforeLastEmojiComponent`, `isEmojiModifier`, `const enum CodePoint` |

## Files to create or change

- `src/LoomEditor.Core/Text/Strings.cs` — create: `internal static partial class Strings` (everything above except the iterator)
- `src/LoomEditor.Core/Text/Strings.Emoji.cs` — create (partial): `IsFullWidthCharacter`, `IsEmojiImprecise`, `GetLeftDeleteOffset`, `GetOffsetBeforeLastEmojiComponent`, `IsEmojiModifier`, the `CodePoint` constants
- `src/LoomEditor.Core/Text/CodePointIterator.cs` — create
- `tests/LoomEditor.Core.Tests/Text/StringsTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- Namespace `LoomEditor.Core.Text`. Everything is `internal`. Method names are Monaco's in PascalCase
  (`getNextCodePoint` → `GetNextCodePoint`).
- `charCode` parameters are `int` (a UTF-16 code unit as an `int`, like JavaScript). Code points are `int`.
- `getNextCodePoint(str, len, offset)` and `getPrevCodePoint(str, offset)` read `str[offset]` like `charCodeAt`.
  JavaScript returns `NaN` out of range; the C# callers never call them out of range, so add
  `Guard.Assert(offset >= 0 && offset < str.Length)` instead of returning a special value.
- `CodePointIterator` is an `internal sealed class` with `Offset` (get), `SetOffset(int)`, `Eol()` (`Offset >= str.Length`),
  `NextCodePoint()` and `PrevCodePoint()`, and the same constructor `(string str, int offset = 0)`. `GraphemeIterator`
  (P3-02) uses `SetOffset`.
- `splitLines` returns `string[]` and must give the same result as the JavaScript regex `/\r\n|\r|\n/`
  (do **not** use `string.Split(Environment.NewLine)`).
- `GetLeadingWhitespace` stops at the first character that is not `' '` or `'\t'` (Monaco tests `charCode !== Space && charCode !== Tab`).
  It returns `str.Substring(start, end - start)` when the whole range is whitespace.
- `LastNonWhitespaceIndex(string str, int startIndex)` — the existing `StringClassification.LastNonWhitespaceIndex(string)`
  in `LoomEditor.Core/Model/` stays as it is. Add the new overload in `Strings`, not in `StringClassification`.
- `const enum CodePoint` becomes `private const int` constants inside `Strings`, with Monaco's names in PascalCase:
  `Zwj = 0x200D`, `EmojiVariantSelector = 0xFE0F`, `EnclosingKeyCap = 0x20E3`, `Space = 0x0020`.
- `getOffsetBeforeLastEmojiComponent` returns `number | undefined` in Monaco: return `int?` in C#.

## Tests

- [ ] Port `strings.test.ts` › `lastNonWhitespaceIndex` (line 296), › `issue #115221: isEmojiImprecise misses ⭐` (317),
      › `isFullWidthCharacter` (322), › `getLeadingWhitespace` (392). Keep the `// Monaco: '<name>'` comment.
- [ ] New: `CodePointIterator` over `"a😀b"` returns `0x61, 0x1F600, 0x62` forwards and the reverse backwards, with
      offsets `1, 3, 4` / `3, 1, 0`
- [ ] New: `GetNextCodePoint` on a lone high surrogate at the end returns the surrogate itself
- [ ] New: `CommonPrefixLength("abcd", "abxy") == 2`, `CommonSuffixLength("xbcd", "abcd") == 3`
- [ ] New: `GetLeftDeleteOffset` — `"a👍🏽"` from the end deletes the whole emoji with its skin tone (offset 1);
      `"👨‍👩‍👧"` from the end returns the offset before the last component (`"👨‍👩‍"`, i.e. offset 5);
      `"ab"` from 2 returns 1; offset 0 returns 0
- [ ] New: `SplitLines("a\r\nb\rc\nd")` returns `["a", "b", "c", "d"]`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: new row `base/common/strings.ts` (partly: code points, emoji, leading whitespace — P3-01)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Grapheme clusters, `GraphemeIterator`, `nextCharLength`, `prevCharLength` (P3-02). Every other function of
`strings.ts` (format, escape, ANSI, ambiguous characters…).

## Suggested commit message

`feat(core): port code point and emoji helpers from strings.ts`
