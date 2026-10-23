# P7-05 — `AutomationTextDocument` and `AutomationTextRange` (text, compare, validate)

- **Status:** todo
- **Depends on:** P7-04
- **Size:** M

## Goal

A UI-free model of what UI Automation sees: a document (the model text as `\n`-joined lines) and mutable ranges over it that can be cloned, compared
and read. Units and movement come in the next cards.

## Sources

None (specified in `docs/tasks/phase-7/README.md` §4). Monaco's closest relative is the screen reader's `ISimpleModel`
(`src/vs/editor/common/viewModel/screenReaderSimpleModel.ts`, 8 lines) that reads text in `EndOfLinePreference.LF`.

## Files to create or change

- `src/LoomEditor.View/Accessibility/AutomationTextUnit.cs` — create (enum `AutomationTextUnit`: `Character = 0`, `Format = 1`, `Word = 2`, `Line = 3`, `Paragraph = 4`, `Page = 5`, `Document = 6`: the values of UIA's `TextUnit`)
- `src/LoomEditor.View/Accessibility/AutomationTextEndpoint.cs` — create (enum `AutomationTextEndpoint`: `Start = 0`, `End = 1`: UIA's `TextPatternRangeEndpoint`)
- `src/LoomEditor.View/Accessibility/AutomationTextDocument.cs` — create
- `src/LoomEditor.View/Accessibility/AutomationTextRange.cs` — create
- `src/LoomEditor.View/Accessibility/README.md` — create (what the folder is, the two classes, a pointer to the phase 7 README §4)
- `tests/LoomEditor.View.Tests/Accessibility/TestHelpers/AutomationTestHelpers.cs` — create
- `tests/LoomEditor.View.Tests/Accessibility/AutomationTextDocumentTests.cs`, `AutomationTextRangeTests.cs` — create

## Design decisions (already made — do not change)

- `AutomationTextDocument` is a `sealed class` created from a `ViewModel` (`new AutomationTextDocument(ViewModel viewModel)`), exposing `ViewModel ViewModel`, `TextModel Model`,
  `Position DocumentStart` (`(1, 1)`), `Position DocumentEnd` (`(LineCount, GetLineMaxColumn(LineCount))`, read from the model each time), `Range DocumentRange`,
  `Range Validate(Range range)` (`Model.ValidateRange`), `Position Validate(Position)` (`Model.ValidatePosition`) and
  `string GetText(Range range, int maxLength = -1)`.
- `GetText`: validates `range`, then `Model.GetValueInRange(range, EndOfLinePreference.LF)`. `maxLength == -1` → the whole text; `maxLength < -1` → `ArgumentOutOfRangeException`; `maxLength >= 0` →
  at most `maxLength` UTF-16 units, and if the cut would fall between a high and a low surrogate, the high surrogate is dropped too. To avoid reading a whole huge document for a small `maxLength`,
  when `maxLength >= 0` clamp the end of the range first: read `Model.GetValueInRange` for the range cut after `maxLength + 1` characters (use `Model.ModifyPosition(range.Start, maxLength + 1)` if it exists, or read in
  full: correctness first, optimization is out of scope).
- `AutomationTextRange` is a `sealed class` (UIA ranges are mutated by `Move*`): `new AutomationTextRange(AutomationTextDocument document, Range range)` (validates), `Document`, a `Range Range` getter that
  **re-validates** the stored range against the model on every read and stores the result, a `Range`-setting internal method `Set(Range)` used by the next cards (validates), `AutomationTextRange Clone()`,
  `bool Compare(AutomationTextRange other)` (same document and equal `Start` and `End`; a different document → `false`), `int CompareEndpoints(AutomationTextEndpoint endpoint, AutomationTextRange other, AutomationTextEndpoint otherEndpoint)`
  (`-1`, `0`, `1` from `Position.CompareTo`; `other` of another document → `ArgumentException`), `string GetText(int maxLength = -1)`, `bool IsDegenerate`, and the static `AutomationTextRange.ForDocument(AutomationTextDocument)`.
- Null arguments throw `ArgumentNullException` (`ArgumentNullException.ThrowIfNull`).
- `AutomationTestHelpers.WithDocument(string text, Action<AutomationTextDocument> test, TestCodeEditorOptions? options = null)` calls `TestCodeEditor.WithTestCodeEditor(text, editor => test(new AutomationTextDocument(editor.ViewModel)), options)`;
  `Pos(int line, int column)` and `Rng(int sl, int sc, int el, int ec)` are one-line factories. Add the row to the README §5 table only if the name differs from the one already listed there.
- Everything `internal`. XML doc + `Threading: UI thread only` remark on both classes.

## Tests

- [ ] New (`AutomationTextDocumentTests`): `DocumentEnd_MultiLineText_IsTheEndOfTheLastLine`, `DocumentEnd_EmptyModel_IsDocumentStart`, `DocumentRange_CoversTheWholeModel`,
      `GetText_CrlfModel_UsesLineFeeds` (a model created with `"a\r\nb"` gives `"a\nb"`), `GetText_RangeAcrossLines_JoinsWithLineFeed`,
      `GetText_RangeOutsideTheModel_IsClampedToTheModel`, `GetText_MaxLength_TruncatesInUtf16Units`, `GetText_MaxLengthZero_ReturnsEmpty`,
      `GetText_MaxLengthCutsASurrogatePair_DropsTheHighSurrogate` (text `"a😀b"`, `maxLength: 2` → `"a"`), `GetText_MaxLengthMinusOne_ReturnsEverything`,
      `GetText_MaxLengthBelowMinusOne_Throws`
- [ ] New (`AutomationTextRangeTests`): `ForDocument_IsTheDocumentRange`, `Clone_IsEqualButIndependent` (after `Set` on the clone the original is unchanged), `Compare_SameEndpoints_ReturnsTrue`,
      `Compare_DifferentEndpoints_ReturnsFalse`, `Compare_DifferentDocuments_ReturnsFalse`, `CompareEndpoints_StartAgainstStart_Signs` (before / equal / after),
      `CompareEndpoints_EndAgainstStart_Signs`, `CompareEndpoints_OtherDocument_Throws`, `Range_AfterTheModelShrinks_IsClamped` (a range on line 5, then the model is replaced by one line:
      `editor.Model.PushEditOperations` or `SetValue`: use what existing tests of `TextModel` use; the range is clamped, no exception), `IsDegenerate_StartEqualsEnd`, `GetText_ReadsTheRange`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Units (P7-06 …), `Move`, `FindText`, geometry, anything WinUI.

## Suggested commit message

`feat(view): add the automation text document and ranges`
