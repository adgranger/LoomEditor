# P1-24 — Port tests: `editableTextModel.test.ts`, part 1

- **Status:** todo
- **Depends on:** P1-23
- **Size:** M (tests only, plus the bug fixes they reveal)

## Goal

Port the first half of Monaco's main `applyEdits` test file. If a test fails, fix production code — never the
expected values.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/test/common/model/editableTextModelTestUtils.ts` | 15–123 | `testApplyEditsWithSyncedModels`, `assertSyncedModels`, `assertLineMapping` |
| `src/vs/editor/test/common/model/editableTextModel.test.ts` | 17–63 | suite `…applyEdits updates mightContainRTL` (6 tests) |
| same | 65–107 | suite `…applyEdits updates mightContainNonBasicASCII` (5 tests) |
| same | 109–620 | suite `EditorModel - EditableTextModel.applyEdits`: the **first 23 tests**, from `high-low surrogates 1` (line 121) to `two unrelated edits` (line 598) included |

## Files to create or change

- `tests/LoomEditor.Core.Tests/Model/TestHelpers/EditableTextModelAssert.cs` — port of the three helpers.
  `MirrorTextModel` → our `MirrorText` (P1-22).
- `tests/LoomEditor.Core.Tests/Model/EditableTextModelMightContainTests.cs` — the two small suites
- `tests/LoomEditor.Core.Tests/Model/EditableTextModelApplyEditsTests.cs` — first 23 tests (P1-25 adds the rest to the same file)

## Design decisions (already made — do not change)

- Monaco's edit literal `{ range: new Range(...), text: 'x' }` → `TextEdit.Replace(new Range(...), "x")`;
  `text: null` → `TextEdit.Delete(...)`.
## Tests

- [ ] `…updates mightContainRTL` — 6 tests
- [ ] `…updates mightContainNonBasicASCII` — 5 tests
- [ ] `EditorModel - EditableTextModel.applyEdits` — 23 tests: `high-low surrogates 1`…`4`,
  `Bug 19872: Undo is funky`, `Bug 19872: Undo is funky (2)`, `insert empty text`, `last op is no-op`,
  `insert text without newline 1`, `… 2`, `insert one newline`, `insert text with one newline`,
  `insert text with two newlines`, `insert text with many newlines`, `insert multiple newlines`, `delete empty text`,
  `delete text from one line`, `delete text from one line 2`, `delete all text from a line`,
  `delete text from two lines`, `delete text from many lines`, `delete everything`, `two unrelated edits`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The rest of the file (P1-25).

## Suggested commit message

`test(core): port editable text model tests (part 1)`
