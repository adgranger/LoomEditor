# P1-22 — `TextModel`: edits, `SetValue`, `SetEol` and the content-changed event

- **Status:** todo
- **Depends on:** P1-21
- **Size:** M

## Goal

Edit the model and tell the world about it: `ApplyEdits`, `SetValue`, `SetEol`, version ids, and the public
`ContentChanged` event (range-based changes, the event that LSP and the future view model need).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/model/textModel.ts` | 467–483 | `_emitContentChangedEvent` (public event part only) |
| same | 484–546 | `setValue`, `_createContentChanged2`, `_setValueFromTextBuffer` |
| same | 547–632 | `setEOL` (and what it calls) |
| same | 782–798 | `_increaseVersionId`, `_overwriteVersionId`, `_overwriteAlternativeVersionId` |
| same | 1483–1602 | `applyEdits` overloads, `_doApplyEdits` — **without** decorations, raw events (P1-23) and edit sources |
| `src/vs/editor/common/textModelEvents.ts` | 42–86 | `IModelContentChangedEvent` |
| `src/vs/editor/test/common/model/textModel.test.ts` | 1158–1174 | `TextModel.mightContainRTL` › `setValue resets 1`, `setValue resets 2` |

## Files to create or change

- `src/LoomEditor.Core/Model/TextModel.Edits.cs` — `ApplyEdits`, `SetValue`, `SetEol`, version ids
- `src/LoomEditor.Core/Model/ModelContentChangedEventArgs.cs` — **public**:
  `IReadOnlyList<ModelContentChange> Changes`, `string Eol`, `int VersionId`, `bool IsUndoing`, `bool IsRedoing`,
  `bool IsFlush`, `bool IsEolChange` (leave out Monaco's `detailedReasons*`)
- `tests/LoomEditor.Core.Tests/Model/TestHelpers/MirrorText.cs` — see below
- `tests/LoomEditor.Core.Tests/Model/TextModelEditTests.cs`
- Run `./eng/Update-PublicApi.ps1 src/LoomEditor.Core`

## Design decisions (already made — do not change)

- **Public edit type = `TextEdit`** (from `LoomEditor.Core.Text`). Monaco's `{ range, text: null }` = `TextEdit.Delete(range)`.
  `forceMoveMarkers` and edit identifiers are not public yet (decorations and cursors come later): internally pass
  `false` / `null` when building `ValidAnnotatedEditOperation`s.
- API:
  - `void ApplyEdits(IReadOnlyList<TextEdit> edits)`
  - `IReadOnlyList<TextEdit> ApplyEdits(IReadOnlyList<TextEdit> edits, bool computeUndoEdits)` — returns the inverse
    edits when `computeUndoEdits` is true (Monaco overloads collapsed to these two)
  - `void SetValue(string text)`, `void SetEol(EndOfLineSequence eol)`
  - `event EventHandler<ModelContentChangedEventArgs>? ContentChanged`
- Ranges are validated with `ValidateRange` (P1-21) before reaching the buffer, as in Monaco.
- `SetValue` raises one event with `IsFlush = true`; `SetEol` raises one with `IsEolChange = true`; no event when
  nothing changes (e.g. an empty edit list or inserting `""`).
- The event is raised **after** the model is fully updated (handlers may read the new text).
- `MirrorText` (test helper, replaces Monaco's `MirrorTextModel`): holds a `string`; for each event it applies the
  `Changes` in order using `RangeOffset`/`RangeLength`/`Text`, and records `VersionId`. It is how tests check that
  events describe the edits correctly.

## Tests

- [ ] `setValue resets 1`, `setValue resets 2`
- [ ] New: insert/delete/replace → text, `VersionId` +1, `AlternativeVersionId` +1, one event per `ApplyEdits` call
- [ ] New: after any random batch of edits (seeded, 200 batches), `MirrorText` equals `GetValue()` and its version equals `VersionId`
- [ ] New: inverse edits returned by `ApplyEdits(edits, true)` restore the original text
- [ ] New: `SetEol(CRLF)` on LF text → `Eol` is `"\r\n"`, event has `IsEolChange`, line contents unchanged
- [ ] New: an event handler that reads `GetValue()` sees the new text

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Line-level ("raw") events (P1-23), undo/redo and `PushEditOperations` (P1-27).

## Suggested commit message

`feat(core): add edits and content-changed event to TextModel`
