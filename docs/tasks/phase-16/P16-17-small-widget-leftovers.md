# P16-17 — Share the leftovers of the suggestion block, composer toolbar, suggest and hover widgets

- **Status:** todo
- **Depends on:** P16-16
- **Size:** S

**Recommended model:** Sonnet

## Goal

The last duplicated blocks of the widgets exist once.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). The files below and their WinUI twins under `src/LoomEditor.WinUI/Controls`.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/Comments/SuggestionBlockView.cs` | 67+17, 105+16 | row building, `Row`/`Code` | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/Comments/CommentComposerToolbar.cs` | 63+32 | `Label`, `MakeTabStop` and the button table | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/Hover/HoverWidget.cs` | 28+22 | the action/link handling | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/Suggest/SuggestWidget.cs` | 16+20 | fields and constants | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/Suggest/SuggestWidget.Rows.cs` | 57+21 | body of `Fill` | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/Suggest/SuggestWidget.Theme.cs` | 7+21 | `ApplyTheme`, `Set` | WinUI same path |

## Files to create or change

- For each file: a shared `src/LoomEditor.HostShared/Controls/<Folder>/<File>.cs` or `<Type>.Logic.cs` with the duplicated members, the rest in `<File>.Host.cs` (or the host file keeps its name when
  it keeps the base class). `HoverWidget.Theme.cs` (same shape as `SuggestWidget.Theme.cs`) moves with it.

## Design decisions (already made — do not change)

- The theme files use the host member `SetBrushColor` of P16-14's shape (same signature).

## Tests

- [ ] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Markdown builders (`MarkdownElementBuilder*`, not in Sonar's list).

## Suggested commit message

`refactor(hosts): share the remaining widget logic`
