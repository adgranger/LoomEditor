# Phase 16 — Shared host logic (WinUI and Avalonia)

**Done when:** the code that `LoomEditor.WinUI` and `LoomEditor.Avalonia` (and their two Galleries) have in common exists once, and SonarCloud's
"Duplication on New Code" on PR #26 is ≤ 3% (it was 23.2%: 4,500 of 19,366 new lines). Neither control changes behavior or public API.

Planned 2026-11-03. Decision: [ADR 0017](../adr/0017-shared-host-logic.md). Rules: [docs/tasks/phase-16/README.md](../tasks/phase-16/README.md).
Task cards: [docs/tasks/phase-16/](../tasks/phase-16/README.md) (24 cards).

The technique is **shared host source**: `src/LoomEditor.HostShared/` holds files compiled into both control assemblies, each in its own namespace; the
framework-specific lines go to small `.Host.cs` parts. Pure helpers that name no host type go down to `LoomEditor.Rendering` as usual.

## Where the duplicated lines are, and which card removes them

| Card | Files (Avalonia side unless stated) | Sonar lines |
|---|---|---|
| P16-01, P16-02 | `CommentEventArgs`, `DiffAddCommentRequestedEventArgs`, the other copied public types | 154 |
| P16-03 … P16-07 | `LoomEditor.Reveal`, `.Snippets`, `.DiffGutter`, `.HostHooks`, `.ViewZonePointer`, `.CommentThreads`, `.Keyboard`, `.cs`, `.Accessibility`, `.Pointer`, `.Scrolling` | 635 |
| P16-08 … P16-13 | `HideUnchangedRegionsController`, `DiffEditorViewZones`, `LoomDiffEditor.*`, `DiffGutterControllers`, `AccessibleDiffViewer`, `DiffBlockActionsController`, `DiffLineCheckBoxesController` | 1,828 |
| P16-14 | `FindWidget.*` | 263 |
| P16-15 … P16-17 | `CommentGutterController`, `CommentZoneHost.*`, `CommentThreadWidget.*`, `SuggestionBlockView`, `CommentComposerToolbar`, `SuggestWidget.*`, `HoverWidget` | 589 |
| P16-18 | `AvaloniaLineLayout` / `Win2DLineLayout`, `AvaloniaTextMeasurer` / `Win2DTextMeasurer` (both sides are new code) | 136 |
| P16-19 | `tests/LoomEditor.Avalonia.Tests/Controls/DiffEditor/*` | 107 |
| P16-20 … P16-23 | Avalonia Gallery: `Lsp/*`, `MainWindow.*`, `DiffDemoWindow.*`, `HostApiDemoWindow.*` | 788 |
| **Total** | | **4,500** |

## Checklist

- [ ] ADR 0017: shared host source, host members, `HostDispatcher`, one source for public UI-free types
- [ ] `src/LoomEditor.HostShared/`, the props file, `LOOM_WINUI` / `LOOM_AVALONIA`, `HostSharedSourceTests`; public UI-free types from one source (P16-01, P16-02)
- [ ] `LoomEditor` partials shared (P16-03 … P16-07)
- [ ] `LoomDiffEditor` and its controllers shared (P16-08 … P16-13)
- [ ] Find, comment, suggest and hover widgets shared (P16-14 … P16-17)
- [ ] Rendering helpers in `LoomEditor.Rendering`; Avalonia diff test window helper (P16-18, P16-19)
- [ ] Galleries share their window logic (P16-20 … P16-23)
- [ ] Self-checks of both Galleries green, Native AOT publish of both Galleries, Sonar duplication ≤ 3% on PR #26 (P16-24)
