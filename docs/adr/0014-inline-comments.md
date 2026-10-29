# ADR 0014 — Inline comment threads: data model, hosting, Markdown, suggestions, keyboard, accessibility

- **Status:** accepted
- **Date:** 2026-10-29

## Context

Phase 11 ([roadmap](../roadmap/phase-11-inline-comments.md)) adds **inline comment threads**, like the review comment box of a GitHub pull request: under a code line (in the
editor, or on either side of the diff editor) a bubble with a title ("Comment on line 13"), a Markdown toolbar, a multi-line text box, a "Preview" toggle and "Comment" /
"Cancel" buttons. Once a thread has comments, they are shown as Markdown. A fenced block ` ```suggestion ` is shown as a small red / green diff ("Suggested change") with an
**Apply suggestion** button that edits the model.

Monaco has **no** such widget in `src/vs/editor` (VS Code's Comments API lives in `src/vs/workbench/contrib/comments`, on top of the editor's view zones and content widgets).
This is therefore a LoomEditor feature, **not a port**: cards say "Sources: none (specified in this card)", have no Monaco header and no "port the tests first" rule. Two Monaco
concepts are kept because they fit: the **view zone** (a gap in the text layout that reserves vertical space, ADR 0012 decision 6) and the **content widget** (an element
positioned over the editor and repositioned when it scrolls).

## Decisions

1. **Where the code goes.**

   | What | Where |
   |---|---|
   | public data types `Comment`, `CommentAuthor`, `CommentThread`, `CommentThreadState` | `LoomEditor.Core/Comments/` (namespace `LoomEditor.Core.Comments`, **public**) |
   | Markdown extensions, the Markdown toolbar edits, suggestion extraction, suggestion edit, suggestion diff rows, the thread view model, relative time, UI strings | `LoomEditor.View/Comments/` and `LoomEditor.View/Markdown/` (internal, pure, no UI) |
   | thread tracking over a model (`CommentThreadManager`), the `Widget` zone kind | `LoomEditor.View/Comments/`, `LoomEditor.View/Layout/` |
   | the feature (commands, context wiring) | `LoomEditor.View/Features/CommentThreads/` (`CommentThreadsFeature`, listed in `EditorFeatures.cs`) |
   | the WinUI widget, the hosting in the overlay layer, the gutter button | `LoomEditor.WinUI/Controls/Comments/` |
   | the public API of the editor and of the diff editor | `LoomEditor.CommentThreads.cs`, `LoomDiffEditor.CommentThreads.cs` (partial files of the controls) |

   The layering rule is unchanged: `WinUI → Rendering → View → Core`. Everything that is not a public API type is `internal`. The data types are public in Core because a host
   application builds them (it maps its own review system to `Comment` objects), exactly like the language feature contracts of phase 8.

2. **Public API shape.** All methods are on the control, and mirror each other on `LoomDiffEditor`.

   ```csharp
   // LoomEditor (control)
   public CommentThread AddCommentThread(int lineNumber, string? title = null, bool startComposing = false);
   public bool RemoveCommentThread(CommentThread thread);
   public IReadOnlyList<CommentThread> CommentThreads { get; }
   public event EventHandler<CommentSubmittedEventArgs> CommentSubmitted;      // Thread, Text (Markdown)
   public event EventHandler<CommentThreadEventArgs> CommentCancelled;         // Thread (draft discarded)
   public event EventHandler<AddCommentRequestedEventArgs> AddCommentRequested; // gutter button / command; LineNumber
   public event EventHandler<SuggestionAppliedEventArgs> SuggestionApplied;    // Thread, Comment, LineNumber
   public bool CommentGutterButtonEnabled { get; set; }                        // default false

   // LoomDiffEditor
   public CommentThread AddCommentThread(DiffSide side, int lineNumber, string? title = null, bool startComposing = false);
   public bool RemoveCommentThread(CommentThread thread);
   public IReadOnlyList<CommentThread> CommentThreads { get; }                 // both sides
   public DiffSide GetSide(CommentThread thread);
   // the same four events, raised for threads of either side (args carry the thread; the side comes from GetSide)
   public enum DiffSide { Original, Modified }
   ```

   - **The control never invents a comment.** On "Comment" it raises `CommentSubmitted` with the text; the host (which knows the user, talks to its server) calls
     `thread.AddComment(new Comment(...))`. This is how GitHub's flow works (post, then the comment appears) and it keeps authentication out of the control. `startComposing: true` opens the composer.
   - "Cancel" on a thread that has **no comments** removes the thread; on a thread with comments it only closes the composer. Both raise `CommentCancelled` first.
   - Line numbers are **model** line numbers, 1-based. A thread follows its line when text is edited above it (decision 3).
   - Public members are listed in `PublicAPI.Unshipped.txt` (Core and WinUI). `AddCommentRequested` carries `AddCommentRequestedEventArgs(int LineNumber)`; the host usually answers with `AddCommentThread(line, startComposing: true)`.

3. **Data model.** All in `LoomEditor.Core.Comments`:

   - `CommentAuthor(string Name, string? AvatarUrl = null)` (record). The avatar is optional; the widget draws the initials in a circle when there is no image (an image is **not** loaded in phase 11: no network code; `AvatarUrl` is stored and exposed for a later card).
   - `Comment` (immutable record): `string Id`, `CommentAuthor Author`, `string Body` (Markdown), `DateTimeOffset CreatedAt`, `DateTimeOffset? UpdatedAt`.
   - `CommentThreadState`: `Open`, `Resolved`.
   - `CommentThread` (class): `Id`, `Title` (nullable: the widget shows "Comment on line N" when `null`), `LineNumber` (read-only for the host, maintained by the editor), `State` (settable; `Resolved` collapses the widget to one line),
     `Comments` (`IReadOnlyList<Comment>`), `AddComment(Comment)`, `RemoveComment(string id)`, `ReplaceComment(Comment)` (same id), and one `Changed` event (`EventHandler<CommentThreadChangedEventArgs>` with a `Kind` enum). No `INotifyPropertyChanged`, no `ObservableCollection` (AOT-safe, no binding).
   - A thread belongs to **one** editor; adding it to another is not supported (it is created by `AddCommentThread`, whose constructor is `internal`).
   - **Anchoring.** The manager keeps one **model decoration** per thread: the whole line range, `NeverGrowsWhenTypingAtEdges`, the same mechanism as Monaco's tracked ranges. When the line moves the decoration moves and the thread's `LineNumber` and its view zone follow. When the line is deleted the decoration collapses and the thread stays at the line that took its place (it is never deleted by an edit).
   - Not stored in the thread (UI state, lost when the thread is removed): the draft text, the Preview toggle, the scroll position of the widget.
   - Out of scope in phase 11: multi-line ranges (a thread has **one** line; the suggestion replaces that line by one or more lines), reactions, edit / delete buttons on comments (the host removes or replaces a comment through the thread; a card can add buttons), `@mentions`, attachments.

4. **Where the widget lives: a view zone that reserves the height, and a WinUI element in the overlay layer.**

   - For each thread the manager adds a `ViewZone` with the new kind `ViewZoneKind.Widget` after the thread's line (`AfterLineNumber = LineNumber`, `ShowInHiddenAreas = false`, `SuppressMouseDown = true`). The renderer paints **nothing** for this kind (the element covers the gap). `HeightInPx` is the height of the element, so the text below is pushed down.
   - The element (`CommentThreadWidget`, a `Border` with the toolbar, text box, etc.) is a child of `PART_OverlayCanvas` (`OverlayHost`, ADR 0011 decision 10), **not** of the text surface. `CommentZoneHost` positions it: `Canvas.Top = zone top − ScrollTop`, `Canvas.Left = gutter width` (`LayoutInfo.ContentLeft`), `Width = content width − vertical scroll bar − 8` (a thread does not scroll horizontally with the text: it stays at the left of the content area, as in VS Code). It listens to `HostScrollChanged`, `HostLayoutChanged` and `ViewZones.ZonesChanged`, and **collapses** (`Visibility.Collapsed`) the elements whose zone is outside the viewport (no virtualization of the XAML; threads are few).
   - **Height follows the content.** After a change of the element's size (text typed, Preview toggled, comment added) the host measures it with the available width and, if the integer height differs from the zone's, sets `HeightInPx` and calls `LayoutZone`. A pass limit (3 per tick, as `DiffEditorViewZones.MaxPasses`) ends any feedback loop; a pass that changes nothing is skipped.
   - **Not in the zone**: the element does not take pointer input through the zone events. `IsFromOverlay` (already used for the hover popup) keeps the editor from treating a press on the widget as a text click. The mouse wheel over a widget scrolls the editor (the host forwards it) unless the widget's own `ScrollViewer` (the text box) can scroll in that direction.
   - **Diff editor.** The comment zone of a thread is a *foreign* zone for `DiffEditorViewZones` (it is not in `_originalOwn` / `_modifiedOwn`), so the existing alignment code pads the other side by the same height; P11-18 verifies this with tests and fixes what the verification finds. In the **inline view** only the modified editor is shown: original-side threads are **not shown** there (decision recorded, shown again in side-by-side view). Threads inside a hidden unchanged region are hidden with it.
   - **Rejected alternatives.** (a) A widget drawn by the renderer like the diff zones: no text box, no IME, no accessibility: rejected. (b) The widget as a child of the editor's text area: the text surface is a Win2D control that clips and redraws everything; XAML children of the overlay canvas are the pattern already used by the hover popup and the completion list. (c) A `Popup`: floats above the window, does not clip with the editor, has its own focus rules: rejected.

5. **Markdown rendering reuse.** `MarkdownParser` (phase 8, `LoomEditor.View/Markdown`) gets an **options** object. With `MarkdownParserOptions.Extended` (comments) it also parses blockquotes (`> x`), strikethrough (`~~x~~`) and underline (`<u>x</u>` and `<ins>x</ins>`, the two tags GitHub accepts) and nested-free lists as before; without it (the hover) the output is byte-identical to today. The XAML builder that `HoverWidget.Markdown.cs` contains is **extracted** to `LoomEditor.WinUI/Controls/Markdown/MarkdownElementBuilder.cs` (P11-12) and used by both widgets, so a Markdown fix lands once. No HTML engine, no images, no tables, no task lists (shown as text, listed in the README of the Markdown folder). Raw HTML other than the two underline tags is shown as text. Links are drawn as `Hyperlink`; opening one raises nothing (the platform default `Launcher` is **not** called: the link text is selectable and the host may add handling later; documented limit).

6. **Suggestion blocks.**
   - **Syntax**: a fenced code block whose info string is exactly `suggestion` (GitHub's syntax). The parser already gives `MarkdownCodeBlock(Language: "suggestion", Code)`; `SuggestionExtractor` lists them with their index in the comment body. The `Code` has no trailing newline; an **empty** block means "delete this line".
   - **Diff rows** (`SuggestionDiff`): the old text is the thread's current line content; rows are `Removed` lines then `Added` lines (GitHub shows exactly this, no context lines). A suggestion equal to the old line is flagged `IsNoOp` (shown as "No changes"). No intra-line highlighting in phase 11.
   - **Apply suggestion**: the button of the widget calls the command `loom.commentThreads.applySuggestion` (arguments: thread id, comment id, suggestion index) which creates **one** edit: replace the full line (and its end of line, when the replacement is empty) by the block's text, written with the model's EOL, through the same path as typing (`PushEditOperations` with an undo stop: one `Ctrl+Z` undoes it). It is refused (command returns `false`, the button is disabled) when the editor is read-only, in the **original** side of a diff editor (read-only), or when the line content at the moment of the click differs from the content the suggestion was rendered against (a stale suggestion: the diff is recomputed and the user clicks again). It raises `SuggestionApplied`. The host decides what to do with the thread (usually mark it `Resolved`); the control does not.
   - Out of scope: multi-line suggestions that replace a **range** of original lines (needs multi-line threads), "commit suggestion" to a repository, batching several suggestions.

7. **Keyboard and focus.** The widget contains real WinUI controls (`TextBox`, `Button`s), so the editor must not steal their keys.
   - The editor's `OnKeyDown` and `OnCharacterReceived` ignore events whose `OriginalSource` is inside the overlay layer (`IsFromOverlay`): this covers keys that bubble from the text box to the control. The editor's own focus state is unchanged (`HasFocus` stays `false` while a child has the focus, so typed characters are not inserted into the text; the caret stops blinking, as for the find widget).
   - **Tab** inside the text box moves the focus to the next control (`AcceptsTab = false`: it is a review comment box, not an editor), `Shift+Tab` to the previous one; the Tab order is: Heading → Bold → … → Suggestion (toolbar), text box, Preview, Cancel, Comment (the toolbar is **one** tab stop with arrow-key navigation, a WinUI `CommandBar`-style roving focus is not used: the toolbar buttons are `IsTabStop = false` except the first, and `Left`/`Right` move between them).
   - **Escape** in the composer: when the draft is **empty** it cancels (same as the Cancel button, focus returns to the editor with the caret on the thread's line); when the draft is **not empty** it does nothing, so typed text is never lost by a stray key: the user presses Cancel. Decision final.
   - **Ctrl+Enter** submits (the "Comment" button); `Enter` inserts a newline. The Comment button is disabled while the text is blank.
   - **Ctrl+B / Ctrl+I / Ctrl+K** in the text box apply bold / italic / link (the toolbar actions); no other global shortcut is handled in the widget. The editor's own shortcuts (`Ctrl+F`, `F12`, …) are **not** active while the focus is in the widget.
   - Opening a thread with `startComposing: true` moves the focus into the text box once the widget is laid out (dispatcher low priority).
   - No keybinding context key is added. The commands of the feature (`loom.commentThreads.add` at the cursor line, `loom.commentThreads.applySuggestion`) are registered **without a default key** (Monaco's `primary: 0` rule of phase 10 README §3.2; the host binds keys if it wants).

8. **Accessibility.** Each part gets a UI Automation name set with `AutomationProperties.Name`: the widget `"Comment thread on line {N}"` (`Group`-like landmark through `AutomationProperties.LocalizedLandmarkType` is **not** used), each comment `"{author}, {relative time}"` (a `ListItem`-like element whose content is read as text), the toolbar buttons `Heading`, `Bold`, `Italic`, `Underline`, `Strikethrough`, `Inline code`, `Code block`, `Link`, `Bulleted list`, `Numbered list`, `Quote`, `Suggest a change`; the text box `"Comment text"`, the toggle `"Preview"`; the diff block `"Suggested change"` with a plain-text description ("Removed: …; Added: …") and the button `"Apply suggestion"`. Adding a thread, posting a comment and applying a suggestion are announced through `AutomationAnnouncer` (ADR 0010, UIA notifications). High contrast: the widget uses the system colors like the find widget does (P7-23). The text of the suggestion diff does **not** rely on red and green alone: rows start with `-` / `+`.

9. **Native AOT.** Nothing new: no reflection, no `dynamic`, no reflection-based JSON, no new `Regex` (the existing `[GeneratedRegex]` of the Markdown parser stays; the new Markdown code, the toolbar edits and the suggestion code are hand written with `string` / `Span`). The data types are plain classes and records. Timestamps are formatted by a pure `RelativeTime` function (no `Intl`, no culture data beyond `DateTime`). The self-check P11-20 proves it on the published exe.

10. **Strings.** All user-visible English strings of the widget are in one internal class `CommentStrings` (View). Localization is out of scope; the class is where a later card plugs a resource loader. The reference screenshot of this feature is French ("Commentaire sur Ligne 13"); the Gallery does not translate it.

## What is not delivered (listed for the close card)

Multi-line thread ranges, reactions, comment edit / delete UI, mentions, attachments, avatar images, localization, original-side threads in the inline diff view, "commit suggestion", a comments panel / list, persistence (the host stores threads), drag to select a range with the "+" button.

## Changes during implementation

None yet. The close card (P11-21) appends the list.
