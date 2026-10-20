# P5-51 — Comment feature: toggle, add, remove line comment and block comment

- **Status:** todo
- **Depends on:** P5-50
- **Size:** S

## Goal

`Ctrl+/` toggles line comments, `Ctrl+K Ctrl+C` adds them, `Ctrl+K Ctrl+U` removes them and `Shift+Alt+A` toggles a
block comment, with every cursor, using the language configuration of the document. The `comments.insertSpace` and
`comments.ignoreEmptyLines` options exist with VS Code's defaults.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/comment/browser/comment.ts` | 20–79 | `CommentLineAction.run` (selection sort, `ignoreFirstLine`, one `LineCommentCommand` per selection, undo stops) |
| same | 81–133 | `ToggleCommentLineAction`, `AddLineCommentAction`, `RemoveLineCommentAction`: ids, `precondition`, `kbOpts` |
| same | 135–176 | `BlockCommentAction` (id, `precondition`, `kbOpts`, `run`) |
| `common/config/editorOptions.ts` | 1517–1575 | `IEditorCommentsOptions`, `EditorComments` defaults (`insertSpace: true`, `ignoreEmptyLines: true`) |

## Files to create or change

- `src/LoomEditor.View/Features/Comment/EditorCommentsOptions.cs` — create
- `src/LoomEditor.View/Features/Comment/CommentActions.cs` — create: `RunLineComment(ViewModel, LineCommentType, EditorCommentsOptions, string source)`, `RunBlockComment(ViewModel, EditorCommentsOptions, string source)`
- `src/LoomEditor.View/Features/Comment/CommentFeature.cs` — change: register the four commands
- `src/LoomEditor.View/Features/Comment/README.md` — change: commands, keys, options, limits
- `src/LoomEditor.View/Commands/DefaultKeybindings.cs` — change: four rules (below)
- `src/LoomEditor.View/Commands/WhenClauses.cs` — change: add `WritableEditorTextFocus` if missing
- `tests/LoomEditor.View.Tests/Features/Comment/CommentFeatureTests.cs` — create
- `tests/LoomEditor.View.Tests/Commands/DefaultKeybindingsTests.cs` — change only if its list of expected conflicts must grow

## Design decisions (already made — do not change)

- `internal sealed record EditorCommentsOptions(bool InsertSpace = true, bool IgnoreEmptyLines = true) { public static EditorCommentsOptions Default { get; } = new(); }`.
  `CommentFeature` has `internal EditorCommentsOptions Options { get; set; } = EditorCommentsOptions.Default;` (no
  public option in phase 5, ADR 0009 decision 12).
- The language configuration registry is the model's: `viewModel.Model.LanguageConfigurationRegistry` (internal,
  P5-15; the same instance as `viewModel.CursorConfig.LanguageConfigurationService` of P5-38). The indent size is
  `viewModel.Model.Options.IndentSize`.
- `run` → `viewModel.Model.PushStackElement(); viewModel.ExecuteCommands(commands, id); viewModel.Model.PushStackElement();`
  (Monaco's `pushUndoStop` / `executeCommands`). The selections sort uses `Range.CompareUsingStarts` with a **stable**
  sort (`OrderBy`).
- `precondition: writable` is ported: each `EditorCommand` gets `canExecute: () => !viewModel.CursorConfig.ReadOnly`.
- Keybindings (all `KeybindingWeight.EditorContrib`, `when` = `writable AND editorTextFocus` =
  `WhenClauses.WritableEditorTextFocus`), registered in this order:

  | Keys (Windows) | Command id |
  |---|---|
  | `Ctrl+/` | `editor.action.commentLine` |
  | `Ctrl+K Ctrl+C` | `editor.action.addCommentLine` |
  | `Ctrl+K Ctrl+U` | `editor.action.removeCommentLine` |
  | `Shift+Alt+A` | `editor.action.blockComment` (the Linux override `Ctrl+Shift+A` is ignored) |

- Menus (`menuOpts`) and `canTriggerInlineEdits` are not ported.

## Tests

- [ ] New: `CommentLine_TypeScriptDocument_TogglesSlashSlashOnEverySelectedLine` (a `TestEditorServices` model whose
      language has `{ lineComment: '//', blockComment: ['/*', '*/'] }`; run twice → original text)
- [ ] New: `CommentLine_TwoCursorsOnTheSameLine_CommentsTheLineOnce` (the `ignoreFirstLine` logic of lines 44–59)
- [ ] New: `AddAndRemoveCommentLine_RunOnPlainText_DoNothing` (plain text has no comment tokens)
- [ ] New: `BlockComment_WithSelection_WrapsItAndUndoRestoresTheText`
- [ ] New: `CommentLine_ReadOnly_DoesNotRun`
- [ ] New: `Options_InsertSpaceFalse_AddsTheTokenWithoutSpace`
- [ ] New: keybinding resolution: `Ctrl+/` → `editor.action.commentLine`; `Ctrl+K` then `Ctrl+C` → `editor.action.addCommentLine`;
      not writable → no command

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `contrib/comment/browser/comment.ts` → `Features/Comment/` (✅ without menus, P5-51);
      keybindings row lists the comment keys
- [ ] Manual check (maintainer): in the Gallery with a C# document, `Ctrl+/`, `Ctrl+K Ctrl+C`, `Ctrl+K Ctrl+U`, `Shift+Alt+A`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Menus, the editor context menu, `editor.comments.*` as public properties.

## Suggested commit message

`feat(view): add the toggle comment commands`
