# P10-23 — `OneSnippet`: tab stops, placeholders and moving between them

- **Status:** done
- **Depends on:** P10-22
- **Size:** M

## Goal

An inserted snippet is tracked in the model by one decoration per placeholder; `OneSnippet.Move(forward)` returns the selections of the next / previous tab stop (all the placeholders of the same index at once, with their transforms applied), and merging a nested snippet into an active placeholder works. The placeholders are highlighted with the theme colors of Monaco.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/snippet/browser/snippetSession.ts` | 28–62 | `OneSnippet` fields, the four decoration option sets (`_decor`: read the class names and stickiness), constructor, `initialize`, `dispose` |
| `src/vs/editor/contrib/snippet/browser/snippetSession.ts` | 63–92 | `_initDecorations` |
| `src/vs/editor/contrib/snippet/browser/snippetSession.ts` | 93–210 | `move` (transform application, deactivation of the old group, activation of the new one, the empty-placeholder collapse detection `_hasPlaceholderBeenCollapsed`) |
| `src/vs/editor/contrib/snippet/browser/snippetSession.ts` | 211–305 | `isAtFirstPlaceholder`, `isAtLastPlaceholder`, `hasPlaceholder`, `isTrivialSnippet`, `computePossibleSelections`, `activeChoice`, `hasChoice`, `activePlaceholderCount` |
| `src/vs/editor/contrib/snippet/browser/snippetSession.ts` | 306–400 | `merge` and `_renormalizePlaceholderIndices`, `getEnclosingRange` |
| `src/vs/editor/contrib/snippet/browser/snippetSession.css` | 5–19 | `snippet-placeholder`, `finish-snippet-placeholder` |
| `src/vs/platform/theme/common/colors/editorColors.ts` | 248–262 | `editor.snippetTabstopHighlightBackground` (dark / hcDark `rgba(124,124,124,0.3)`, light / hcLight `rgba(10,50,100,0.2)`), `editor.snippetTabstopHighlightBorder` (none), `editor.snippetFinalTabstopHighlightBackground` (none), `editor.snippetFinalTabstopHighlightBorder` (dark / hcDark `#525252`, light `rgba(10,50,100,0.5)`, hcLight `#292929`) |

## Files to create or change

- `src/LoomEditor.View/Features/Snippet/OneSnippet.cs` (+ `OneSnippet.Move.cs`, `OneSnippet.Merge.cs`) — create (headers: ported from `snippetSession.ts`)
- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change: the four colors (copy the style of the neighbouring rows: `(dark, light, hcDark, hcLight)`; an absent default is the empty string / the null marker the file already uses: read how other "none" defaults are written)
- `src/LoomEditor.View/Layout/DecorationStyles.cs` — change: `snippet-placeholder` → `DecorationStyle("editor.snippetTabstopHighlightBackground", "editor.snippetTabstopHighlightBorder", BorderWidth: 1)`, `finish-snippet-placeholder` → `DecorationStyle("editor.snippetFinalTabstopHighlightBackground", "editor.snippetFinalTabstopHighlightBorder", BorderWidth: 1)` (the CSS `outline` is drawn as the existing 1 px border; `min-width: 2px` is not ported: an empty placeholder is drawn by the empty-range mechanism if the renderer has one, otherwise not at all: say which in the commit body)
- `tests/LoomEditor.View.Tests/Features/Snippet/OneSnippetTests.cs` — create

## Design decisions (already made — do not change)

- `OneSnippet` is `internal sealed class` with the constructor `(EditorContext editor, TextmateSnippet snippet, string leadingWhitespace)` (Monaco passes the `IActiveCodeEditor`; the C# editor-side object that owns the model, the selections and the decorations is `EditorContext`; its model is `Model`, and decorations are changed through `Model.ChangeDecorations(accessor => …)` with `AddDecoration`, `ChangeDecoration`, `RemoveDecoration`, `ChangeDecorationOptions`, and read through `Model.GetDecorationRange(id)`: use the names of the code base, `grep -rn "GetDecorationRange\|ChangeDecorations" src/LoomEditor.Core`).
- The decoration owner is the **snippet feature's** owner id (a constant `SnippetFeature.OwnerId` obtained like other features obtain theirs: see `FindDecorations`); the four option sets are `static readonly ModelDecorationOptions` built with the factory the other features use.
- Port `move` line by line, including the transformation of placeholder text (edits applied with `Model.PushEditOperations` inside `Move`, returning the selections computed from the decoration ranges **after** the edit), the choice that "an empty or collapsed placeholder is skipped" and the `_placeholderGroupsIdx` logic. `groupBy` of Monaco is `GroupAdjacentBy` on the sorted placeholders (write a 10-line private helper).
- Nested snippets: `Merge(IReadOnlyList<OneSnippet> others)` and `_nestingLevel` as Monaco, including the placeholder index renormalization.
- The selections returned are `Selection`s with `selectionStart` at the placeholder start and the cursor at its end (Monaco: `new Selection(range.startLineNumber, range.startColumn, range.endLineNumber, range.endColumn)`).
- `<remarks>Threading: UI thread only.</remarks>`.

## Tests

- [x] Port: the tests of `snippetSession.test.ts` (969 lines, 47 tests) that exercise `OneSnippet` directly or through a session helper that **only moves between placeholders**: titles such as "snippets, repeated tabstops", "snippets, just text", "snippets, selections and snippet ranges", "snippets, nested sessions" … — read the file, take the ones that do not call `SnippetSession.insert` with several cursors / `adjustWhitespace` (P10-24 ports those). Use a helper that builds the `TextmateSnippet` with `SnippetParser`, inserts its text into a model at an offset, calls `Initialize` and `Move`
- [x] New: `Move_Forward_VisitsPlaceholdersInOrderThenFinal`, `Move_Backward_AtFirst_StaysAtFirst`, `Move_SameIndexPlaceholders_SelectedTogether`, `Move_AppliesTransformOfLeftPlaceholder`, `Move_EmptyPlaceholder_ReturnsCaret`, `Typing_InActivePlaceholder_GrowsDecoration`, `Typing_AfterInactivePlaceholder_DoesNotGrowIt`, `Merge_NestedSnippet_RenumbersPlaceholders`, `IsTrivialSnippet_OnlyFinalTabstop_True`, `ActiveChoice_OnChoicePlaceholder`, `Dispose_RemovesDecorations`
- [x] New (Rendering): `snippet-placeholder` draws a 1 px border and background (recording surface)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `snippetSession.ts` (OneSnippet) 🔶, `snippetSession.css`, the four theme colors
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

`SnippetSession` (P10-24), the controller and keys (P10-25), showing the choices as a list.

## Suggested commit message

`feat(view): port OneSnippet (snippet tab stops and placeholders)`
