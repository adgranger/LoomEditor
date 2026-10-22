# P6-11 — Line breaks with fixed-width and width-only injected text (tests)

- **Status:** done
- **Depends on:** P6-10
- **Size:** S

## Goal

The thirteen Monaco tests about injected text that has a fixed width (`widthInEm`) or only reserves space pass against
`CreateLineBreaks`. This is a test card: production code changes only if a test reveals a porting bug in P6-09 / P6-10.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/test/common/viewModel/monospaceLineBreaksComputer.test.ts`.

| Lines | What to port |
|---|---|
| 138–151 | `accounts for fixed injected text width when wrapping` |
| 153–166 | `treats multi-character fixed-width injected text as an atomic span` |
| 168–175 | `wraps after an escaped line feed followed by fixed-width injected text` |
| 177–191 | `treats adjacent fixed-width injected texts as separate atomic spans` |
| 193–206 | `keeps fixed-width injected text wider than the wrap column atomic` |
| 208–224 | `wraps before a tab that no longer fits after fixed-width injected text` |
| 226–244 | `expands a tab following fixed-width injected text from the character column` |
| 246–261 | `reserves width for a spacing-only injection at the beginning of a line` |
| 263–272 | `does not create empty lines for oversized spacing-only injected text` |
| 274–291 | `lets a spacing-only injection in the middle of a line inherit surrounding break opportunities` |
| 293–308 | `ignores a spacing-only injection at the end of a line` |
| 310–326 | `expands a tab following a spacing-only injection from the character column` |
| 328–341 | `does not use fixed-width injected whitespace as continuation indentation` |

## Files to create or change

- `tests/LoomEditor.View.Tests/Layout/MonospaceLineBreaksComputerInjectedTextTests.cs` — create
- `tests/LoomEditor.View.Tests/Layout/TestHelpers/LineBreaksTestHelpers.cs` — change if a helper is missing
- production files of P6-09 / P6-10 — only to fix a bug a test reveals (say which in the commit body)

## Design decisions (already made — do not change)

- `new LineInjectedText(0, 1, 5, { content: '\xa0', widthInEm: 1 }, 0)` →
  `new LineInjectedText(0, 1, 5, ModelDecorationInjectedTextOptions.From(new InjectedTextOptions { Content = " ", WidthInEm = 1 }), 0)`;
  add a `Injected(int column, string content, double? widthInEm = null, int order = 0)` helper to the test helpers to keep the
  tests short.
- `assert.deepStrictEqual({ breakOffsets, breakOffsetsVisibleColumn }, {...})` → two `Assert.Equal` on the arrays
  (`BreakOffsetsVisibleColumn` is `double[]`: compare with `double[]` literals).
- A test that expects `null` data asserts `Assert.Null`.
- Keep each Monaco test as one `[Fact]` with its `// Monaco:` comment.

## Tests

- [x] Port: the 13 tests listed above (`Editor ViewModel - MonospaceLineBreaksComputer` suite)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` "Tests ported in phase 6": the `monospaceLineBreaksComputer.test.ts` count is updated
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Rendering injected text (P6-25).

## Suggested commit message

`test(view): port the fixed-width injected text line break tests`
