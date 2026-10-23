# P7-06 — Text units: character, paragraph, document

- **Status:** done
- **Depends on:** P7-05
- **Size:** M

## Goal

The "starts" of the units `Character`, `Paragraph` and `Document` are computed (`StartAtOrBefore`, `PreviousStart`, `NextStart`), through a common base
class that the word, line and page units extend in the next cards.

## Sources

None (specified in `docs/tasks/phase-7/README.md` §4). Grapheme helpers: `LoomEditor.Core.Text.Strings.NextCharLength`, `PrevCharLength`, `GetCharContainingOffset`
(`src/LoomEditor.Core/Text/Strings.Graphemes.cs`; Monaco `base/common/strings.ts`, ADR 0006 decision 7).

## Files to create or change

- `src/LoomEditor.View/Accessibility/AutomationUnitStarts.cs` — create (abstract base + the factory)
- `src/LoomEditor.View/Accessibility/CharacterUnitStarts.cs`, `ParagraphUnitStarts.cs`, `DocumentUnitStarts.cs` — create
- `tests/LoomEditor.View.Tests/Accessibility/CharacterUnitStartsTests.cs`, `ParagraphUnitStartsTests.cs`, `DocumentUnitStartsTests.cs` — create

## Design decisions (already made — do not change)

- ```csharp
  internal abstract class AutomationUnitStarts
  {
      protected AutomationUnitStarts(AutomationTextDocument document) { Document = document; }
      public AutomationTextDocument Document { get; }
      public abstract Position StartAtOrBefore(Position position);   // greatest start <= position (always exists: DocumentStart is a start)
      public abstract Position? PreviousStart(Position position);    // greatest start < position, null when none
      public abstract Position? NextStartStrict(Position position);  // smallest start > position, null when none
      public Position NextStart(Position position) => NextStartStrict(position) ?? Document.DocumentEnd;   // README §4
      public static AutomationUnitStarts Create(AutomationTextDocument document, AutomationTextUnit unit);
  }
  ```
  Every method validates its argument first (`Document.Validate`). `Create` returns `CharacterUnitStarts`, `ParagraphUnitStarts`, `DocumentUnitStarts` for those units and throws
  `NotSupportedException($"The {unit} unit is added by a later card.")` for `Format`, `Word`, `Line`, `Page` (P7-07 and P7-08 replace the throw, `Format` maps to the word unit there).
  An undefined enum value throws `ArgumentOutOfRangeException`.
