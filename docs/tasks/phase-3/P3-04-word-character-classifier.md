# P3-04 — `CharacterClassifier` and `WordCharacterClassifier`

- **Status:** done
- **Depends on:** P3-03
- **Size:** S

## Goal

Classify characters as *regular*, *whitespace* or *word separator*, the way Monaco does for Ctrl+← / Ctrl+→,
Ctrl+Backspace and double-click. The word separators are the `wordSeparators` editor option
(``USUAL_WORD_SEPARATORS = '`~!@#$%^&*()-=+[{]}\|;:'",.<>/?'``).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

If a function is not exactly at the given line, search for its name.

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/core/characterClassifier.ts` | 11–60 | `class CharacterClassifier<T>` (256-entry ASCII array + a map for other characters) |
| same | 67–86 | `class CharacterSet` |
| `src/vs/editor/common/core/wordCharacterClassifier.ts` | 12–16 | `const enum WordCharacterClass` |
| same | 18–96 | `class WordCharacterClassifier` (constructor; the `Intl` methods become stubs, see below) |
| same | 102–112 | `getMapForWordSeparators` (LRU cache of 10) |
| `src/vs/editor/common/core/wordHelper.ts` | 10, 15–28 | `USUAL_WORD_SEPARATORS`, `IWordAtPosition` |
| `src/vs/editor/test/common/core/characterClassifier.test.ts` | 14 | test `works` |

## Files to create or change

- `src/LoomEditor.Core/Text/CharacterClassifier.cs` — create: `internal class CharacterClassifier<T> where T : struct`
- `src/LoomEditor.Core/Text/CharacterSet.cs` — create
- `src/LoomEditor.Core/Text/WordCharacterClassifier.cs` — create: `WordCharacterClass`, `WordCharacterClassifier`, `WordCharacterClassifierCache`
- `src/LoomEditor.Core/Text/WordHelper.cs` — create: `internal static class WordHelper { public const string UsualWordSeparators = "…"; }` and `internal readonly record struct WordAtPosition(string Word, int StartColumn, int EndColumn)` (Monaco: `IWordAtPosition`)
- `tests/LoomEditor.Core.Tests/Text/CharacterClassifierTests.cs`, `WordCharacterClassifierTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `CharacterClassifier<T>`: `T Get(int charCode)`, `void Set(int charCode, T value)`, constructor `(T defaultValue)`.
  Codes `0..255` use a `T[]`; others use a `Dictionary<int, T>`. `Get` on a negative code returns the default value
  (Monaco's `charCodeAt` can be `NaN`, which falls back to the default; a negative code is the C# equivalent; callers
  pass `-1` when Monaco reads past the string).
- `WordCharacterClassifier : CharacterClassifier<WordCharacterClass>`, constructor
  `(string wordSeparators, IReadOnlyList<string> intlSegmenterLocales)`. It sets each UTF-16 unit of
  `wordSeparators` to `WordSeparator`, then `' '` and `'\t'` to `Whitespace` (this order matters: a space in
  `wordSeparators` ends up as `Whitespace`).
- **Intl segmentation is not ported** (no Native AOT-friendly equivalent of `Intl.Segmenter`).
  `FindPrevIntlWordBeforeOrAtOffset` and `FindNextIntlWordAtOrAfterOffset` exist and always return `null`.
  Their return type is `IntlWordSegment?` with `internal readonly record struct IntlWordSegment(string Segment, int Index)`.
  `IntlSegmenterLocales` is kept so that the word operations can be ported unchanged. Document this in the XML doc
  and in `docs/PORTING.md` → "Behavior differences".
- `getMapForWordSeparators` becomes `WordCharacterClassifierCache.Get(string wordSeparators, IReadOnlyList<string> locales)`,
  an LRU of 10 entries keyed by `$"{wordSeparators}/{string.Join(',', locales)}"`. It is **not thread-safe**
  (UI thread only, like Monaco); say so in the threading note. A `WordCharacterClassifier` is immutable after
  construction and can be shared.
- `WordHelper.UsualWordSeparators` is exactly the string of `wordHelper.ts` line 10. Copy it from the source; check
  the backslash and the quotes with a test.

## Tests

- [x] Port `characterClassifier.test.ts` › `works` (`// Monaco: 'works'`)
- [x] New: `new WordCharacterClassifier(WordHelper.UsualWordSeparators, [])` classifies `'a'` and `'é'` and `'中'` as
      `Regular`, `'.'` and `'('` as `WordSeparator`, `' '` and `'\t'` as `Whitespace`
- [x] New: a separator string that contains a space still classifies `' '` as `Whitespace`
- [x] New: `WordCharacterClassifierCache.Get` returns the same instance for the same key and a new one after 10 other keys
- [x] New: `WordHelper.UsualWordSeparators` equals ``"`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?"`` (C# literal), its length
      is 31, and it contains exactly one backslash

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map rows for `characterClassifier.ts`, `wordCharacterClassifier.ts`, `wordHelper.ts` (constants
      only); behavior difference "`wordSegmenterLocales` is ignored (no `Intl.Segmenter`)"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`getWordAtText` and the word regular expressions of `wordHelper.ts` (phase 5); the word operations themselves (P3-29).

## Suggested commit message

`feat(core): port the word character classifier`
