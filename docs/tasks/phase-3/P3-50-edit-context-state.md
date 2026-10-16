# P3-50 — Edit context state (the text the IME sees, and its updates)

- **Status:** done
- **Depends on:** P3-49
- **Size:** S

## Goal

An IME works on a piece of text it receives from the editor: here, as in Monaco's `nativeEditContext.ts`, the full
lines of the **primary** selection, with the selection offsets inside them. When the IME updates a range of that text,
the update must become a `Type` or a `CompositionType` of the cursor. Port this logic as a pure class (no Windows
types), so that it is tested without an IME.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/browser/controller/editContext/native/nativeEditContext.ts` (613 lines). If a line is not exactly
where given, search for the name.

| Lines | What to port |
|---|---|
| 198–222 | the `textupdate` listener: buffering a lone high surrogate and merging it with the next low surrogate (update range shifted by −1) |
| 416–432 | `_updateEditContext` (which text and selection to push, only when changed; `' '` when the text is empty) |
| 434–469 | `_emitTypeEvent` (compute `replacePrevCharCnt`, `replaceNextCharCnt`, the padded text and `positionDelta` against the **previous** selection) |
| 471–477 | `_onType` (`compositionType` when a count or the delta is non-zero, else `type`) |
| 479–498 | `_getNewEditContextState` |
| `nativeEditContextUtils.ts` 10–15 | `ITypeData` |

## Files to create or change

- `src/LoomEditor.View/Controller/EditContextState.cs` — create: `EditContextState`, `TypeData`, `TextUpdate`
- `src/LoomEditor.View/Controller/EditContextModel.cs` — create: keeps the text and selection last given to the IME
  and turns IME updates into cursor calls
- `tests/LoomEditor.View.Tests/Controller/EditContextModelTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- `internal sealed record EditContextState(string Text, int SelectionStartOffset, int SelectionEndOffset, Selection PrimarySelection)`
  — `GetNewEditContextState(TextModel model, Selection primary)` returns `null` when the range is not valid (port of
  479–498: `model.IsValidRange`, text of the lines from the start line to the end line, `SelectionStartOffset = startColumn - 1`,
  `SelectionEndOffset = text.Length + endColumn - endLineMaxColumn`).
- `internal readonly record struct TextUpdate(int UpdateRangeStart, int UpdateRangeEnd, string Text, int SelectionStart, int SelectionEnd)`
  (Monaco's `ITextUpdateEvent`; the WinUI `CoreTextTextUpdatingEventArgs` maps to it in P3-51).
- `internal readonly record struct TypeData(string Text, int ReplacePrevCharCnt, int ReplaceNextCharCnt, int PositionDelta)`.
- `EditContextModel`:
  - fields `_previousText` (initially `""`), `_previousSelection` (`(0, 0)`), `_primarySelection`, `string? _highSurrogate`;
  - `(bool TextChanged, bool SelectionChanged) Update(TextModel model, Selection primary)` — port of `_updateEditContext`:
    it returns what changed so that the control calls `NotifyTextChanged` / `NotifySelectionChanged` (P3-51) only when needed;
  - `string Text` and `(int Start, int End) Selection` (what `TextRequested` / `SelectionRequested` answer);
  - `TypeData? OnTextUpdate(TextUpdate e)` — the surrogate buffering of 198–222 (returns `null` while a high surrogate
    is buffered), then `_emitTypeEvent` (434–469): it **reads the previous text** for the padding, then sets the
    previous selection to the update's selection. The padding uses `Substring` with the same bounds as Monaco's `substring`;
  - `static void Apply(ViewModel viewModel, TypeData data)` — port of `_onType`: `CompositionType(..., source: "keyboard")`
    when `ReplacePrevCharCnt != 0 || ReplaceNextCharCnt != 0 || PositionDelta != 0`, else `Type(text, "keyboard")`.
- Only the primary selection is exposed to the IME; with several cursors, `CompositionType` still applies to all of them
  (ADR 0006; Monaco does the same).

## Tests

No Monaco test covers this logic (`nativeEditContextUtils.test.ts` tests a DOM focus tracker). New tests:

- [x] New: `GetNewEditContextState` on `["abc", "def"]` with a selection `(1,2) → (2,3)` → text `"abc\ndef"`, offsets 1 and 6
- [x] New: insert at a collapsed selection: previous `(3,3)`, update `(3,3,"x", 4,4)` → `TypeData("x", 0, 0, 0)` → `Type`
- [x] New: IME replaces the two previous characters: previous `(5,5)`, update `(3,5,"せん", 5,5)` → `("せん", 2, 0, 0)` → `CompositionType`
- [x] New: the IME moves its caret inside the composition: previous `(5,5)`, update `(3,5,"せん", 4,4)` → `PositionDelta == -1`
- [x] New: a high surrogate alone returns `null`; the next low surrogate returns the pair with the update range shifted by −1
- [x] New: `Update` reports `TextChanged` only when the lines of the primary selection changed

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row `editor/browser/controller/editContext/native/nativeEditContext.ts` (🔶 text, selection
      and type data, P3-50); behavior difference "only the primary selection is exposed to the IME" (same as Monaco: write it as a note)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The Windows API (P3-51), layout bounds (P3-52), formats (P3-53).

## Suggested commit message

`feat(view): port the edit context state used by the IME`
