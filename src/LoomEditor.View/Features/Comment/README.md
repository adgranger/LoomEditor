# Comment

**Role:** toggles line comments and block comments (`Ctrl+/`, `Shift+Alt+A`, `Ctrl+K Ctrl+C`, `Ctrl+K Ctrl+U`).

**Monaco sources:** `src/vs/editor/contrib/comment/browser/` (`blockCommentCommand.ts`, `lineCommentCommand.ts`, `comment.ts`).

**Files:**
- `BlockCommentCommand.cs` — the edit command that wraps the selection in the block comment tokens of the language, or removes them.
- `CommentFeature.cs` — the feature; it registers nothing yet: the actions come in P5-51.
- `CommentCommands.cs` — command ids.

**Tests:** `tests/LoomEditor.View.Tests/Features/Comment/`.

**Known limits:** the line comment command (P5-49) and the actions (P5-51) are not there yet.
