# How to use comment threads

A comment thread is a GitHub-review style conversation under one line: a title, comments drawn as Markdown, a composer (toolbar, text box, Preview, Comment / Cancel) and, for a ` ```suggestion ` block, an **Apply suggestion** button.
The host owns the data and stores it; the control draws it and raises events. The design is in [ADR 0014](../adr/0014-inline-comments.md).

```csharp
var thread = editor.AddCommentThread(lineNumber: 12, title: "Review", startComposing: true);   // opens the composer
thread.AddComment(new Comment("c1", new CommentAuthor("Ada"), "Use `const` here.", DateTimeOffset.Now));
```

## The four events (the host's job)

| Event | When | What the host does |
|---|---|---|
| `AddCommentRequested` | the gutter "+" button or the command `loom.commentThreads.add` (no default key) | calls `AddCommentThread(e.StartLineNumber, e.LineNumber, startComposing: true)` |
| `CommentSubmitted` | `Ctrl+Enter` or the Comment button | stores `e.Text`, then `e.Thread.AddComment(...)` with a real id and author; the control adds nothing itself |
| `CommentCancelled` | Cancel / Escape on an empty draft | removes the thread if it has no comment (`RemoveCommentThread`) |
| `SuggestionApplied` | after **Apply suggestion** edited the text | marks the thread resolved (`thread.State`) or removes it; the control does not |

## A suggestion

Write this in a comment; line 12 (or the whole range) is replaced by the block's lines (an empty block deletes the line):

````
```suggestion
const total = items.length;
```
````

One `Ctrl+Z` undoes the edit. Apply is refused (the button is disabled or fails with a tooltip) when the editor is read-only or when the line changed since the comment was written.

## Ranges of lines and exact text

A thread can cover several lines, like GitHub's "lines 12 to 15", or the exact text you selected; the box sits under the last line and the range or text is highlighted while the thread is open.

```csharp
var thread = editor.AddCommentThread(12, 15, title: "Review", startComposing: true);   // whole lines 12 to 15
var word = editor.AddCommentThread(new Range(4, 5, 4, 10));                            // line 4, columns 5 to 10 (end exclusive)
editor.AddCommentRequested += (_, e) => _ = e.TextRange is { } r
    ? editor.AddCommentThread(r, startComposing: true)
    : editor.AddCommentThread(e.StartLineNumber, e.LineNumber, startComposing: true);
```

`e.TextRange` is `null` for whole lines. A range over full lines gives a whole-line thread; read `thread.IsWholeLines`, `StartColumn` and `EndColumn` when you save (a change of columns raises no event).

A suggestion replaces **all the lines that contain the range**, never only the selected text (an empty block deletes them); it is refused when any of them changed. `AddCommentThread(line)` still works.

## Gutter button and diff editor

`CommentGutterButtonEnabled` shows a "+" in the gutter on hover. `LoomDiffEditor` has the same API with a side: `AddCommentThread(DiffSide.Modified, line, ...)`, `GetSide(thread)`; the other side gets a blank gap so the lines stay level ([use-the-diff-editor.md](use-the-diff-editor.md)).

## Limits

No dragging in the gutter to select lines, no suggestion that replaces only the selected text; no reactions, edit / delete UI, mentions, attachments, avatar images, localization or "commit suggestion"; links are drawn but not opened; no persistence.
The WinUI widget has not been checked by the agents (manual plans: [phase-11-manual-tests.md](../testing/phase-11-manual-tests.md), [phase-12-manual-tests.md](../testing/phase-12-manual-tests.md)).
