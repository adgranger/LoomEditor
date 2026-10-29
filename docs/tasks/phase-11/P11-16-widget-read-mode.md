# P11-16 — The widget (1/4): header, comments and the collapsed "Resolved" state

- **Status:** todo
- **Depends on:** P11-05, P11-15
- **Size:** M

## Goal

`CommentThreadWidget` shows a thread the way a review does: a header ("Comment on line 13" or the thread's title, a "Resolved" badge), the comments one under the other (initials in a circle, author, relative time, the body as rendered Markdown), and a collapsed one-line form for a resolved thread. Theme colors follow the editor theme. The composer and the suggestion block come next (P11-17 … P11-19).

## Sources

Sources: none (specified in this card; ADR 0014 decisions 3, 5, 8). Read first: the placeholder `CommentThreadWidget.cs` (P11-12 / P11-14), `CommentThreadViewModel.cs` (P11-08), `RelativeTime.cs`, `AvatarInitials.cs` and `CommentStrings.cs` (P11-05), `MarkdownElementBuilder.cs` (P11-15), `Hover/HoverWidget.Theme.cs` and how it takes colors from `EditorTheme`, `src/LoomEditor.Themes/` color ids (grep `editorWidget.background`, `editorWidget.border`, `foreground`, `descriptionForeground`, `textLink.foreground`, `textCodeBlock.background`), `Find/FindWidget*.cs` for the way a widget is styled.

## Files to create or change

- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.cs` — change: becomes the shell (state, theme, rebuild)
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Header.cs`, `CommentThreadWidget.Comments.cs`, `CommentThreadWidget.Theme.cs`, `CommentAvatar.cs` — create
- `src/LoomEditor.Themes/…` — change only if a color id the card names does not exist (then use the closest existing one and say so; do not add theme data in this card)
- `docs/testing/phase-11-manual-tests.md` — change: section "P11-16"

## Design decisions (already made — do not change)

- **Layout (top to bottom)** in a `Border` (1 px border, corner radius 6, no shadow): header row; one `Grid` per comment; (P11-17 appends the Reply / composer area). A 1 px separator line between comments. Padding 8 px; font = the editor's UI font size (the hover's size), the body font size the same.
- **Header:** title = `viewModel.Title` (bold) ; when `thread.State == Resolved` a small "Resolved" badge (`CommentStrings.Resolved`) and, when collapsed, the first comment's first line of plain text (`MarkdownParser.Parse(...).ToPlainText()` cut to one line) and the comment count ("· 3 comments"); the whole header is a button that toggles `viewModel.ToggleCollapsed()` **only** for a resolved thread (otherwise it is not interactive). Glyph `▸` / `▾` as text.
- **Comment row:** `CommentAvatar` (a 28 px `Border` with `CornerRadius = 14`, initials from `AvatarInitials.Get(Author.Name)`, background = one of eight fixed brushes of a theme-independent palette defined in the file, chosen by `AvatarInitials.PaletteIndex(Author.Name, 8)`, white text; `AvatarUrl` is ignored in phase 11, a comment in the code says so), then author name (semi-bold), relative time (`RelativeTime.Format(updatedOrCreated, DateTimeOffset.UtcNow)`, dimmed, with `" (edited)"` when `UpdatedAt` is set), then the body from `MarkdownElementBuilder.Build(MarkdownParser.Parse(body, MarkdownParserOptions.Extended), …)` in a `StackPanel`. An empty body shows nothing. Text is selectable where `RichTextBlock.IsTextSelectionEnabled` allows it.
- **Rebuild strategy:** rebuild the comment rows on `viewModel.Changed` and `thread.Changed`; no incremental diffing (threads are small). Keep the focus: do not rebuild the composer area (P11-17 owns it) — the rows and the composer area are two separate child panels. The temporary text box and buttons of P11-14 move into the composer panel unchanged (P11-17 replaces them).
- **Relative times** are computed at build time only; no timer (a stale "2 minutes ago" is acceptable and listed in the phase README limits).
- **Theme:** `ApplyTheme(EditorTheme)` resolves: background `editorWidget.background`, border `editorWidget.border`, text `editor.foreground`/`foreground`, dim text `descriptionForeground`, link `textLink.foreground`, code background `textCodeBlock.background`, quote bar `textBlockQuote.border` or `descriptionForeground` at 50 %. Missing ids fall back as the hover does. High contrast (P11-20) is handled there.
- **Height:** the widget has no fixed height; `MaxHeight` is **not** set (the zone grows; a very long thread makes a tall gap, accepted).
- The widget stays `internal`; no public surface.

## Tests

- [ ] Automated: none for WinUI (initials and palette index are `AvatarInitials`, tested in P11-05).
- [ ] Manual (section "P11-16", Gallery sample with threads built by code: one comment, three comments with Markdown (bold, italic, strike, underline, inline code, a link, a bullet list, a quote, a fenced block), a resolved thread, a thread with a very long line): (1) text and layout look like a review comment; (2) strike and underline are drawn; (3) the resolved thread is one line and expands on click; (4) light and dark themes and the Gallery theme switch recolor the widget; (5) the widget grows when a comment is added from code and the text below moves.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] The manual section is written; the card result says it was not run by the agent
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The composer, Reply, the suggestion block (until P11-19 a suggestion is drawn as an ordinary code block), avatars from URLs, timers, edit / delete menus, reactions.

## Suggested commit message

`feat(winui): draw comment threads: header, comments and the resolved state`
