# P16-16 — Share the comment thread widget logic

- **Status:** todo
- **Depends on:** P16-15
- **Size:** M

**Recommended model:** Sonnet

## Goal

The comment thread widget's model handling (rebuild, header summary, suggestions and Apply, composer state, theme values) exists once.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget*.cs` and their twins.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.cs` | 28+25, 91+26 | constructor part, `OnChanged`, `Rebuild`, `UpdateCommentedText` | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.Header.cs` | 26+27, 54+22 | header text, `CollapsedSummary` | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.Suggestions.cs` | 9+31, 43+62 | whole file (`OnLineTextChanged`, `Refresh`, `ScheduleRebuild`, `SuggestionOptions`, `SuggestionView`, `OnApply`) | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.Theme.cs` | 9+33 | `ApplyTheme` | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.Composer.cs` | 76+18, 105+17 | parts of `CreateComposer`, `UpdateComposer` | WinUI same path |

## Files to create or change

- `src/LoomEditor.HostShared/Controls/Comments/CommentThreadWidget.Suggestions.cs` — from the WinUI file; `Dispatcher.UIThread.Post` / `DispatcherQueue` → `HostDispatcher.Post`.
- `src/LoomEditor.HostShared/Controls/Comments/CommentThreadWidget.Model.cs` — new: the duplicated members of `CommentThreadWidget.cs`, `.Header.cs`, `.Composer.cs` (state and text decisions only).
- `src/LoomEditor.HostShared/Controls/Comments/CommentThreadWidget.Theme.cs` — `ApplyTheme` computing the colors; brush creation → host member.
- Each host keeps `CommentThreadWidget.cs` (base class, constructor), `.Header.cs`, `.Composer.cs`, `.Comments.cs`, `.Preview.cs` with their framework code.

## Design decisions (already made — do not change)

- `SuggestionBlockView`, `SuggestionBlockTheme`, `MarkdownBuildOptions` are host types with the same names in both hosts: shared code uses them by name.

## Tests

- [ ] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`SuggestionBlockView`, `CommentComposerToolbar` (P16-17).

## Suggested commit message

`refactor(hosts): share the comment thread widget logic`
