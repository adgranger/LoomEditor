# P7-10 — Find text in a range

- **Status:** done
- **Depends on:** P7-09
- **Size:** S

## Goal

`AutomationTextRange.FindText` finds a string inside a range, forward or backward, with or without case, as `ITextRangeProvider.FindText` requires.

## Sources

None (specified in `docs/tasks/phase-7/README.md` §4, `FindText`). The editor's own find (`LoomEditor.Core/Model/Search/`) is **not** used: UIA wants a plain ordinal search inside the range.

## Files to create or change

- `src/LoomEditor.View/Accessibility/AutomationTextDocument.cs` — change: `Position PositionAfter(Position start, int textOffset)`
- `src/LoomEditor.View/Accessibility/AutomationTextRange.cs` (or its `partial` navigation file) — change: `AutomationTextRange? FindText(string text, bool backward, bool ignoreCase)`
- `tests/LoomEditor.View.Tests/Accessibility/AutomationTextRangeFindTests.cs` — create
- `tests/LoomEditor.View.Tests/Accessibility/AutomationTextDocumentTests.cs` — change: tests of `PositionAfter`

## Design decisions (already made — do not change)

- `PositionAfter(start, textOffset)`: the position reached by walking `textOffset` UTF-16 units of the **LF-joined** text from `start` (a line break counts as **one** unit, whatever the model's EOL). Algorithm: validate `start`;
  `line = start.LineNumber`, `col = start.Column`, `remaining = textOffset`; loop: `available = GetLineMaxColumn(line) - col`; if `remaining <= available` return `(line, col + remaining)`; else if `line == LineCount` return `DocumentEnd`
  (offset past the end: clamp); else `remaining -= available + 1` (the line break), `line++`, `col = 1`. `textOffset < 0` → `ArgumentOutOfRangeException`.
- `FindText`: `text` null → `ArgumentNullException`; `text.Length == 0` → returns `null`. `content = GetText()` (the whole range, no `maxLength`), `comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal`,
  `index = backward ? content.LastIndexOf(text, comparison) : content.IndexOf(text, comparison)`. `-1` → `null`. Otherwise the result is a **new** range `[PositionAfter(Range.Start, index), PositionAfter(Range.Start, index + text.Length))`
  of the same document. It does not change `this`.
- A match that overlaps a line break works because the text is LF-joined (`"a\nb"` is found in a model whose lines are `a`, `b`); a search text containing `\r\n` finds nothing (the document text has no `\r`).
- No culture-sensitive comparison anywhere; no regular expressions.
- A large range allocates its text once; that is accepted (UIA callers use it on a line or a visible page).

## Tests

- [x] New (`PositionAfter`): `PositionAfter_InsideTheLine`, `PositionAfter_AcrossTheLineBreak` (`"ab\ncd"`, from `(1,2)` offset 2 → `(2,1)`), `PositionAfter_CrlfModelCountsTheBreakAsOne` (model `"ab\r\ncd"`, from `(1,1)` offset 3 → `(2,1)`),
      `PositionAfter_PastTheEnd_IsDocumentEnd`, `PositionAfter_ZeroOffset_IsTheStart`, `PositionAfter_NegativeOffset_Throws`
- [x] New (`FindText`): `FindText_Forward_ReturnsTheFirstMatch` (`"abc abc"`, `"abc"` → `[(1,1),(1,4))`), `FindText_Backward_ReturnsTheLastMatch` (→ `[(1,5),(1,8))`),
      `FindText_IgnoreCase`, `FindText_CaseSensitive_NoMatch_ReturnsNull`, `FindText_Empty_ReturnsNull`, `FindText_OnlyInsideTheRange` (range `[(1,1),(1,4))` of `"abc abc"` does not see the second match),
      `FindText_AcrossLines` (`"a\nb"`, text `"a\nb"` → the whole document), `FindText_Crlf_SearchTextWithCarriageReturn_ReturnsNull`, `FindText_DoesNotChangeTheRange`,
      `FindText_Unicode_Ordinal` (`"日本語"`), `FindText_SurrogatePair` (`"a😀b"`, `"😀"` → `[(1,2),(1,4))`), `FindText_Null_Throws`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`FindAttribute` (answered in the WinUI card P7-17: attributes are uniform), regex and whole-word options.

## Suggested commit message

`feat(view): add FindText to automation text ranges`
