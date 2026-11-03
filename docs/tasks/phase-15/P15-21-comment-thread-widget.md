# P15-21 — Comment thread widget: shell, header, comments, preview, theme, avatar

- **Status:** done
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

- [x] New: `Widget_ShowsEveryComment`; `Header_ShowsResolvedState`; `Collapsed_ShowsPreviewOnly`; `Avatar_NoImage_ShowsInitials`; `Theme_Dark_UsesThemeColors`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] Card status and `docs/tasks/README.md` updated

## Deviations

- `CommentAvatar` draws the initials only: the WinUI twin ignores `CommentAuthor.AvatarUrl` (no image is downloaded), so there is no `Bitmap` callback to port.
- The automation peers of the WinUI twin (`CommentThreadAutomationPeer`, `CommentRowPanel`) are replaced by `AutomationProperties.ControlTypeOverride = Group` on the widget and on
  each comment row; the full description of a row goes to `HelpText`.
- `CommentThreadWidget.Composer.cs` is a stub that holds the text box and the preview; P15-22 replaces it with the composer, its buttons and the suggestion blocks, and adds their
  colors to `ApplyTheme` (which has no `highContrast` parameter yet for the same reason). `Announce` is declared but only used from P15-22.

## Out of scope

The composer and suggestions (P15-22), the zone (P15-23).

## Suggested commit message

`feat(avalonia): comment thread widget display`
