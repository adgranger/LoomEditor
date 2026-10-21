# P5-41 — Track auto-closed characters with model decorations (`AutoClosedAction`)

- **Status:** done
- **Depends on:** P5-40
- **Size:** M

## Goal

The cursor remembers which closing characters the editor inserted, with two decorations per character (the character
and the enclosing pair), and forgets them when a cursor leaves the pair or the pair spans lines. Edits made with the
source `"snippet"` (completion) are also tracked. Nothing reads the tracked characters yet: overtype and delete come
in P5-42, the typing that creates them in P5-43.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/cursor/cursor.ts` | 40, 56, 62 | `_autoClosedActions` and its disposal |
| same | 90–106 | `_validateAutoClosedActions` |
| same | 327–352 | `_pushAutoClosedAction` |
| same | 366–381 | `_executeEditOperation`: collect `closeCharacterRange` / `enclosingRange` of `BaseTypeWithAutoClosingCommand` commands |
| same | 439–473 | `_findAutoClosingPairs` |
| same | 475–515 | `executeEdits`: the `'snippet'` path (`_isTracked`, ranges from the undo edits) |
| same | 542–544 | `getAutoClosedCharacters` |
| same | 682–748 | `AutoClosedAction` |
| `src/vs/editor/common/cursor/cursorTypeEditOperations.ts` | 935–955 | `BaseTypeWithAutoClosingCommand` |
| `src/vs/editor/common/viewModel/viewModelImpl.ts` | 1171–1173 | `getCursorAutoClosedCharacters` |

## Files to create or change

- `src/LoomEditor.View/Cursors/AutoClosedAction.cs` — change (replace the stub)
- `src/LoomEditor.View/Cursors/CursorsController.cs`, `CursorsController.Edits.cs` — change
- `src/LoomEditor.View/Cursors/TypeEditOperations.AutoClosing.cs` — create (partial file of this concern):
  `BaseTypeWithAutoClosingCommand`
- `src/LoomEditor.View/Cursors/Commands/ReplaceCommand.cs` — change: `ReplaceCommandWithOffsetCursorState` is no longer `sealed`
- `src/LoomEditor.View/Layout/ViewModel.Cursors.cs` — change: `GetCursorAutoClosedCharacters()` becomes an instance method
- `src/LoomEditor.View/Features/WordOperations/DeleteWordCommand.cs` — change: call the instance method
- `tests/LoomEditor.View.Tests/Cursors/AutoClosedActionTests.cs` — create

## Design decisions (already made — do not change)

- Decorations are step A's: `TextModel.DeltaDecorations(ownerId: 0, oldIds, newDecorations)`,
  `TextModel.ChangeDecorations(0, accessor => …)`, `TextModel.GetDecorationRange(id)`. Monaco's `deltaDecorations`
  without owner uses owner 0.
- The two options are created once, as `static readonly ModelDecorationOptions` fields of `CursorsController`
  (Monaco builds equal dynamic options each time): `Description = "auto-closed-character"`,
  `InlineClassName = "auto-closed-character"`, `Stickiness = NeverGrowsWhenTypingAtEdges`; and
  `Description = "auto-closed-enclosing"`, same stickiness. No `DecorationStyles` entry: Monaco's CSS rule for
  `auto-closed-character` is commented out (`editor.css` 74–78), so it draws nothing.
- `AutoClosedAction` keeps Monaco's shape: constructor `(TextModel model, string[] autoClosedCharactersDecorations,
  string[] autoClosedEnclosingDecorations)`, `IDisposable`, static `GetAllAutoClosedCharacters`,
  `GetAutoClosedCharactersRanges()`, `IsValid(IReadOnlyList<Range> selections)`. Monaco sorts both arrays in place with
  `Range.compareRangesUsingStarts`: sort **copies** with a stable sort (`OrderBy(r => r, comparer)`; phase 3 trap 3).
  `strictContainsRange` → `Range.StrictContains(Range)`.
- `_validateAutoClosedActions`: set `_autoClosedActions` to the valid list **before** disposing the invalid ones
  (disposal raises `DecorationsChanged`, whose listeners may re-enter the cursor; test `issue #205598` in P5-43).
  `CursorsController.Dispose` disposes the remaining actions.
- `BaseTypeWithAutoClosingCommand : ReplaceCommandWithOffsetCursorState` (unsealed, with `virtual`
  `GetEditOperations` / `ComputeCursorState`) has public `CloseCharacterRange` and `EnclosingRange` (`Range?`) and the
  protected `ComputeCursorStateWithRange`. It is abstract here; P5-43 and P5-45 add the two subclasses.
- `_findAutoClosingPairs`: the regex `/([)\]}>'"`])([^)\]}>'"`]*)$/` is a `static readonly Regex` (CultureInvariant);
  `edit.text.lastIndexOf(openChar, closeCharIndex - 1)` → `LastIndexOf(openChar, closeCharIndex - 1, StringComparison.Ordinal)`
  — JavaScript accepts a start index of `-1`, C# does not: return `null` when `closeCharIndex - 1 < 0` (Monaco then gets
  `-1` and returns `null` too). `edits[0]._isTracked = true` → the existing `IdentifiedEditOperation.IsTracked`: the
  record is immutable, so copy the list and replace its first item with `edits[0] with { IsTracked = true }`.
- `ExecuteEdits(…)` keeps its phase 3 signature; remove the "snippet" cut point remark.

## Tests

- [x] New: `ExecuteEdits_SnippetWithQuotes_TracksTheClosingQuote` (`autoClosingLanguage`, `'<div id'`, snippet edit
      `id=""` → `GetCursorAutoClosedCharacters()` is `[(1,10)-(1,11)]`)
- [x] New: `ExecuteEdits_OtherSource_TracksNothing`
- [x] New: `AutoClosedAction_CursorLeavesThePair_IsForgotten`
- [x] New: `AutoClosedAction_PairBecomesMultiline_IsForgotten`
- [x] New: `AutoClosedAction_Forgotten_RemovesItsDecorations` (no decoration with a description starting with
      `auto-closed-` is left)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map row `cursor.ts` (+ auto-closed actions, P5-41); "Cut points" row of `AutoClosedAction.cs`
      and `CursorsController.Edits.cs` removed
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Reading the tracked characters (overtype, delete: P5-42). Typing that creates them (P5-43).

## Suggested commit message

`feat(view): track auto-closed characters with decorations`
