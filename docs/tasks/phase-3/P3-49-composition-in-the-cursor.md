# P3-49 — Composition in the cursor (`compositionType`, start and end of a composition)

- **Status:** todo
- **Depends on:** P3-48
- **Size:** M

## Goal

An IME composes text step by step: "s", then "せ", then "せん"… Each step replaces the previous characters around the
cursor (`compositionType(text, replacePrevCharCnt, replaceNextCharCnt, positionDelta)`). Port the cursor side of
composition: the operation, the composition state captured at the start, the outcome deduced at the end, and the
view model API. The WinUI side comes in P3-51.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursor.ts` | 546–562 | `startComposition`, `endComposition` |
| same | 589–607 | `compositionType` (the no-op edit that only moves the cursor by `positionDelta`) |
| same | 1044–1126 | `CompositionLineState`, `CompositionState` (`_capture`, `deduceOutcome`, `_deduceOutcome`) |
| same | 564–587 | `type`: pass `isDoingComposition = _compositionState != null` (it was `false` since P3-33) |
| `src/vs/editor/common/cursor/cursorTypeOperations.ts` | 68–163 | `compositionType`, `compositionEndWithInterceptors` (see the cuts) |
| same | 205–215 | `CompositionOutcome` |
| `src/vs/editor/common/cursor/cursorTypeEditOperations.ts` | 406–419 | `CompositionEndOvertypeOperation` |
| same | 797–820 | `CompositionOperation` |
| `src/vs/editor/common/viewModel/viewModelImpl.ts` | 1213–1224 | `startComposition`, `endComposition`, `compositionType` |
| `src/vs/editor/test/browser/controller/cursor.test.ts` | 2525 | `issue #9675: Undo/Redo adds a stop in between CHN Characters` |

## Files to create or change

- `src/LoomEditor.View/Cursors/CompositionState.cs` — create (`CompositionLineState`, `CompositionState`, `CompositionOutcome`)
- `src/LoomEditor.View/Cursors/TypeEditOperations.Composition.cs` — create (partial): `CompositionOperation`, `CompositionEndOvertypeOperation`
- `src/LoomEditor.View/Cursors/TypeOperations.cs` — change: `CompositionType`, `CompositionEndWithInterceptors`
- `src/LoomEditor.View/Cursors/CursorsController.Composition.cs` — create (partial)
- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — change: `StartComposition()`, `EndComposition(string? source = null)`,
  `CompositionType(string text, int replacePrevCharCnt, int replaceNextCharCnt, int positionDelta, string? source = null)`
- `tests/LoomEditor.View.Tests/Cursors/CompositionTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `CompositionState._capture` returns `null` when there are several selections on the same line or a non-empty
  multi-line selection (port its exact rules); `deduceOutcome` uses `Strings.CommonPrefixLength` /
  `CommonSuffixLength` (P3-01).
- `compositionEndWithInterceptors` for a model with no language configuration:
  - the **surround** branch (`hasDeletion`): `shouldSurroundChar(config, ch)` and `config.surroundingPairs` are a
    phase 4–5 cut point → return `null` (Monaco returns `null` there when `surroundingPairs` is empty);
  - `AutoClosingOvertypeWithInterceptorsOperation` and `AutoClosingOpenCharTypeOperation`: cut points (they return
    `undefined` without a language) → skip;
  - `CompositionEndOvertypeOperation.GetEdits` is ported: it returns `null` unless `InputMode == Overtype`, so in phase 3
    the end of a composition makes no extra edit.
- `CompositionOperation._compositionType` ignores non-empty selections (returns `null`, issue #2773) and clamps the
  range to the line: port exactly. It uses `ReplaceCommandWithOffsetCursorState` (P3-15).
- `CursorsController.EndComposition` runs the interceptors only when `source == "keyboard"`, as Monaco.

## Tests

- [ ] Port `cursor.test.ts` › `issue #9675: Undo/Redo adds a stop in between CHN Characters` (2525)
- [ ] New: `StartComposition`, `CompositionType("に", 0, 0, 0)`, `CompositionType("にほ", 1, 0, 0)`, `EndComposition("keyboard")`
      → the line is `"にほ"`, the cursor after it, one undo step removes it
- [ ] New: `CompositionType("", 0, 0, -1)` moves the cursor one column left without editing
- [ ] New: two cursors on two lines: `CompositionType` replaces at both
- [ ] New: `CompositionState.DeduceOutcome` for `"ab|"` → `"abc|"` gives inserted `"c"`, deleted `""`
- [ ] New: while a composition is active, `Type("x", "keyboard")` passes `isDoingComposition = true` (Enter is then
      typed as a plain character: `EnterOperation` only applies when not composing)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map rows `cursor.ts` (✅ except auto-closed actions, P3-49), `cursorTypeOperations.ts` (+ composition);
      tests table `cursor.test.ts` (+1)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The edit context and the WinUI side (P3-50, P3-51), composition underlines (P3-53).

## Suggested commit message

`feat(view): port IME composition in the cursor`
