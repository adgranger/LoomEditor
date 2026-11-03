# P15-02 — Move the UI-free helpers of `LoomEditor.WinUI` into View

- **Status:** todo
- **Depends on:** P15-01
- **Size:** S (moves)

**Recommended model:** Sonnet

## Goal

The internal WinUI helpers that use no WinUI or Windows API live in `LoomEditor.View`, so the Avalonia control reuses them instead of copying them.

## Sources

Sources: none (specified in this card; [phase-15/README.md](README.md) "not a port"). Read first: the four files below and their callers
(`rtk grep -rn "SashLayout\|DiffGutterLanes\|DiffViewerRowItem\|EditorDecorationTarget" src tests`).

## Files to create or change

| From (`src/LoomEditor.WinUI/Controls/DiffEditor/`) | To (`src/LoomEditor.View/DiffEditor/`) |
|---|---|
| `SashLayout.cs` | `SashLayout.cs` |
| `DiffGutterLanes.cs` | `DiffGutterLanes.cs` |
| `DiffViewerRowItem.cs` | `DiffViewerRowItem.cs` |
| `EditorDecorationTarget.cs` | `EditorDecorationTarget.cs` |

- namespace `LoomEditor.View.DiffEditor`; `using`s of their WinUI callers updated
- `tests/LoomEditor.View.Tests/DiffEditor/` — tests for `SashLayout` and `DiffGutterLanes` (they had none: no WinUI test project)

## Design decisions (already made — do not change)

- `git mv`, namespace change only. They stay `internal` (View already has `InternalsVisibleTo` for WinUI; P15-05 adds Avalonia).
- **Not moved** (they use the control or Windows): `RendererLineMetrics` (holds the WinUI control), `ScreenReaderDetector` (P/Invoke in `Interop/NativeMethods.cs`),
  `DiffGutterControllers` (holds WinUI controllers). They get Avalonia twins in their own cards.
- If one of the four files turns out to use a WinUI type, leave it, and say so in the Result section.

## Tests

- [ ] New (View): `SashLayout_DefaultRatio_SplitsWidth`, `SashLayout_ResizingDisabled_IgnoresDrag` (or the closest real members), `DiffGutterLanes_NothingOn_HasNoLane`,
  `DiffGutterLanes_AllOn_OrdersLanesAsDecision1` (phase 14 README decision 1)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Any other refactor of the WinUI control.

## Suggested commit message

`refactor(view): move the UI-free diff editor helpers from WinUI to View`
