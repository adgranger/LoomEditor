# P15-24 — `LoomDiffEditor`: shell, layout, sash, public diff types

- **Status:** todo
- **Depends on:** P15-23
- **Size:** M

**Recommended model:** Sonnet

## Goal

`<loom:LoomDiffEditor Original="…" Modified="…" />` shows the two side editors with the diff decorations and a draggable sash on Avalonia. The UI-free public diff types exist.

## Sources

The WinUI twins: `src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.cs`, `LoomDiffEditor.Layout.cs`, `DiffEditorSash.cs`, the `LoomDiffEditor` style of `Themes/Generic.xaml`,
and the public types `DiffSide.cs`, `DiffLine.cs`, `DiffBlockAction.cs`, `DiffBlockActionsVisibility.cs`, `DiffBlockActionInvokedEventArgs.cs`,
`DiffLineActionInvokedEventArgs.cs`, `DiffAddCommentRequestedEventArgs.cs`. Also read [ADR 0012](../../adr/0012-diff-editor.md), `src/LoomEditor.View/DiffEditor/SashLayout.cs`
(moved in P15-02).

## Files to create or change

- `src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.cs`, `LoomDiffEditor.Layout.cs`, `DiffEditorSash.cs` — create (twins)
- `src/LoomEditor.Avalonia/Controls/DiffEditor/` — the 7 public types, copied ([README](README.md) decision 3)
- `src/LoomEditor.Avalonia/Themes/Generic.axaml` — the `LoomDiffEditor` control theme: `Panel` with `PART_Original`, `PART_Modified` (`LoomEditor`), `PART_Sash`,
  `PART_Overview` (added in P15-26; leave the slot with a `// P15-26` comment in the template), `PART_ViewerHost`
- `tests/LoomEditor.Avalonia.Tests/Controls/DiffEditor/LoomDiffEditorTests.cs` — create
- `PublicAPI.Unshipped.txt`, `AvaloniaParityPending.txt`
- `docs/testing/phase-15-manual-tests.md` — section "P15-24"

## Design decisions (already made — do not change)

- The sash is a `Control` with `Cursor = SizeWestEast`, pointer capture on press, same snapping and ratio rules (`SashLayout`).
- The diff is computed on the same background path as WinUI (`IEditorScheduler`); the result is applied on `Dispatcher.UIThread`.

## Tests

- [ ] New: `DiffEditor_TwoTexts_ShowsInsertedLineDecoration` (pixels of the inserted-line color in the modified side); `Sash_Drag_ChangesSplit`;
  `NarrowWidth_FallsBackToInline` (`RenderSideBySideEffective` false below the WinUI threshold); `DiffChanged_RaisedAfterCompute`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] Card status, `AvaloniaParityPending.txt` and `docs/tasks/README.md` updated

## Out of scope

Options (P15-25), zones and overview (P15-26), hidden regions and comments (P15-27), viewer (P15-28), actions (P15-29, P15-30).

## Suggested commit message

`feat(avalonia): the LoomDiffEditor shell, layout and sash`
