# P3-11 — Trim auto-inserted whitespace (`trimAutoWhitespace`)

- **Status:** done
- **Depends on:** P3-10
- **Size:** S

## Goal

When Enter inserts indentation on a new line and the user then presses Enter again (or moves away and edits
elsewhere near the cursor), Monaco removes the whitespace it inserted automatically, so no line ends with trailing
spaces the user never typed. Wire Monaco's `trimAutoWhitespace` into `TextModel.PushEditOperations`. The piece tree
already computes the candidate lines (`ApplyEditsResult.TrimAutoWhitespaceLineNumbers`, phase 1); nothing uses them yet.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/model/textModel.ts`. If a function is not exactly at the given line, search for its name.

| Lines | What to port |
|---|---|
| 276 | `private _trimAutoWhitespaceLines: number[] \| null` |
| 385, 532 | reset to `null` in the constructor and in `_setValueFromTextBuffer` |
| 1351–1433 | `_pushEditOperations`: the whole `if (this._options.trimAutoWhitespace && this._trimAutoWhitespaceLines)` block ("edits are near cursors" heuristic, the two "inserts a new line" exceptions, then one trim edit per allowed line) |
| 1506–1510 | `_doApplyEdits`: pass `this._options.trimAutoWhitespace` to `buffer.applyEdits` and store `result.trimAutoWhitespaceLineNumbers` |

## Files to create or change

- `src/LoomEditor.Core/Model/TextModel.Undo.cs` — change: the trimming block runs at the start of the internal `PushEditOperations` (P3-10)
- `src/LoomEditor.Core/Model/TextModel.Edits.cs` — change: `recordTrimAutoWhitespace: Options.TrimAutoWhitespace` (today it is hard-coded `false`, line ~138), store the line numbers
- `src/LoomEditor.Core/Model/TextModel.TrimAutoWhitespace.cs` — create (partial) if `TextModel.Undo.cs` would pass ~400 lines
- `tests/LoomEditor.Core.Tests/Model/TextModelTrimAutoWhitespaceTests.cs` — create

## Design decisions (already made — do not change)

- Read [the phase 3 conventions](README.md) first.
- Port lines 1352–1433 line by line. The trim edit is
  `new ValidAnnotatedEditOperation(Identifier: null, Range: new Range(line, 1, line, maxColumn), Text: null, ForceMoveMarkers: false, IsAutoWhitespaceEdit: false, IsTracked: false)`.
  It is appended to the list of operations **before** they are pushed to the edit stack, so it is undone with them.
- The `incomingEdits` ranges are validated with `ValidateRange`, as in Monaco.
- `editText.charAt(0) === '\n'` and `editText.charAt(editText.length - 1) === '\n'` check `'\n'` only (not `"\r\n"`):
  the cursor always types `"\n"`; the model converts it to the model EOL later. Keep it exactly like that.
- Only `PushEditOperations` trims. `ApplyEdits` (no undo) does not trim but **does** record the line numbers, as in Monaco.
- `SetValue` resets the list to `null`.

## Tests

The cursor tests that exercise this (`removeAutoWhitespace` in `cursor.test.ts`) are ported with typing (P3-33).
Here, test the model directly:

Setup for the first three tests: model `"  x"`; push an edit that inserts `"\n  "` at `(1,4)` with
`IsAutoWhitespaceEdit = true` (this is what Enter does), then call `PushStackElement()`. Line 2 is now `"  "` and is
recorded as a trim candidate.

- [x] New: push an edit that inserts `"\n"` at `(2,3)` (Enter again at the end of line 2) with
      `beforeCursorState = [(2,3)]` → line 2 becomes `""` and line 3 is `""`; one `Undo` restores `"  x\n  "`
      (exception "inserts a new line after `trimLine`")
- [x] New: push an edit that inserts `"z"` at `(2,3)` with `beforeCursorState = [(2,3)]` → line 2 is `"  z"`, not
      trimmed (the edit touches the line)
- [x] New: in a 20-line document, push an edit on line 15 with `beforeCursorState = [(2,3)]` → line 2 is **not**
      trimmed (heuristic "edits are near cursors": the edit is not near the cursor)
- [x] New: with `UpdateOptions(TrimAutoWhitespace: false)`, nothing is ever trimmed
- [x] New: `Undo` after a trimming edit restores both the edit and the trimmed whitespace

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row `textModel.ts` mentions `trimAutoWhitespace` (P3-11)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The cursor side that marks edits as auto-whitespace (Enter, Tab: P3-33, P3-34).

## Suggested commit message

`feat(core): trim auto-inserted whitespace on the next edit`
