# P5-69 — Find model: replace, replace all, `ReplaceAllCommand`

- **Status:** done
- **Depends on:** P5-68
- **Size:** M

## Goal

`FindModelBoundToEditorModel.Replace()` replaces the current match and moves to the next one; `ReplaceAll()` replaces
every match in one undo step, with regex groups, case operators and preserve case. The 17 replace tests of
`findModel.test.ts` pass (44 / 44 with P5-68).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/find/browser/findModel.ts` | 474–505 | `_getReplacePattern`, `replace` |
| same | 515–584 | `replaceAll`, `_largeReplaceAll`, `_regularReplaceAll` |
| `contrib/find/browser/replaceAllCommand.ts` | 16–72 | `ReplaceAllCommand` |

## Files to create or change

- `src/LoomEditor.View/Features/Find/FindModelBoundToEditorModel.Replace.cs` — create (partial)
- `src/LoomEditor.View/Features/Find/ReplaceAllCommand.cs` — create (`IEditCommand`)
- `tests/LoomEditor.View.Tests/Features/Find/FindModelReplaceTests.cs`, `ReplaceAllCommandTests.cs` — create

## Design decisions (already made — do not change)

- `ReplaceCommand` and `ReplaceCommandThatPreservesSelection` exist (`Cursors/Commands/ReplaceCommand.cs`, phase 3).
- `ReplaceAllCommand(Selection editorSelection, IReadOnlyList<Range> ranges, IReadOnlyList<string> replaceStrings)`: sort the
  operations with a **stable** sort by `Range.CompareUsingStarts` (phase 3 trap 3), merge touching ones like Monaco, then
  `builder.AddEditOperation` each; `TrackSelection(editorSelection)`; `ComputeCursorState` returns the tracked selection.
- `_regularReplaceAll` limit `Constants.MAX_SAFE_SMALL_INTEGER` → `TextModel.MaxSafeSmallInteger` (P5-64).
- **`_largeReplaceAll` without `String.prototype.replace`** (phase 5 README trap 3). Rebuild the regex as Monaco does
  (a non-multiline regex gets `Multiline`, keeps `IgnoreCase`; `g` and `u` have no option), then walk the text returned by
  `Model.GetValue(EndOfLinePreference.LF)` with a loop of `regex.Match(text, index)`: append the text before the match,
  then `replacePattern.BuildReplaceString(groups, preserveCase)` — `groups` built like `FindMatch.Create` (P5-62) when
  `hasReplacementPatterns || preserveCase`, else `null`; after an **empty** match the next search starts one code point
  further (JavaScript `AdvanceStringIndex` with the `u` flag; the skipped text is copied with the next slice); stop when
  `Match` fails or the index passes the end, then append the rest. Word separators are ignored here,
  as in Monaco (a Monaco quirk: keep it and say so in a comment). The result replaces the full model range through
  `ReplaceCommandThatPreservesSelection`.
- `isTooLargeForHeapOperation` and the "too large" notification of `CommonFindController.replaceAll` are not ported (no
  notification service): `ReplaceAll` always runs.

## Tests

`findModel.test.ts`, suite `FindModel`, as `[Theory]` plain / chunked like P5-68 (reuse its helpers):

- [x] Port (17): `replace hello`, `replace bla`, `replaceAll hello`, `replaceAll two spaces with one space`, `replaceAll bla`,
      `replaceAll bla with \t\n`, `issue #3516: "replace all" moves page/cursor/focus/scroll to the place of the last replacement`,
      `replace when search string has look ahed regex`,
      `replace when search string has look ahed regex and cursor is at the last find match`,
      `replaceAll when search string has look ahed regex`,
      `replace when search string has look ahed regex and replace string has capturing groups`,
      `replaceAll when search string has look ahed regex and replace string has capturing groups`,
      `replaceAll when search string is multiline and has look ahed regex and replace string has capturing groups`,
      `replaceAll preserving case`, `issue #18711 replaceAll with empty string`,
      `issue #32522 replaceAll with ^ on more than 1000 matches`,
      `issue #19740 Find and replace capture group/backreference inserts \`undefined\` instead of empty string`
- [x] New: `ReplaceAllCommand_TouchingRanges_AreMergedIntoOneEdit`, `ReplaceAll_IsOneUndoStep`,
      `LargeReplaceAll_EmptyMatches_StepOverSurrogatePairs` (call the private path through a model with ≥ 19 999 matches of `^`)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map rows `findModel.ts` ✅ (P5-68, P5-69), `replaceAllCommand.ts` ✅; tests table `findModel.test.ts` 44 / 44
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The commands and the widget buttons that call these methods (P5-70 … P5-74).

## Suggested commit message

`feat(view): replace and replace all in the find model`
