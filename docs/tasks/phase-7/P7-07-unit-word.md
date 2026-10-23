# P7-07 — Text unit: word (and format)

- **Status:** todo
- **Depends on:** P7-06
- **Size:** M

## Goal

The `Word` unit (and `Format`, which maps to it) has its starts, built on Monaco's word character classes.

## Sources

None for the unit itself (specified in `docs/tasks/phase-7/README.md` §4, "Word tokens"). It uses `WordCharacterClassifier` (`src/LoomEditor.Core/Text/WordCharacterClassifier.cs`, port of
`src/vs/editor/common/core/wordCharacterClassifier.ts`) and `WordHelper.UsualWordSeparators` (`src/LoomEditor.Core/Text/WordHelper.cs`), the default separators of Monaco.

## Files to create or change

- `src/LoomEditor.View/Accessibility/WordUnitStarts.cs` — create
- `src/LoomEditor.View/Accessibility/AutomationUnitStarts.cs` — change: `Create` returns `WordUnitStarts` for `Word` and `Format`
- `tests/LoomEditor.View.Tests/Accessibility/WordUnitStartsTests.cs` — create
- `tests/LoomEditor.View.Tests/Accessibility/AutomationUnitStartsTests.cs` — change: remove `Create_Word_NotSupportedYet`; add `Create_Format_IsTheWordUnit`

## Design decisions (already made — do not change)

- `WordUnitStarts` holds one static `WordCharacterClassifier` created with `new WordCharacterClassifier(WordHelper.UsualWordSeparators, [])`.
- A private method `GetTokenStartColumns(string content)` returns the 1-based start columns of the tokens of a line (README §4): scan the UTF-16 units; classes from `classifier.Get(content[i])`
  (`Regular`, `Whitespace`, `WordSeparator`). Tokens: a start is recorded at `i` when (a) `i == 0`, or (b) the class at `i` is `Regular` or `WordSeparator` and the previous unit's class is **different** from the class at `i`
  (a change `Whitespace → Regular/WordSeparator`, `Regular → WordSeparator`, `WordSeparator → Regular`). A `Whitespace` unit never starts a token except at `i == 0`. An empty line has the single start column `1`.
  (So `"  x"` → starts `1`, `3`; `"foo  bar"` → `1`, `6`; `"foo.bar"` → `1`, `4`, `5`; `"a  "` → `1`; `"   "` → `1`.)
- Starts of the document: for each line its token start columns; the line break belongs to the last token (it is not a start), so a start never sits at `(L, maxColumn)` of a non-empty line.
  An empty line `L` has a start `(L, 1)` — also when it is the last line (consistent with `Line` and `Paragraph`).
- `StartAtOrBefore(p)`: the greatest token start column `<= p.Column` of line `p.LineNumber` (there always is one: column `1`); `p` at the end of a non-empty line belongs to the last token.
  `PreviousStart(p)`: the greatest token start `< p.Column` on the same line, else the **last** token start of the previous line (every line has at least one start), else `null` on line 1.
  `NextStartStrict(p)`: the smallest token start `> p.Column` on the same line, else `(L + 1, 1)` when `L < n`, else `null`.
- Surrogate pairs are two UTF-16 units of class `Regular` (no special case). CJK text is one `Regular` run (no `Intl.Segmenter`): say so in the XML doc.
- `Format` returns the same object type; no separate class.
- `internal sealed`, XML doc + `Threading` remark.

## Tests

- [ ] New (`WordUnitStartsTests`; `S`, `P`, `N` as in P7-06): `Starts_FooDotBar` (`"foo.bar"`: `N((1,1)) = (1,4)`, `N((1,4)) = (1,5)`, `N((1,5)) = null`),
      `Starts_TrailingWhitespaceBelongsToTheWord` (`"foo  bar"`: `N((1,1)) = (1,6)`, `S((1,5)) = (1,1)`),
      `Starts_LeadingWhitespaceIsAWord` (`"  x"`: `N((1,1)) = (1,3)`), `Starts_WhitespaceOnlyLine_IsOneWord` (`"   "`: `N((1,1)) = null`),
      `Starts_SeparatorRun_IsOneWord` (`"a//b"`: `N((1,1)) = (1,2)`, `N((1,2)) = (1,4)`), `Starts_EmptyLine_IsAWord` (`"a\n\nb"`: `N((1,1)) = (2,1)`, `N((2,1)) = (3,1)`),
      `S_AtTheEndOfALine_IsTheLastWord` (`"ab cd"`, `(1,6)` → `(1,4)`), `P_AtLineStart_IsTheLastWordOfThePreviousLine` (`"ab cd\nx"`, `(2,1)` → `(1,4)`),
      `P_AtDocumentStart_IsNull`, `N_InTheLastWord_IsNull`, `NS_InTheLastWord_IsDocumentEnd`, `Tab_IsWhitespace` (`"a\tb"`: `N((1,1)) = (1,3)`),
      `Digits_AreRegular` (`"abc123 x"`: `N((1,1)) = (1,8)`), `Underscore_IsRegular` (`"a_b c"`: `N((1,1)) = (1,5)` — `_` is not in `UsualWordSeparators`),
      `CjkRun_IsOneWord` (`"日本語 x"`: `N((1,1)) = (1,5)`), `EmptyDocument_StartIsDocumentStart_NextIsNull`
- [ ] New (`AutomationUnitStartsTests`): `Create_Format_IsTheWordUnit` (`Create(doc, Format)` and `Create(doc, Word)` give the same starts for a sample text)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Line and page units (P7-08), word segmentation by language, camelCase word parts.

## Suggested commit message

`feat(view): add the word text unit`
