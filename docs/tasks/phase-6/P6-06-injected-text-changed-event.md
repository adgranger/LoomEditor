# P6-06 — The injected text changed event of the text model

- **Status:** todo
- **Depends on:** P6-05
- **Size:** S

## Goal

When a decoration with injected text is added, moved, changed or removed, the text model raises
`InjectedTextChanged` with one `ModelRawLineChanged` per affected line, before `DecorationsChanged`, like Monaco's
`ModelInjectedTextChangedEvent`. The view model consumes it in P6-21.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/textModelEvents.ts` | 546–557 | `ModelInjectedTextChangedEvent` |
| `src/vs/editor/common/model/textModel.ts` | 1624–1632 | `handleBeforeFireDecorationsChangedEvent` (the injected-text part only) |
| same | 1890–1934 | `_changeDecorationImpl`: the `recordLineAffectedByInjectedText` calls before and after the move (not line heights, not fonts) |
| same | 1936–1974 | `_changeDecorationOptionsImpl`: the same records for the old and the new options |
| same | 1976–2072 (read to the end of the method) | `_deltaDecorationsImpl`: the records for removed and added nodes |
| same | 2588–2700 | `DidChangeDecorationsEmitter`: `_affectedInjectedTextLines`, `recordLineAffectedByInjectedText`, clearing it in `endDeferredEmit`, and `doFire` calling `handleBeforeFire` first |
| `src/vs/editor/test/common/model/modelInjectedText.test.ts` | 15–183 | suite `Editor Model - Injected Text Events` › `Basic`, and `mapChange` |

## Files to create or change

- `src/LoomEditor.Core/Model/Events/ModelInjectedTextChangedEventArgs.cs` — create: `internal sealed class … : EventArgs` with
  `IReadOnlyList<ModelRawLineChanged> Changes`
- `src/LoomEditor.Core/Model/TextModel.DecorationsEmitter.cs` — change: `_affectedInjectedTextLines` (`HashSet<int>?`),
  `RecordLineAffectedByInjectedText(int)`, the "before fire" step, the event `internal event EventHandler<ModelInjectedTextChangedEventArgs>? InjectedTextChanged`;
  update the file's header comment (injected text is now ported)
- `src/LoomEditor.Core/Model/TextModel.Decorations.cs` — change: the three `…Impl` methods record the affected lines
- `tests/LoomEditor.Core.Tests/Model/ModelInjectedTextTests.cs` — create

## Design decisions (already made — do not change)

- Monaco calls the view models directly (`_onDidChangeContentOrInjectedText`); LoomEditor raises an event, like it does for
  `RawContentChanged`. The event is raised in `DoFireDecorationsChanged`, **before** `DecorationsChanged`, only when the
  set is not empty; the lines are reported in the set's insertion order (Monaco iterates a `Set`: insertion order). Use a
  `List<int>` plus `HashSet<int>` (or an ordered set) so the order is the insertion order: do not rely on `HashSet`
  enumeration order.
- `EndDecorationsDeferredEmit` clears the set even when nothing fires (Monaco lines 2626–2630).
- `SetValue` / flush: Monaco does not record injected lines there; do not add records.
- The test port: Monaco registers a fake view model; LoomEditor subscribes to **both** `RawContentChanged` and
  `InjectedTextChanged` and records the changes of both, in the order the events arrive, mapped by a `MapChange` helper like
  Monaco's (`lineChanged` / `linesDeleted` / `linesInserted` / `flush`, read lines 156–183). If the order of the two events
  differs from the expected arrays, fix the order of the deferral blocks in the model (Monaco's `applyEdits` /
  `pushEditOperations` begin the decorations deferral **before** the content one), not the test.
- Undo in the test: `TextModel.Undo()` exists (P3); use it.

## Tests

- [ ] Port: `Editor Model - Injected Text Events` › `Basic`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row of `textModelEvents.ts` (`ModelInjectedTextChangedEvent`), a new row in the phase 6 test table for
      `modelInjectedText.test.ts` (1 / 1); update the remark of `ModelRawChange.cs` ("Injected text … is left out") to say
      that the injected text changes come through `TextModel.InjectedTextChanged`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Line heights and font decorations (not ported); consuming the event in the view model and the cursor (P6-21).

## Suggested commit message

`feat(core): raise an event when injected text changes`
