# P1-23 — Line-level ("raw") content-changed events + `model.test.ts`

- **Status:** todo
- **Depends on:** P1-22
- **Size:** M

## Goal

The second change event Monaco raises, which describes changes **per line** (lines inserted, deleted, changed,
flush, EOL changed). The view model (phase 2) is driven by it. Then port `model.test.ts`, which checks it.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/textModelEvents.ts` | 221–239 | `RawContentChangedType`, `ModelRawFlush` |
| same | 352–372 | `ModelRawLineChanged` |
| same | 424–493 | `ModelRawLinesDeleted`, `ModelRawLinesInserted`, `ModelRawEOLChanged` |
| same | 495–548 | `ModelRawChange`, `ModelRawContentChangedEvent` |
| `src/vs/editor/common/model/textModel.ts` | 1503–1602 | the raw-change part of `_doApplyEdits` (lines ~1530–1580) |
| same | 514–546, 547–632 | raw `Flush` in `_setValueFromTextBuffer`, raw `EOLChanged` in `setEOL` |
| `src/vs/editor/test/common/model/model.test.ts` | 31–327 | suite `Editor Model - Model` (26 tests) |
| same | 329–366 | suite `Editor Model - Model Line Separators` (3 tests) |

## Files to create or change

`src/LoomEditor.Core/Model/Events/` (namespace `LoomEditor.Core.Model.Events`), **public**:
- `ModelRawChange.cs` — `abstract record ModelRawChange` with subclasses `ModelRawFlush`, `ModelRawLineChanged`
  (`LineNumber`, `Detail`), `ModelRawLinesDeleted` (`FromLineNumber`, `ToLineNumber`), `ModelRawLinesInserted`
  (`FromLineNumber`, `ToLineNumber`, `Detail` lines), `ModelRawEolChanged`
- `ModelRawContentChangedEventArgs.cs` — `Changes`, `VersionId`, `IsUndoing`, `IsRedoing`; helpers `ContainsEvent(type)` as in Monaco
- `src/LoomEditor.Core/Model/TextModel.Edits.cs` — raise `event EventHandler<ModelRawContentChangedEventArgs>? RawContentChanged`
  **before** `ContentChanged`, as Monaco does
- `tests/LoomEditor.Core.Tests/Model/ModelTests.cs`, `ModelLineSeparatorsTests.cs`
- Run `./eng/Update-PublicApi.ps1 src/LoomEditor.Core`

## Design decisions (already made — do not change)

- **Leave out injected text** (`LineInjectedText`, the `injectedText` fields): decorations do not exist yet. Where
  Monaco passes injected text, pass nothing.
- Leave out `ModelLineHeightChanged`, `ModelFontChanged` (phase 2+).
- Records give value equality for free: tests can compare expected and actual raw changes with `Assert.Equal`
  (use arrays → compare with `Assert.Equal(expected, actual.Changes)` on lists of records).

## Tests

- [ ] `Editor Model - Model` — 26 tests (`model getValue`, `model insert empty text`, …, `issue #46342: Maintain edit operation order in applyEdits`)
- [ ] `Editor Model - Model Line Separators` — 3 tests

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Suite `Editor Model - Words` (word helpers: phase 5).

## Suggested commit message

`feat(core): add raw content-changed events to TextModel`
