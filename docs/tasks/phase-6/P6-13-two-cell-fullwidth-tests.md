# P6-13 — Line breaks with two-cell full-width characters (tests)

- **Status:** done
- **Depends on:** P6-12
- **Size:** S

## Goal

The six Monaco tests of `useTwoCellFullwidthCharacters` pass, which closes `monospaceLineBreaksComputer.test.ts`. Test card:
production code changes only to fix a bug a test reveals.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/test/common/viewModel/monospaceLineBreaksComputer.test.ts`.

| Lines | What to port |
|---|---|
| 473–487 | `MonospaceLineBreaksComputer - two-cell full-width character width` |
| 489–506 | `… two-cell width determines whether continuation indentation fits` |
| 508–525 | `… two-cell width uses shared full-width classification` (`Ａ`, Vai `ꔀ`) |
| 527–544 | `… two-cell width preserves natural control-character metrics` |
| 546–558 | `… two-cell width treats combining marks independently` |
| 560–590 | `… two-cell width supports incremental line breaks` (needs P6-12's `AssertIncrementalLineBreaks`) |

## Files to create or change

- `tests/LoomEditor.View.Tests/Layout/MonospaceLineBreaksComputerTwoCellTests.cs` — create
- production files of P6-09 / P6-10 / P6-12, or `Strings.IsFullWidthCharacter` in Core — only to fix a bug a test reveals

## Design decisions (already made — do not change)

- Each `assert.deepStrictEqual({a: …, b: …}, {a: …, b: …})` becomes one `Assert.Equal` per key, in the same order, with the
  key name in a comment (`// lyingFontTwoCells`).
- `'あ'.repeat(4)` → `new string('あ', 4)`.
- The `?.wrappedTextIndentLength` / `?.breakOffsetsVisibleColumn` of a `null` result: Monaco would get `undefined`; here
  assert the data is not null first (the expected values are never `undefined`).
- If `uses shared full-width classification` fails because `Strings.IsFullWidthCharacter` (Core, ported in phase 3) does not
  classify `ꔀ` like Monaco's `isFullWidthCharacter` (`src/vs/base/common/strings.ts`, read it at the pinned commit),
  fix Core to match upstream and add a Core test.

## Tests

- [x] Port: the 6 tests listed above

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md` "Tests ported in phase 6": `monospaceLineBreaksComputer.test.ts` is complete (all upstream tests ported)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Any option or UI for two-cell characters (it is the `EditorWrappingOptions.UseTwoCellFullwidthCharacters` boolean).

## Suggested commit message

`test(view): port the two-cell full-width line break tests`
