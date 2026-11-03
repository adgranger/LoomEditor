# P15-16 — The host API: decorations, scroll and layout sync, gutter clicks

- **Status:** todo
- **Depends on:** P15-15
- **Size:** M

**Recommended model:** Sonnet

## Goal

The phase 13 host API exists on the Avalonia `LoomEditor`: `CreateDecorationsCollection`, `ScrollChanged`, `LayoutChanged`, `LineHeight`, `GetTopForLineNumber`,
`LineNumbersEnabled`, `ResetText`, `SetHiddenAreas`, `GutterClicked`.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/LoomEditor.HostHooks.cs`, `EditorScrollChangedEventArgs.cs`, `GutterClickedEventArgs.cs` (and the `GutterArea` enum),
[ADR 0015](../../adr/0015-public-host-api.md), `docs/how-to/use-the-host-api.md`.

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/LoomEditor.HostHooks.cs` — create (twin)
- `src/LoomEditor.Avalonia/Controls/EditorScrollChangedEventArgs.cs`, `GutterClickedEventArgs.cs` (+ `GutterArea`) — copy ([README](README.md) decision 3)
- `tests/LoomEditor.Avalonia.Tests/Controls/HostHooksTests.cs` — create; complete the `// completed in P15-16` test of P15-12
- `PublicAPI.Unshipped.txt`, `AvaloniaParityPending.txt`

## Design decisions (already made — do not change)

- Same event timing as WinUI: `ScrollChanged` raised synchronously after the scroll state changes, on the UI thread; `LayoutChanged` after a re-layout (font, zoom, wrap).

## Tests

- [ ] New: `CreateDecorationsCollection_DrawsInlineClass` (a custom style with a background → pixels of that color in the range); `ScrollChanged_RaisedOnWheel`;
  `LayoutChanged_RaisedOnZoom`; `GetTopForLineNumber_MatchesRenderer`; `SetHiddenAreas_HidesLines`; `ResetText_KeepsScrollAtTop`; `GutterClicked_LineNumberArea`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] No key of `LoomEditor` (the single editor) is left in `AvaloniaParityPending.txt`, except members of widgets and comments (P15-17 … P15-23)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Widgets (step D).

## Suggested commit message

`feat(avalonia): the host API of LoomEditor`
