# P1-25 — Port tests: `editableTextModel.test.ts`, part 2, and auto tests

- **Status:** done
- **Depends on:** P1-24
- **Size:** M (tests only, plus the bug fixes they reveal)

## Goal

Finish porting Monaco's `applyEdits` tests, including CRLF normalization and the randomized "auto" tests.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/test/common/model/editableTextModel.test.ts` | 621–1121 | suite `EditorModel - EditableTextModel.applyEdits`: the last 23 tests, from `two edits on one line` (line 621) to `issue #48741: Broken undo stack with move lines up with multiple cursors` (line 1100) |
| same | 1123–1240 | suite `CRLF edit normalization` (5 tests) |
| `src/vs/editor/test/common/model/editableTextModelAuto.test.ts` | all (300 lines) | suite `EditorModel Auto Tests` (4 tests) |

## Files to create or change

- `tests/LoomEditor.Core.Tests/Model/EditableTextModelApplyEditsTests.cs` — add tests 24–46
- `tests/LoomEditor.Core.Tests/Model/EditableTextModelCrlfTests.cs`
- `tests/LoomEditor.Core.Tests/Model/EditableTextModelAutoTests.cs`

## Design decisions (already made — do not change)

- Same as P1-24.
- The auto tests use `Math.random`: use seeded `Random` (several fixed seeds via `[Theory]`), seed in assertion messages.
- `change while emitting events 1/2` modify the model from inside a `ContentChanged` handler: this must work (Monaco
  supports it). If it does not, fix `TextModel`, do not skip the tests.

## Tests

- [x] `EditorModel - EditableTextModel.applyEdits` — 23 tests: `two edits on one line`, `many edits`,
  `many edits reversed`, `replacing newlines 1`, `… 2`, `advanced 1`, `advanced simplified`, `issue #144`,
  `issue #2586 …`, `issue #3980`, the 8 `touching edits` / `overlapping edits` tests, `change while emitting events 1`,
  `… 2`, `issue #1580 …`, `issue #47733: Undo mangles unicode characters`, `issue #48741 …`
  (the two "overlapping edits" tests expect an exception)
- [x] `CRLF edit normalization` — 5 tests
- [x] `EditorModel Auto Tests` — 4 tests

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Undo/redo (P1-26, P1-27).

## Suggested commit message

`test(core): port editable text model tests (part 2) and auto tests`
