# P6-10 — `MonospaceLineBreaksComputerFactory` and `CreateLineBreaks`

- **Status:** done
- **Depends on:** P6-09
- **Size:** M

## Goal

Given a line, its injected text, the tab size and the wrapping column, LoomEditor computes where the line wraps
(`ModelLineProjectionData`, or `null` when nothing wraps and nothing is injected), with Monaco's monospace algorithm. The
factory batches requests like Monaco's.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewModel/monospaceLineBreaksComputer.ts` | 15–58 | `MonospaceLineBreaksComputerFactory` (`create` from options, the constructor, `createLineBreaksComputer` with `addRequest` / `finalize`) |
| same | 102–103 | `arrPool1`, `arrPool2` (see decisions) |
| same | 359–519 | `createLineBreaks` |
| `src/vs/editor/test/common/viewModel/monospaceLineBreaksComputer.test.ts` | 13–90 | `parseAnnotatedText`, `toAnnotatedText`, `getLineBreakData`, `assertLineBreaks` (test helpers) |
| same | 96–136 | `MonospaceLineBreaksComputer` |
| same | 463–471 | `MonospaceLineBreaksComputer - CJK and Kinsoku Shori` |
| same | 592–653 | `WrappingIndent.Same`, `issue #16332`, `issue #35162`, `issue #75494` ×3, `WrappingIndent.DeepIndent`, `issue #33366`, `issue #152773`, `issue #112382`, the two CJK `Word break …` tests |
| same | 654–692 | `issue #258022: wrapOnEscapedLineFeeds: should work correctly after editor resize` |

## Files to create or change

- `src/LoomEditor.View/Layout/MonospaceLineBreaksComputer.cs` — create: `MonospaceLineBreaksComputerFactory : ILineBreaksComputerFactory`
  and `CreateLineBreaks` (in the `MonospaceLineBreaksComputer` static partial class of P6-09)
- `tests/LoomEditor.View.Tests/Layout/TestHelpers/LineBreaksTestHelpers.cs` — create (the four helpers)
- `tests/LoomEditor.View.Tests/Layout/MonospaceLineBreaksComputerTests.cs` — create

## Design decisions (already made — do not change)

- `MonospaceLineBreaksComputerFactory(string breakBeforeChars, string breakAfterChars)`; `static Create(EditorWrappingOptions options)`
  replaces Monaco's `create(IComputedEditorOptions)`.
- The computer returned by `CreateLineBreaksComputer` is a private nested class holding two `List`s; `Finalize()` returns an
  array. In this card `Finalize` **always** calls `CreateLineBreaks` (the `previousLineBreakData` fast path is P6-12: put a
  `// P6-12: createLineBreaksFromPreviousLineBreaks` comment where Monaco's `if` is, and keep the `isLineFeedWrappingEnabled`
  computation: `wrapOnEscapedLineFeeds && lineText.Contains('"') && lineText.Contains("\\n")`).
- `arrPool1` / `arrPool2` are a JavaScript allocation trick used only by `createLineBreaksFromPreviousLineBreaks`: do not port
  them here (P6-12 decides).
- `columnsForFullWidthChar = fontInfo.TypicalFullwidthCharacterWidth / fontInfo.TypicalHalfwidthCharacterWidth` (`double`).
- `createLineBreaks` builds growing `List<int>` for the offsets and converts to arrays for `ModelLineProjectionData`.
  `injectionOptions` = the `Options` of each `LineInjectedText`; `injectionOffsets` = `Column - 1`.
- The returned data uses `ModelDecorationInjectedTextOptions[]` (P6-07).
- Test helper `GetLineBreakData(factory, tabSize, breakAfter, columnsForFullWidthChar, wrappingIndent, wordBreak, wrapOnEscapedLineFeeds, text, previous, injectedText = null, useTwoCellFullwidthCharacters = false)`
  builds a `FontInfo` with the values of lines 49–66 (`FontInfo` record constructor of LoomEditor; `fontFeatureSettings` and
  `fontVariationSettings` do not exist: drop them). The clone of the previous data copies the two arrays.
- Each Monaco `test` becomes one `[Fact]` with the same assertions; `assert.strictEqual(actual, annotated)` →
  `Assert.Equal(annotatedText, actualAnnotatedText)`.

## Tests

- [x] Port: `Editor ViewModel - MonospaceLineBreaksComputer` › `MonospaceLineBreaksComputer`
- [x] Port: `MonospaceLineBreaksComputer - CJK and Kinsoku Shori`
- [x] Port: `MonospaceLineBreaksComputer - WrappingIndent.Same`, `issue #16332: Scroll bar overlaying on top of text`,
      `issue #35162: wrappingIndent not consistently working`, `issue #75494: surrogate pairs`, `… overrun 1`, `… overrun 2`,
      `MonospaceLineBreaksComputer - WrappingIndent.DeepIndent`, `issue #33366: …punctuation`, `issue #152773: …bracket followed by comma`,
      `issue #112382: …control characters`, `Word break work well with … CJK … normal`, `… keepAll`
- [x] Port: `issue #258022: wrapOnEscapedLineFeeds: should work correctly after editor resize` — it passes
      `previousLineBreakData`, but the factory skips the fast path for lines with escaped line feeds (Monaco line 46), so it
      runs `CreateLineBreaks` both times and passes with this card's factory.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` map: new row `editor/common/viewModel/monospaceLineBreaksComputer.ts` (🔶 P6-10, no previous-breaks fast path)
- [x] `docs/PORTING.md` "Tests ported in phase 6": row `monospaceLineBreaksComputer.test.ts` with the count so far
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The fixed-width injected text tests (P6-11), `createLineBreaksFromPreviousLineBreaks` and the incremental tests (P6-12:
`incremental 1`, `issue #95686`, `issue #110392`), the two-cell tests (P6-13), the DOM line-break computer (`wrappingStrategy: advanced`, not ported).

## Suggested commit message

`feat(view): compute monospace line breaks`
