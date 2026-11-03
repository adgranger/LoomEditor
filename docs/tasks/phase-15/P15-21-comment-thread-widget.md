# P15-21 — Comment thread widget: shell, header, comments, preview, theme, avatar

- **Status:** todo
- **Depends on:** P15-20
- **Size:** M

**Recommended model:** Sonnet

## Goal

`CommentThreadWidget` displays a thread (header with state and actions, the list of comments with avatars and Markdown bodies, the collapsed preview) on Avalonia. Writing
comments comes in P15-22.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.cs`, `CommentThreadWidget.Header.cs`, `CommentThreadWidget.Comments.cs`,
`CommentThreadWidget.Preview.cs`, `CommentThreadWidget.Theme.cs`, `CommentAvatar.cs`.

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/Comments/` — the same 6 files (twins)
- `tests/LoomEditor.Avalonia.Tests/Controls/CommentThreadWidgetTests.cs` — create

## Design decisions (already made — do not change)

- Comment bodies use the P15-18 `MarkdownElementBuilder`. `RichTextBlock` → `SelectableTextBlock` as in P15-18.
- `CommentAvatar`: an `Ellipse`/`Border` with initials, or the image when the thread gives one (`Bitmap` loaded by the host callback, as WinUI). No `PersonPicture` (Avalonia has
  none).
- Code font of the widget: `BareFontInfo.DefaultFontFamilyForCurrentOS` instead of WinUI's hard-coded `"Cascadia Mono, Consolas, Courier New"`.

## Tests

- [ ] New: `Widget_ShowsEveryComment`; `Header_ShowsResolvedState`; `Collapsed_ShowsPreviewOnly`; `Avatar_NoImage_ShowsInitials`; `Theme_Dark_UsesThemeColors`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The composer and suggestions (P15-22), the zone (P15-23).

## Suggested commit message

`feat(avalonia): comment thread widget display`
