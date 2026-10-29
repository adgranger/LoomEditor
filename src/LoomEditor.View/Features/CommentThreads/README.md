# CommentThreads

**Role:** adds the `loom.commentThreads.add` command (no default keybinding): it asks the host to add a comment thread
on the cursor line. It also works in a read-only editor.

**Monaco source:** none (ADR 0014).

**Files:**
- `CommentThreadsFeature.cs` — registers the command; it calls `RequestAdd` on the controller, if any.
- `CommentThreadsCommands.cs` — command ids.

The threads live in `LoomEditor.View.Comments.CommentThreadsController`, stored in `EditorContext.CommentThreads` by the
control, not by this feature.

**Tests:** `tests/LoomEditor.View.Tests/Features/CommentThreads/`.
