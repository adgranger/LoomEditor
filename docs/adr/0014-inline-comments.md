# ADR 0014 — Inline comment threads: data model, hosting, Markdown, suggestions, keyboard, accessibility

- **Status:** accepted
- **Date:** 2026-10-29 (reviewed the same day against the code: decisions 4, 7 and 11 were corrected before any card started)

## Context

Phase 11 ([roadmap](../roadmap/phase-11-inline-comments.md)) adds **inline comment threads**, like the review comment box of a GitHub pull request: under a code line (in the
editor, or on either side of the diff editor) a bubble with a title ("Comment on line 13"), a Markdown toolbar, a multi-line text box, a "Preview" toggle and "Comment" /
"Cancel" buttons. Once a thread has comments, they are shown as Markdown. A fenced block ` ```suggestion ` is shown as a small red / green diff ("Suggested change") with an
**Apply suggestion** button that edits the model.

Monaco has **no** such widget in `src/vs/editor` (VS Code's Comments API lives in `src/vs/workbench/contrib/comments`, on top of the editor's view zones and zone widgets).
This is therefore a LoomEditor feature, **not a port**: cards say "Sources: none (specified in this card)", have no Monaco header and no "port the tests first" rule. Two Monaco
concepts are kept because they fit: the **view zone** (a gap in the text layout that reserves vertical space, ADR 0012 decision 6) and the **content widget** (an element
positioned over the editor and repositioned when it scrolls). What exists today and what does not (checked in the code):

- View zones exist (`ViewZone`, `ViewZones`, `IViewZoneChangeAccessor`, all `internal`, `src/LoomEditor.View/Layout/`), but **every zone today is painted by the renderer**
  (`ViewZoneKind.Empty`, `DeletedLines`, `Collapsed`; `TextViewRenderer.ViewZones.cs`). **No code places a XAML element over a zone yet**: phase 11 is the first.
- The visible zones and their positions are available as `ViewLayout.GetWhitespaceViewportData()` → `ViewWhitespaceViewportData(Id, AfterLineNumber, VerticalOffset, Height)`;
  a zone's `Id` **is** its whitespace id (`ViewZones.AddZone`).
- The control template (`src/LoomEditor.WinUI/Themes/Generic.xaml`) is one `Grid` cell, in this order: `PART_Canvas` (Win2D text), `PART_FindWidget`, `PART_OverlayCanvas`
  (hover, completion list), the two scroll bars. The overlay canvas is **above** the find widget.
- Keys: `LoomEditor.OnKeyDown` and `OnCharacterReceived` already do nothing when the control itself is not focused (`HasFocus => FocusState != Unfocused`): the find widget's
  text boxes rely on this. The IME (`LoomEditor.Ime.cs`) only enters on `GotFocus` when `HasFocus`.
- The list of features of an editor is public and replaceable (`LoomEditor.Features`, and `LoomDiffEditor` passes its own list to its two editors).
- `IEditorContext` is a **public** interface; per-editor controllers are internal properties of `EditorContext` reached through `EditorContextExtensions` (e.g. `SnippetController`, `GetSnippetController()`).

## Decisions

1. **Where the code goes.**

   | What | Where |
   |---|---|
   | public data types `Comment`, `CommentAuthor`, `CommentThread`, `CommentThreadState`, `CommentThreadChangeKind`, `CommentThreadChangedEventArgs` | `LoomEditor.Core/Comments/` (namespace `LoomEditor.Core.Comments`, **public**) |
   | Markdown extensions | `LoomEditor.View/Markdown/` (internal) |
   | toolbar edits, suggestion extraction / edit / diff rows, strings, relative time, avatar initials, the thread view model, `CommentThreadManager`, `CommentThreadsController` | `LoomEditor.View/Comments/` (internal) |
   | the `Widget` zone kind | `LoomEditor.View/Layout/ViewZone.cs` |
   | the command `loom.commentThreads.add` | `LoomEditor.View/Features/CommentThreads/` (`CommentThreadsFeature`, listed in `EditorFeatures.cs`). Not `Features/Comment/`: that folder is *Toggle Line Comment*. |
   | the WinUI widget, its host, the gutter button | `LoomEditor.WinUI/Controls/Comments/` |
   | the Markdown → XAML builder shared with the hover | `LoomEditor.WinUI/Controls/Markdown/` |
   | the public API of the controls | `LoomEditor.CommentThreads.cs`, `DiffEditor/LoomDiffEditor.CommentThreads.cs` (partial files) |

   The layering rule is unchanged: `WinUI → Rendering → View → Core`. Everything that is not a public API type is `internal`. The data types are public in Core because a host
   builds them (it maps its own review system to `Comment` objects), like the language feature contracts of phase 8.

2. **Public API shape.**

   ```csharp
   // LoomEditor (control)
   public CommentThread AddCommentThread(int lineNumber, string? title = null, bool startComposing = false);
   public bool RemoveCommentThread(CommentThread thread);
   public IReadOnlyList<CommentThread> CommentThreads { get; }
   public event EventHandler<CommentSubmittedEventArgs>? CommentSubmitted;        // Thread, Text (Markdown)
   public event EventHandler<CommentThreadEventArgs>? CommentCancelled;           // Thread
   public event EventHandler<AddCommentRequestedEventArgs>? AddCommentRequested;  // LineNumber (gutter button, command)
   public event EventHandler<SuggestionAppliedEventArgs>? SuggestionApplied;      // Thread, Comment, LineNumber
   public bool CommentGutterButtonEnabled { get; set; }                           // default false (P11-21)

   // LoomDiffEditor (P11-22)
   public CommentThread AddCommentThread(DiffSide side, int lineNumber, string? title = null, bool startComposing = false);
   public bool RemoveCommentThread(CommentThread thread);
   public IReadOnlyList<CommentThread> CommentThreads { get; }                    // original's, then modified's
   public DiffSide GetSide(CommentThread thread);
   public event EventHandler<CommentSubmittedEventArgs>? CommentSubmitted;        // same args; side through GetSide
   public event EventHandler<CommentThreadEventArgs>? CommentCancelled;
   public event EventHandler<DiffAddCommentRequestedEventArgs>? AddCommentRequested; // Side, LineNumber (own class, no inheritance)
   public event EventHandler<SuggestionAppliedEventArgs>? SuggestionApplied;
   public enum DiffSide { Original, Modified }
   ```

   - **The control never invents a comment.** On "Comment" it raises `CommentSubmitted` with the text; the host (which knows the user and talks to its server) calls
     `thread.AddComment(Comment.Create(...))`. This keeps authentication and persistence out of the control.
   - "Cancel" on a thread that has **no comments** removes the thread; on a thread with comments it only closes the composer. `CommentCancelled` is raised before the removal.
   - Line numbers are **model** line numbers, 1-based. A thread follows its line when text is edited above it (decision 3).
   - **The API works whatever the feature list is.** The `CommentThreadsController` is created by the control itself (not by a feature) and stored in `EditorContext` (internal property, as `SnippetController`); the feature only registers the command. A host that passes a custom `Features` list without `CommentThreadsFeature` still has `AddCommentThread`; only the command is missing.
   - Public members are listed in `PublicAPI.Unshipped.txt` (Core and WinUI). `IEditorContext` (public) is **not** changed.

3. **Data model.** All in `LoomEditor.Core.Comments`:

   - `CommentAuthor(string Name, string? AvatarUrl = null)` (record). The widget draws the initials in a circle; an image is **not** loaded in phase 11 (no network code).
   - `Comment` (immutable record): `Id`, `Author`, `Body` (Markdown), `CreatedAt`, `UpdatedAt?`; `Comment.Create(author, body, createdAt?)` makes the id.
   - `CommentThreadState`: `Open`, `Resolved`.
   - `CommentThread` (class): `Id`, `Title` (nullable: "Comment on line N" when `null`), `LineNumber` (read-only for the host, maintained by the editor), `State` (settable; `Resolved` collapses the widget),
     `Comments`, `AddComment`, `RemoveComment(id)`, `ReplaceComment(comment)`, one `Changed` event with a `Kind`. No `INotifyPropertyChanged`, no `ObservableCollection` (no binding, AOT).
   - A thread belongs to **one** editor; its constructor is `internal` (created by `AddCommentThread`).
   - **Anchoring.** `CommentThreadManager` keeps one **model decoration** per thread (whole line, `NeverGrowsWhenTypingAtEdges`), like Monaco's zone widgets keep a tracked range. On `TextModel.DecorationsChanged` it moves the thread (`LineNumber`) and its zone (`AfterLineNumber` + `LayoutZone`). This is required: `ViewZones` re-derives a zone's position from `ViewZone.AfterLineNumber` when the line mapping changes (`RecomputeWhitespaceProps`), so a zone whose field is not updated would jump back. A deleted line collapses the decoration; the thread stays at the line that took its place (never deleted by an edit).
   - Not stored in the thread (UI state): the draft, the Preview toggle.

4. **Where the widget lives: a view zone that reserves the height, and a WinUI element in a dedicated zone-widget layer.**

   - For each thread the manager adds a `ViewZone` of the new kind `ViewZoneKind.Widget` after the thread's line (`ShowInHiddenAreas = false`, `SuppressMouseDown = true`, `Data` = the thread). The renderer paints **nothing** for it (its `DrawViewZones` already skips unknown kinds; a test pins it). `HeightInPx` is the element's height, so the text below is pushed down.
   - **A new optional template part `PART_ZoneWidgetCanvas`** (a `Canvas`) is declared in `Generic.xaml` **between `PART_Canvas` and `PART_FindWidget`**. Reason: `PART_OverlayCanvas` is above the find widget; a comment box scrolled to the top would cover the find widget. In the new layer the widgets sit above the text and **below** the find widget, the hover, the completion list and the scroll bars. The layer canvas is clipped to its own bounds (`Clip` = a `RectangleGeometry` updated on `SizeChanged`; a `Canvas` does not clip its children). It reuses the existing `OverlayHost` class (it is not hover-specific: `Show`, `Measure`, `Hide`, `Contains`). The pointer guard (`IsFromOverlay`) covers both layers.
   - **Position:** from `ViewLayout.GetWhitespaceViewportData()` (only the zones in the viewport): `Top = VerticalOffset − ScrollTop`, `Left = LayoutInfo.ContentLeft`, `Width = LayoutInfo.ContentWidth` minus a margin (Monaco's `contentWidth` already excludes the minimap and the vertical scroll bar). A widget whose zone is not in that list is `Collapsed`. The widget does **not** follow the horizontal scroll. Positions are applied **from the control's draw pass** (`OnCanvasDraw`), so text and widgets move in the same frame; scroll and layout events only request a redraw.
   - **Height follows the content.** After the element's size changes the host measures it (`Measure(width, ∞)`) and, when `ceil(DesiredSize.Height)` differs from the zone height, calls `CommentThreadManager.SetHeight` (→ `LayoutZone`). A pass that changes nothing does nothing; at most 3 height passes per dispatcher tick (as `DiffEditorViewZones.MaxPasses`).
   - **Wheel:** `LoomEditor.OnPointerWheelChanged` already receives wheel events that bubble from children; a text box that scrolls itself marks the event handled. Nothing to forward; the cards verify it.
   - **Diff editor.** A comment zone is a *foreign* zone for `DiffEditorViewZones` (not in its own id sets), and `AdditionalLineHeights.Compute` counts foreign zones, so the other side is padded by the same height. In the **inline view** the original editor is `Collapsed`, so original-side threads are not shown (shown again in side-by-side). Threads inside a hidden unchanged region are hidden with it; `startComposing: true` reveals the line first (`DiffEditorViewModel.EnsureOriginalLineIsVisible` / `EnsureModifiedLineIsVisible`).
   - **Rejected alternatives.** (a) A widget drawn by the renderer like the diff zones: no text box, no IME, no accessibility. (b) Reusing `PART_OverlayCanvas`: covers the find widget (above). (c) A `Popup`: does not clip with the editor and has its own focus rules.

5. **Markdown rendering reuse.** `MarkdownParser.Parse(text, options)` gets `MarkdownParserOptions.Extended` (comments): blockquotes (`> x`), strikethrough (`~~x~~`), underline (`<u>x</u>`, `<ins>x</ins>`). Without it (the hover) the output is identical to today. The XAML building code of `HoverWidget.Markdown.cs` moves to `MarkdownElementBuilder` (P11-15) and serves both widgets. No HTML engine, no images, no tables, no task lists. Links are drawn as today in the hover; nothing opens them (documented limit).

6. **Suggestion blocks.**
   - **Syntax**: a fenced code block whose info string is exactly `suggestion`. The parser already gives `MarkdownCodeBlock(Language: "suggestion", Code)`; `SuggestionExtractor` lists them with their index. An **empty** block means "delete this line" (replacing a line by a blank line is not expressible in phase 11).
   - **Diff rows** (`SuggestionDiff`): the old text is the thread's current line. Rows: `Removed(old)` then one `Added` per new line; when a new line equals the old one it is shown as a `Context` row between the added ones; a suggestion equal to the old line is `IsNoOp` ("No changes"). No intra-line highlighting.
   - **Apply suggestion** is a method of `CommentThreadsController` (commands in this code base take no arguments: `EditorCommand(string id, Action execute)`), not a command. It builds **one** `TextEdit` (whole line content, or the line and its end of line for a deletion; the model's `Eol` between lines) and applies it between two undo stops (`Model.PushStackElement()`, `ViewModel.ExecuteEdits(...)`, `Model.PushStackElement()`, as `CoreEditingCommands` does), so one `Ctrl+Z` undoes it. It returns an `ApplySuggestionResult` (`Applied`, `ReadOnly`, `Stale`, `NoOp`, `NotFound`): refused when the editor is read-only (`IEditorContext.IsReadOnly`; the original side of a diff editor is read-only unless `OriginalEditable`), or when the line differs from the text the suggestion was drawn against. It raises `SuggestionApplied`. The host decides what to do with the thread.

7. **Keyboard and focus.**
   - **Already true in the code**: while a child of the control has the focus, `HasFocus` is `false`, so `OnKeyDown` hands the key to the base class without running a keybinding, `OnCharacterReceived` inserts nothing, the caret stops blinking and the IME context stays inactive. This is how the find widget's text boxes work. Phase 11 keeps it and **adds tests-by-checklist, not new routing**: the card verifies each path and adds the explicit `IsFromOverlay`-style guard only where a path does not check `HasFocus` (pointer moved / released over a widget, access keys).
   - **Tab / Shift+Tab** move between the widget's controls by standard XAML focus navigation (a WinUI `TextBox` never inserts a tab; there is no `AcceptsTab` property). The toolbar is one tab stop with arrow-key (roving) focus.
   - **Escape** in the composer: with an **empty** draft it cancels (as the Cancel button; the focus returns to the editor, caret on the thread's line); with a non-empty draft it does nothing. **Ctrl+Enter** submits; `Enter` inserts a newline. **Ctrl+B / Ctrl+I / Ctrl+K** apply bold / italic / link. The widget handles these keys in its own `KeyDown` and marks only them handled.
   - **Focus out of a removed widget:** before a widget that holds the focus is removed (Cancel on an empty thread, `RemoveCommentThread`), the focus is moved to the editor first; otherwise XAML moves it to an arbitrary element.
   - `startComposing: true` moves the focus into the text box once the widget is in the visual tree.
   - The command `loom.commentThreads.add` has **no default key** (phase 10 README §3.2); no keybinding context key is added.

8. **Accessibility.** UIA names with `AutomationProperties.Name` (strings in `CommentStrings`): the widget `"Comment thread on line {N}"`, each comment `"{author}, {relative time}"`, the twelve toolbar buttons, the text box `"Comment text"`, `"Preview"`, `"Suggested change"` (with `FullDescription` = the diff described in words) and `"Apply suggestion"`. Adding a thread with `startComposing`, posting a comment and applying a suggestion are announced through the existing announcer (`EditorContext.Announcer`, `IEditorAnnouncer.Announce(string)`, which raises a UIA notification). High contrast uses the system colors as the find widget does (P7-23). The suggestion diff does not rely on color alone: rows start with `-` / `+`.

9. **Native AOT.** No reflection, no `dynamic`, no `x:Bind` / `{Binding}` to view models (elements are built in code, like the hover and the completion list), no reflection-based JSON, no new `Regex` (the existing `[GeneratedRegex]` of the Markdown parser stays). The self-check P11-24 runs the logic in the published exe; the **WinUI widget itself is not covered** by it (manual plan only).

10. **Strings.** All user-visible English strings are in `CommentStrings` (View). Localization is out of scope. The reference screenshot is French ("Commentaire sur Ligne 13"); the Gallery does not translate it.

11. **What is verified by machines, and what is not.** Logic (steps A and B of the phase) is unit tested. There is no WinUI test project: every WinUI card has **measurable** manual checks (positions in pixels, counts, event logs shown by the Gallery) in `docs/testing/phase-11-manual-tests.md`, and the Gallery shows a small diagnostics line (zone height vs widget height, height passes) so a mismatch is visible, not guessed. Agents do not run the Gallery UI; the close card must say so.

## What is not delivered (listed for the close card)

Reactions, comment edit / delete UI, mentions, attachments, avatar images, localization, original-side threads in the inline diff view, "commit suggestion", a comments panel / list, persistence (the host stores threads), drag to select a range with the "+" button, opening links. Line ranges were added in phase 12 (last section).

## Changes during implementation (2026-10-29)

Cards 1 to 11 and 24 recorded no change to a decision. The others:

- **`Border` is sealed in WinUI 3** (P11-12, P11-19): `CommentThreadWidget` and `SuggestionBlockView` are `UserControl`s whose content is the bordered box, and `CommentAvatar` is a static factory. They cannot implement `IDisposable` (CsWinRT maps it to `IClosable`): `CommentThreadWidget.Detach()` unsubscribes.
- **Keys read in `PreviewKeyDown`** (P11-14, P11-17, P11-18), not `KeyDown` as decision 7 says: the WinUI `TextBox` handles `Enter` and some shortcuts first. Escape, `Ctrl+Enter`, `Ctrl+B` / `I` / `K` are read there.
- **No routing code was added** (P11-14): all six paths of decision 7 were already correct (`HasFocus` is false while a child has the focus). After Submit / Cancel / Escape the widget raises `FocusReturnRequested` and the host calls `ReturnFocusToEditor`.
- **Height** (P11-13): the pass measures the widget's bordered box (`MeasureContentHeight`), not the widget.
- **Colors** (P11-16, P11-17, P11-19, P11-20): ids missing from the built-in defaults use fallbacks; the fallback diff colors had a wrong alpha argument, fixed in P11-20.
- **Accessibility** (P11-20): a `CommentThreadAutomationPeer` (control type `Group`) was needed, since `UserControl` and `Grid` create no peer.
- **Gutter button** (P11-21): hit test reuses the `MouseTarget` of `OnPointerMoved`; `CommentGutterButtonEnabled` is a public dependency property.
- **Diff editor** (P11-22): no alignment code change; two zones after one line give two alignment entries whose heights add up.
- **Gallery** (P11-23): no `--uia-selfcheck` switch exists; the plan uses the button. `--comments-selfcheck` is the new switch (P11-24); the CsWinRT analyzer forbids collection expressions typed `IEnumerable<T>` there.

## Phase 12: line ranges (2026-10-29)

A thread can cover a range of whole lines, as a GitHub review does (`start_line` … `line`). Cards P12-01 to P12-14 recorded no change to these decisions.

- **Lines, not columns**: the API takes two line numbers, never a `Range`. `CommentLineRange.FromSelection` is the only place a selection becomes lines (a selection ending at column 1 of a later line drops that line).
- **`LineNumber` is the last line** (the box and the gutter rule are unchanged); new `StartLineNumber`; equal for a single-line thread. One `Changed` with `LineChanged` when either changes.
- **Source compatible**: only overloads and added properties (`AddCommentThread(start, end, ...)` on both editors, `StartLineNumber` on the three event args); `1 <= start <= end <= LineCount` else `ArgumentOutOfRangeException`.
- **Edits**: the same single decoration spans `(start, 1)` to `(end, maxColumn)`, `NeverGrowsWhenTypingAtEdges`; deleting the whole range collapses it to one line; a flush clamps both ends.
- **Highlight**: the class `comment-thread-range`, theme id `loom.commentThreadRangeBackground`, shown only while the thread is multi-line and open, by switching the options of the same decoration.
- **Suggestions replace the range**: the old text is the range text (lines joined with `\n`); the diff is a line LCS of N old against M new lines; stale means the range text changed; apply is one undo step, caret at `(start, 1)`. The toolbar seeds the block with the range text.
- **Native AOT**: `--comments-selfcheck` has four range checks (15 of 15 under AOT, P12-14).
- **Not delivered**: character-precise ranges, dragging or shift-click in the gutter to select lines, a gutter bar or overview-ruler mark, overlap rules, and the rest of the list above.
