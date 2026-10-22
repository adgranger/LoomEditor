# P6-09 — Line-break computer interfaces, the wrapping character classifier and width helpers

- **Status:** todo
- **Depends on:** P6-08
- **Size:** S

## Goal

The building blocks of Monaco's monospace line-break computer exist: the `ILineBreaksComputer*` interfaces, the
`WrappingCharacterClassifier` (Kinsoku Shori classes), `CanBreak`, the column and pixel width helpers and
`ComputeWrappedTextIndentLength`. P6-10 assembles them into `CreateLineBreaks`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/modelLineProjectionData.ts` | 331–346 | `ILineBreaksComputerContext`, `ILineBreaksComputerFactory`, `ILineBreaksComputer` |
| `src/vs/editor/common/viewModel/monospaceLineBreaksComputer.ts` | 60–100 | `CharacterClass`, `WrappingCharacterClassifier` (with the CJK ranges of `get`) |
| same | 521–549 | `computeCharWidth`, `fullWidthCharacterColumns`, `tabCharacterWidth` |
| same | 551–582 | `computeCharPixelWidth`, `computeFixedWidthRangeColumnWidth` |
| same | 583–600 | `isEscapedLineBreakAtPosition` |
| same | 602–612 | `canBreak` |
| same | 614–643 | `computeWrappedTextIndentLength` |

## Files to create or change

- `src/LoomEditor.View/Layout/ILineBreaksComputer.cs` — create (header `modelLineProjectionData.ts`; the three interfaces)
- `src/LoomEditor.View/Layout/MonospaceLineBreaksComputer.Helpers.cs` — create (header `monospaceLineBreaksComputer.ts`):
  `internal static partial class MonospaceLineBreaksComputer` with the helpers, plus `CharacterClass` and
  `WrappingCharacterClassifier` (nested or file-local `internal` types in the same file)
- `tests/LoomEditor.View.Tests/Layout/MonospaceLineBreaksComputerHelpersTests.cs` — create

## Design decisions (already made — do not change)

- Interfaces:
  - `internal interface ILineBreaksComputerContext { string GetLineContent(int lineNumber); IReadOnlyList<LineInjectedText>? GetLineInjectedText(int lineNumber); }`
  - `internal interface ILineBreaksComputerFactory { ILineBreaksComputer CreateLineBreaksComputer(ILineBreaksComputerContext context, FontInfo fontInfo, int tabSize, int wrappingColumn, WrappingIndent wrappingIndent, WordBreak wordBreak, bool wrapOnEscapedLineFeeds, bool useTwoCellFullwidthCharacters); }`
  - `internal interface ILineBreaksComputer { void AddRequest(int lineNumber, ModelLineProjectionData? previousLineBreakData); ModelLineProjectionData?[] Finalize(); }`
- `WrappingCharacterClassifier : CharacterClassifier<CharacterClass>` (Core's `LoomEditor.Core.Text.CharacterClassifier<T>`;
  if its ASCII map or `Get` are not overridable/accessible, make `Get` `virtual` and the ASCII map `protected` there — the only
  allowed change to Core in this card). `CharacterClass` is `internal enum CharacterClass : byte { None, BreakBefore, BreakAfter, BreakIdeographic }`.
- The helpers are `internal static` so the tests (and P6-10/P6-12) can call them; `isFullWidthCharacter` →
  `Strings.IsFullWidthCharacter`, `firstNonWhitespaceIndex` → `StringClassification.FirstNonWhitespaceIndex`, `isHighSurrogate`
  → `Strings.IsHighSurrogate`.
- `CharCode.Tab` → `'\t'`, `CharCode.Space` → `' '`, `CharCode.Null` → `0`.
- Widths in pixels are `double` (Monaco numbers); column widths are `int` except `columnsForFullWidthChar`, which is a
  `double` (it is `typicalFullwidthCharacterWidth / typicalHalfwidthCharacterWidth`, not always 2). So `ComputeCharWidth`
  and `ComputeFixedWidthRangeColumnWidth` return `double`, and the algorithms of P6-10 / P6-12 keep `visibleColumn` as a
  `double`, stored as is in `ModelLineProjectionData.BreakOffsetsVisibleColumn` (`double[]`, P6-07). `tabCharacterWidth`
  takes and returns `double` too (`tabSize - (visibleColumn % tabSize)`, C# `%` on doubles matches JavaScript).
  `ComputeWrappedTextIndentLength` returns `int` (it only adds tab widths of integral columns and 1s: cast the tab width with
  `(int)`, it is integral there); its `columnsForFullWidthChar` parameter is `double`.
- `fixedWidthRanges?.[0]?.startOffset === i` → `fixedWidthRanges is { Count: > 0 } && fixedWidthRanges[0].StartOffset == i`.

## Tests

No upstream test calls these helpers directly. New:

- [ ] `Classifier_BreakBeforeAndAfterCharacters_AreClassified`
- [ ] `Classifier_CjkRanges_AreIdeographic` (`0x3040`, `0x4E00`, `0x9FFF`; `0xA000` is `None`)
- [ ] `CanBreak_KinsokuShori_Cases` (theory: after `BreakAfter`, before `BreakBefore`, ideographic with and without keep-all, never before a space)
- [ ] `ComputeCharWidth_Tab_FullWidth_Control_Normal`
- [ ] `ComputeCharPixelWidth_TwoCells_UsesTwoHalfwidths`
- [ ] `IsEscapedLineBreakAtPosition_OddBackslashes_True_EvenFalse`
- [ ] `ComputeWrappedTextIndentLength_None_Same_Indent_DeepIndent` (theory with `"\t  x"`, tab size 4)
- [ ] `ComputeWrappedTextIndentLength_IndentTooWide_FallsBackToZero`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`createLineBreaks` and the factory (P6-10), `createLineBreaksFromPreviousLineBreaks` (P6-12).

## Suggested commit message

`feat(view): add the line-break computer building blocks`
