# P10-13 — Selection anchor and `Expand Line Selection`

- **Status:** todo
- **Depends on:** P9-33
- **Size:** S

## Goal

Four anchor commands (`Ctrl+K Ctrl+B` set the anchor, `Ctrl+K Ctrl+K` select from the anchor to the caret, `editor.action.goToSelectionAnchor`, `Escape` cancels) and `expandLineSelection` (`Ctrl+L`) exist as editor commands.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/anchorSelect/browser/anchorSelect.ts` | 21–100 | `SelectionAnchorSet` context key, `SelectionAnchorController` (`setSelectionAnchor`, `goToSelectionAnchor`, `selectFromAnchorToCursor`, `cancelSelectionAnchor`, the model-change reset) |
| `src/vs/editor/contrib/anchorSelect/browser/anchorSelect.ts` | 102–171 | the four actions: ids `editor.action.setSelectionAnchor` (`Ctrl+K Ctrl+B`), `editor.action.goToSelectionAnchor` (no key), `editor.action.selectFromAnchorToCursor` (`Ctrl+K Ctrl+K`, `when selectionAnchorSet`), `editor.action.cancelSelectionAnchor` (`Escape`, `when selectionAnchorSet`) |
| `src/vs/editor/contrib/anchorSelect/browser/anchorSelect.css` | 5–8 | the anchor is a 2 px wide bar, color `#007ACC` |
| `src/vs/editor/contrib/lineSelection/browser/lineSelection.ts` | 19–48 | `ExpandLineSelectionAction`: id `expandLineSelection`, `Ctrl+L`, when `textInputFocus`; `pushStackElement`, `CursorMoveCommands.ExpandLineSelection` (already in `src/LoomEditor.View/Cursors/CursorMoveCommands.cs:124`), set the cursor states with reason `Explicit`, reveal all cursors |
| `src/vs/editor/contrib/lineSelection/test/browser/lineSelection.test.ts` | whole file | the tests |

## Files to create or change

- `src/LoomEditor.View/Features/SelectionAnchor/SelectionAnchorFeature.cs`, `SelectionAnchorCommands.cs`, `README.md` — create (header: ported from `anchorSelect.ts`)
- `src/LoomEditor.View/Features/LineSelection/LineSelectionFeature.cs`, `LineSelectionCommands.cs`, `README.md` — create (header: ported from `lineSelection.ts`)
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: both features
- `src/LoomEditor.View/Commands/KeybindingContext.cs` — change: field `SelectionAnchorSet` (`selectionAnchorSet`); fill it where the control fills `SuggestWidgetVisible` (`grep -rn "SuggestWidgetVisible" src`)
- `src/LoomEditor.View/Commands/DefaultKeybindings.cs` — change: the four keys (the chord helper is `Add(…, new Keybinding(Ctrl, KeyK), …)` as in the `removeCommentLine` row, line ~145) and `Ctrl+L`
- `src/LoomEditor.View/Layout/DecorationStyles.cs`, `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: class `selection-anchor` → `DecorationStyle(BorderColorId: "loom.selectionAnchor", BorderWidth: 0, LeftBorderWidth: 2)` and the row `["loom.selectionAnchor"] = ("#007ACC", "#007ACC", "#007ACC", "#007ACC")` with a comment saying the id is LoomEditor's (Monaco hard-codes the color in CSS)
- `tests/LoomEditor.View.Tests/Features/SelectionAnchor/…`, `…/LineSelection/…` — create

## Design decisions (already made — do not change)

- The anchor is **one model decoration** with an **empty range** at the caret (`TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges`, class `selection-anchor`, description `selection-anchor`), created through the feature's `EditorDecorationsCollection` (look at `FindDecorations.cs`). The 2 px bar is drawn by the existing left-border mechanism that P9-16 added for empty ranges (`LeftBorderWidth`, used by `diff-range-empty`).
- `hoverMessage` ("Selection Anchor") is **not** ported (the hover contract of phase 8 is provider based). The `alert(...)` ("Anchor set at line:column") becomes `IEditorAnnouncer.Announce` with a new `AccessibilityMessages.AnchorSet(line, column)`.
- The anchor is removed when the model changes (the `onDidChangeModel` reset: here, the feature is disposed with the editor, so only `Cancel` and a **new `SetSelectionAnchor`** remove it) and when the model is disposed. `selectFromAnchorToCursor` cancels it after selecting.
- Read-only editors: set / select / go to work (they do not edit). `Escape` with `selectionAnchorSet` must **not** swallow the Escape of other features: give its keybinding a **lower** weight than the find widget's and the suggest widget's `Escape` rules (read how `KeybindingWeight` orders them and copy the weight of the lowest existing `Escape` rule), and note it in the README.
- `expandLineSelection`: exactly Monaco's `run` (`PushStackElement`, `ViewModel.SetCursorStates(…, CursorChangeReason.Explicit, CursorMoveCommands.ExpandLineSelection(...))`, then reveal all cursors with the smooth flag as `LinesOperations` actions do). No arguments.

## Tests

- [ ] Port: `lineSelection.test.ts` (names in `// Monaco:` comments)
- [ ] New: `SetAnchor_CreatesDecorationAtCaret`, `SetAnchor_Twice_ReplacesTheFirst`, `SelectFromAnchorToCursor_SelectsAndCancels`, `SelectFromAnchorToCursor_AnchorBeforeCaret_Forward`, `SelectFromAnchorToCursor_AnchorAfterCaret_Backward`, `GoToAnchor_MovesCaret`, `Anchor_FollowsEditsBeforeIt`, `Cancel_RemovesDecoration_AndContextKey`, `ContextKey_TrueOnlyWhileAnchorSet`, `Escape_WithAnchor_ResolvesToCancel`, `Escape_WithoutAnchor_DoesNotResolveToCancel`, `Announces_AnchorSet`
- [ ] Rendering: `selection-anchor` empty range draws a 2 px left border (recording surface; copy the diff-range-empty test of P9-16 in `TextViewRendererDecorationsTests`)
- [ ] Manual (section "P10-13"): `Ctrl+K Ctrl+B`, move, `Ctrl+K Ctrl+K` selects; a blue bar marks the anchor; `Escape` removes it; `Ctrl+L` selects the line then the next, repeatedly.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `anchorSelect.ts`, `lineSelection.ts` rows; "Behavior differences": no hover message, color id `loom.selectionAnchor`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`cursorUndo` (P10-14), the anchor in several models, the hover message.

## Suggested commit message

`feat(view): add the selection anchor commands and expand line selection`
