# P16-07 — Share the pointer and scrolling decisions of `LoomEditor`

- **Status:** todo
- **Depends on:** P16-05
- **Size:** M

**Recommended model:** Opus — the only card that reshapes input handlers, and the WinUI ones have no automated test: keeping the exact order of focus, capture and shared calls in two different event models needs judgement.

## Goal

The decisions taken on a pointer press or move (target, gutter click, zone, sticky scroll line, minimap point) and the scroll properties and scroll bar update
exist once. The framework event handlers stay per host and only read the event and call the shared code.

## Sources

Not a Monaco port (phase 16 is a refactoring). Read first: [phase-16/README.md](README.md) (rules), [ADR 0017](../../adr/0017-shared-host-logic.md). `src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.cs`, `src/LoomEditor.WinUI/Controls/LoomEditor.Scrolling.cs` and their Avalonia twins.

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| File (Avalonia side unless stated) | Lines | Members | Twin |
|---|---|---|---|
| `src/LoomEditor.Avalonia/Controls/LoomEditor.Pointer.cs` | 89+32, 157+23, 286+22, 320+25 | bodies of `OnPointerPressed` and `OnPointerMoved`; `HandleStickyPress`, `SetHoveredStickyLine`, `TryGetMinimapPoint`, `MinimapLeft`, `CreateMouseTarget` | WinUI same path |
| `src/LoomEditor.Avalonia/Controls/LoomEditor.Scrolling.cs` | 18+33, 73+23, 175+25 | `ScrollTop`, `ScrollLeft`, body of `ScrollFromWheel`, `UpdateScrollBar` | WinUI same path |

## Files to create or change

- `src/LoomEditor.HostShared/Controls/LoomEditor.Pointer.cs` — new: the members listed above, and the duplicated bodies extracted as methods that take plain values, e.g.
  `PointerPressResult HandlePointerPress(EditorMouseEvent mouseEvent)` returning what the host must do (`None`, `Handled`, `HandledFocus`, `StartDrag`).
  Each host's `LoomEditor.Pointer.cs` → `git mv` to `LoomEditor.Pointer.Host.cs`: framework handlers that build the `EditorMouseEvent`, call the shared method,
  then focus (`FocusState.Pointer` / `NavigationMethod.Pointer`), capture (`CapturePointer` / `Capture`) and set `e.Handled`.
- `src/LoomEditor.HostShared/Controls/LoomEditor.Scrolling.cs` — new: `ScrollTop`, `ScrollLeft`, the wheel arithmetic (`ScrollByWheelDelta(double deltaX, double deltaY, …)`), `UpdateScrollBar`
  over host members for the scroll bar values. Host files → `LoomEditor.Scrolling.Host.cs`.

## Design decisions (already made — do not change)

- The order of operations of each handler stays exactly as in WinUI (focus before capture before `_mouseDown.Start`). Where the two hosts already differ
  (wheel delta units, `Capture` semantics), the difference stays in the host part.
- If a duplicated block cannot be extracted without changing the order of a framework call and a shared call, leave that block and say so in the Result section.

## Tests

- [ ] No new test. The Avalonia headless tests stay green **unchanged** (they now run the shared code).
- [ ] `LoomEditorPointerTests`, `LoomEditorScrollingTests` and `HostViewZoneTests` pass unchanged.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] `docs/PORTING.md` rows of the moved files updated (rule 7 of the phase README)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Keyboard and IME handlers. Any new pointer behavior.

## Suggested commit message

`refactor(hosts): share the pointer and scrolling decisions of LoomEditor`
