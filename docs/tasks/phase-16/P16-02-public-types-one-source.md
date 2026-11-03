# P16-02 — The other copied public types and namespace-only partials from one source

- **Status:** todo
- **Depends on:** P16-01
- **Size:** S (moves)

**Recommended model:** Sonnet

## Goal

Every public UI-free type that phase 15 copied into `LoomEditor.Avalonia` (ADR 0016 decision 4), and every control partial whose twin differs only in its namespace,
is compiled from one file in `src/LoomEditor.HostShared/`. Most of them are under Sonar's 10-line threshold; they move for consistency, so that no copied file is left.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). Diff each pair first: `rtk git diff --no-index <winui file> <avalonia file>`.

## Files to create or change

`git mv` from `src/LoomEditor.WinUI/Controls/…` to `src/LoomEditor.HostShared/Controls/…` (header block), then delete the Avalonia twin:

| Path under `Controls/` |
|---|
| `EditorAccessibilitySupport.cs`, `EditorBracketPairGuides.cs`, `EditorMinimapSide.cs`, `EditorScrollChangedEventArgs.cs`, `EditorWordWrap.cs`, `EditorWrappingIndent.cs` |
| `GutterClickedEventArgs.cs` (with `GutterArea`) |
| `LoomEditor.GotoDefinition.cs`, `LoomEditor.LanguageFeatures.cs` |
| `DiffEditor/DiffBlockAction.cs`, `DiffEditor/DiffBlockActionInvokedEventArgs.cs`, `DiffEditor/DiffBlockActionsVisibility.cs`, `DiffEditor/DiffLine.cs`, `DiffEditor/DiffLineActionInvokedEventArgs.cs`, `DiffEditor/DiffSide.cs` |

## Design decisions (already made — do not change)

- A pair that differs in more than its `namespace` line, `using` lines and `…`/`...` in XML doc stays as it is: list it in the Result section.
- Keep the WinUI XML doc.

## Tests

- [ ] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] None of the listed files exists under `src/LoomEditor.WinUI/Controls` or `src/LoomEditor.Avalonia/Controls` any more
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Files with any framework call (they get host parts in their own cards).

## Suggested commit message

`refactor(hosts): compile the copied public types of both controls from one source`