- **Character** (all positions are validated; `content` = the line's text, `n` = line count, `L`, `col` = the position, `offset = col - 1`):
  - a helper `FirstStartOfLine(L)`: `(L, 1)` when `content` is not empty **or** `L < n` (the line break is a character), otherwise `null`;
  - `StartAtOrBefore`: if `offset < content.Length` → `(L, GetCharContainingOffset(content, offset).Start + 1)`; else (`offset == content.Length`): if `L < n` → the position itself (the line break);
    else (last line, at the end): if `content.Length > 0` → `(L, content.Length - PrevCharLength(content, content.Length) + 1)`; else if `L > 1` → `(L - 1, GetLineMaxColumn(L - 1))` (the last line break); else `DocumentStart`;
  - `NextStartStrict`: if `offset < content.Length`: `next = GetCharContainingOffset(content, offset).End`; if `next < content.Length` → `(L, next + 1)`; else if `L < n` → `(L, content.Length + 1)` (the line break); else `null`.
    If `offset == content.Length` (at the end of the line): `L < n ? FirstStartOfLine(L + 1) : null`; and when `FirstStartOfLine(L + 1)` is `null` (an empty last line) the result is `null`;
  - `PreviousStart`: if `offset > 0` → `(L, offset - PrevCharLength(content, offset) + 1)` — but if `offset` is inside a grapheme, first move to its start with `GetCharContainingOffset(...).Start`
    and take the previous grapheme of that; else if `L > 1` → `(L - 1, GetLineMaxColumn(L - 1))` (the line break before this line); else `null`.
- **Paragraph**: `StartAtOrBefore = (L, 1)`; `PreviousStart = col > 1 ? (L, 1) : L > 1 ? (L - 1, 1) : null`; `NextStartStrict = L < n ? (L + 1, 1) : null`.
- **Document**: `StartAtOrBefore = DocumentStart`; `PreviousStart = position > DocumentStart ? DocumentStart : null`; `NextStartStrict = null`.
- The classes are `internal sealed` (except the base). XML doc + `Threading` remark. No allocation per call beyond the line string.

## Tests

(Use `AutomationTestHelpers.WithDocument`; `S` = `StartAtOrBefore`, `P` = `PreviousStart`, `N` = `NextStartStrict`, `NS` = `NextStart`)

- [x] New (`CharacterUnitStartsTests`): `S_InsideALine_IsThePositionItself` (`"abc"`, `(1,2)` → `(1,2)`), `N_InsideALine_IsTheNextColumn`, `P_InsideALine_IsThePreviousColumn`,
      `N_LastCharacterOfALine_IsTheLineBreak` (`"ab\ncd"`, `(1,2)` → `(1,3)`), `N_AtTheLineBreak_IsTheNextLineStart` (`(1,3)` → `(2,1)`), `P_AtLineStart_IsThePreviousLineBreak` (`(2,1)` → `(1,3)`),
      `S_AtTheEndOfTheLastLine_IsTheLastCharacter` (`"ab\ncd"`, `(2,3)` → `(2,2)`), `N_AtTheEndOfTheLastLine_IsNull`, `NS_AtTheEndOfTheLastLine_IsDocumentEnd`,
      `P_AtDocumentStart_IsNull`, `S_AtDocumentStart_IsDocumentStart`, `EmptyDocument_StartIsDocumentStart_NextIsNull`,
      `EmptyLineInTheMiddle_IsOneCharacter` (`"a\n\nb"`: `N((1,2)) = (2,1)`, `N((2,1)) = (3,1)`), `TrailingEmptyLastLine` (`"a\n"`: `N((1,2)) = null` because `(2,1)` is an empty last line;
      `S((2,1)) = (1,2)`),
      `SurrogatePair_IsOneCharacter` (`"a😀b"`: `N((1,2)) = (1,4)`, `P((1,4)) = (1,2)`, `S((1,3)) = (1,2)`),
      `Grapheme_CombiningMark_IsOneCharacter` (`"éx"`: `N((1,1)) = (1,3)`), `Crlf_Model_HasNoSeparateCarriageReturn` (model `"a\r\nb"`: `N((1,1)) = (1,2)`, `N((1,2)) = (2,1)`)
- [x] New (`ParagraphUnitStartsTests`): `S_IsTheLineStart`, `P_MidLine_IsTheLineStart`, `P_AtLineStart_IsThePreviousLineStart`, `P_AtDocumentStart_IsNull`, `N_IsTheNextLineStart`,
      `N_OnTheLastLine_IsNull`, `NS_OnTheLastLine_IsDocumentEnd`, `WrappedLine_IsOneParagraph` (word wrap on, a wrapped model line: `N` of a position in its second view line is the next *model* line)
- [x] New (`DocumentUnitStartsTests`): `S_Anywhere_IsDocumentStart`, `P_AtDocumentStart_IsNull`, `P_Elsewhere_IsDocumentStart`, `N_Anywhere_IsNull`, `NS_Anywhere_IsDocumentEnd`
- [x] New (`AutomationUnitStartsTests`): `Create_Word_NotSupportedYet` (removed by P7-07), `Create_UndefinedUnit_Throws`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Word, line and page units (P7-07, P7-08), expanding and moving ranges (P7-09).

## Suggested commit message

`feat(view): add the character, paragraph and document text units`
