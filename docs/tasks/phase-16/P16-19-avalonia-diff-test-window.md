# P16-19 — One helper for the Avalonia diff editor test window

- **Status:** done
- **Depends on:** P15-34
- **Size:** S

**Recommended model:** Sonnet

## Goal

The window setup that five diff editor test classes repeat (create, show, set the test font, wait for the diff, settle frames) is one helper.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `tests/LoomEditor.Avalonia.Tests/Controls/DiffEditor/*.cs`, `tests/LoomEditor.Avalonia.Tests/TestHelpers/EditorWindow.cs`.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `tests/…/DiffEditor/HiddenRegionsTests.cs` | 21+21 | `Open`, `Settle` | `DiffCommentThreadsTests` 18+21, `DiffLineSelectionTests` 17+19, `DiffBlockActionsTests` 26+19 |
| `tests/…/DiffEditor/AccessibleDiffViewerTests.cs` | 18+13 | setup | `DiffAlignmentTests` 16+14 |

## Files to create or change

- `tests/LoomEditor.Avalonia.Tests/TestHelpers/DiffEditorWindow.cs` — create: `internal static class DiffEditorWindow` with
  `Task<(LoomDiffEditor Diff, Window Window)> OpenAsync(string original, string modified, double width = 1000, double height = 300, Action<LoomDiffEditor>? configure = null)`
  and `Task SettleAsync(Window window, int frames = 5)`.
- The six test classes use it; their private `Open` / `Settle` copies go.

## Design decisions (already made — do not change)

- Each test keeps its name, its asserts and its timing (same frame count and delay as today).

## Tests

- [x] No new test; every existing diff editor test still passes (same count as before).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [x] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Non-diff tests.

## Suggested commit message

`test(avalonia): one helper for the diff editor test window`
