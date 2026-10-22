# P5-93 — The `WordHighlighter` feature and its commands (F7 / Shift+F7)

- **Status:** done
- **Depends on:** P5-92
- **Size:** S

## Goal

Word highlight is on in every editor: the `WordHighlighterFeature` (Monaco's `WordHighlighterContribution`) owns a
`WordHighlighter`, and the commands `editor.action.wordHighlight.next` (F7), `editor.action.wordHighlight.prev`
(Shift+F7) and `editor.action.wordHighlight.trigger` (no key) exist.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/wordHighlighter/browser/wordHighlighter.ts` | 44 | `ctxHasWordHighlights` (`hasWordHighlights`) |
| same | 428–464 | `moveNext`, `moveBack` (the screen reader `alert` is not ported) |
| same | 864–940 | `WordHighlighterContribution` (`createWordHighlighterIfPossible`, `saveViewState`, `moveNext`, `moveBack`, `restoreViewState(250)`, `stopHighlighting`, `dispose`) |
| same | 943–1023 | `WordHighlightNavigationAction`, `NextWordHighlightAction`, `PrevWordHighlightAction`, `TriggerWordHighlightAction`, registrations |

## Files to create or change

- `src/LoomEditor.View/Features/WordHighlighter/WordHighlighterFeature.cs` — create (`Id = "loom.wordHighlighter"`)
- `src/LoomEditor.View/Features/WordHighlighter/WordHighlighterCommands.cs` — create (the three ids)
- `src/LoomEditor.View/Features/WordHighlighter/WordHighlighter.cs` — change: `MoveNext`, `MoveBack`
- `src/LoomEditor.View/Features/WordHighlighter/README.md` — create
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: `() => new WordHighlighterFeature()`
- `src/LoomEditor.View/Commands/DefaultKeybindings.cs` — change: F7 → next, Shift+F7 → prev
  (`WhenClauses.EditorTextFocus`, `KeybindingWeight.EditorContrib`)
- `tests/LoomEditor.View.Tests/Features/WordHighlighter/WordHighlighterFeatureTests.cs` — create

## Design decisions (already made — do not change)

- No highlighter for `Model.IsTooLargeForTokenization` (Monaco 885); one model per editor, so `onDidChangeModel` is
  not ported.
- The precondition `hasWordHighlights` is checked in the command (`WordHighlighter.HasDecorations()`); the keybinding
  `when` is `editorTextFocus` only (as the folding commands, P5-89).
- `moveNext` / `moveBack` set the position with `viewModel.SetSelections("api", …)` while `_ignorePositionChangeEvent`
  is true, then reveal with `VerticalRevealType.CenterIfOutsideViewport` (Monaco `revealRangeInCenterIfOutsideViewport`).
- The view-state methods (`saveViewState`, `restoreViewState`) are internal; only `trigger` uses `restoreViewState(true)`.

## Tests

No Monaco test is ported (see P5-92). New:

- [x] `Next_MovesToTheNextOccurrence_AndWraps`; `Prev_MovesToThePreviousOccurrence_AndWraps`
- [x] `Next_WithoutHighlights_DoesNothing`
- [x] `Trigger_AfterTyping_HighlightsAfter250Ms`
- [x] `Next_DoesNotClearTheHighlights` (the position change is ignored)
- [x] Keybindings: `F7` → `editor.action.wordHighlight.next`, `Shift+F7` → `…prev`
- [x] `EditorFeatures_Default_ContainsWordHighlighter`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `wordHighlighter.ts` row (contribution and actions, P5-93)
- [x] Card status and `docs/tasks/README.md` updated
- [ ] Manual check (maintainer, Gallery, C# sample): the cursor on an identifier highlights its other occurrences
      after a short delay; F7 / Shift+F7 cycle through them; typing removes the highlights

## Out of scope

Semantic highlights (read / write kinds) from language features: phase 8.

## Suggested commit message

`feat(word-highlight): add the word highlighter feature and its navigation commands`
