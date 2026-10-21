# Comment

**Role:** toggles line comments and block comments, with every cursor, using the comment tokens of the language of the document.

**Commands and keys (Windows):**

| Command | Keys |
|---|---|
| `editor.action.commentLine` (toggle line comment) | `Ctrl+/` |
| `editor.action.addCommentLine` | `Ctrl+K Ctrl+C` |
| `editor.action.removeCommentLine` | `Ctrl+K Ctrl+U` |
| `editor.action.blockComment` (toggle block comment) | `Shift+Alt+A` |

All of them need a writable editor with the text focus.

**Monaco sources:** `src/vs/editor/contrib/comment/browser/` (`blockCommentCommand.ts`, `lineCommentCommand.ts`, `comment.ts`).

**Files:**
- `BlockCommentCommand.cs`, `LineCommentCommand*.cs`, `LineCommentType.cs` — the edit commands.
- `CommentActions.cs` — what the commands do to the selections (sort, `ignoreFirstLine`, undo stops).
- `CommentFeature.cs` — registers the four commands; `EditorCommentsOptions.cs` — `insertSpace` and `ignoreEmptyLines` (VS Code's defaults, no public option in phase 5).
- `CommentCommands.cs` — command ids; `JsStrings.cs` — JavaScript string searches.

**Tests:** `tests/LoomEditor.View.Tests/Features/Comment/`.

**Known limits:** no menus; the Linux key of the block comment is ignored.
