# P13-05 — Scroll and layout sync: `ScrollChanged`, `LayoutChanged`, `LineHeight`, `GetTopForLineNumber`

- **Status:** todo
- **Depends on:** P13-04
- **Size:** S

**Recommended model:** Sonnet

## Goal

A host draws a column beside the editor (VIGO's blame: one commit per line) that stays aligned with the text: it listens to `ScrollChanged` and `LayoutChanged`, and places
each row at `GetTopForLineNumber(line, includeViewZones: true) - ScrollTop`, `LineHeight` high.

## Sources

Sources: none (specified in this card; [phase-13/README.md](README.md) table of section 2 and decision 13). Read first: `src/LoomEditor.WinUI/Controls/LoomEditor.HostHooks.cs`
(`HostScrollChanged`, `HostLayoutChanged`, `LineHeight`, `GetTopForLineNumber`, `RaiseHostScroll`, `RaiseHostContentSizeChanged`, the record `HostScrollEventArgs`),
`src/LoomEditor.WinUI/Controls/LoomEditor.cs` (`OnViewLayoutInfoChanged`), `src/LoomEditor.WinUI/Controls/LoomEditor.Font.cs` (where `_commentZoneHost?.OnFontChanged()` is
called), `src/LoomEditor.View/Layout/ViewModelLinesFromProjectedModel.cs` (`ConvertModelPositionToViewPosition` for a hidden line), and every user of the two events
(`grep -rn "HostScrollChanged\|HostScrollEventArgs\|HostLayoutChanged" src samples tests`).

## Files to create or change

- `src/LoomEditor.WinUI/Controls/LoomEditor.HostHooks.cs` — change
- `src/LoomEditor.WinUI/Controls/EditorScrollChangedEventArgs.cs` — create
- `src/LoomEditor.WinUI/Controls/LoomEditor.cs`, `LoomEditor.Font.cs` — change
- `src/LoomEditor.WinUI/Controls/DiffEditor/DiffEditorViewZones.cs`, `LoomDiffEditor.Overview.cs` and any other user found by the grep — change (rename only)
- `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — change
- `docs/testing/phase-13-manual-tests.md` — change (section "P13-05")

## Design decisions (already made — do not change)

- **`ScrollChanged`**: the internal `HostScrollChanged` becomes `public event EventHandler<EditorScrollChangedEventArgs>? ScrollChanged;` ("Raised when the scroll position or
  the scroll height changes: scrolling, edits, folding, view zones, wrapping"). Same raise points (`RaiseHostScroll`, `RaiseHostContentSizeChanged`); rename those two private
  methods `RaiseScrollChanged` / `RaiseContentSizeChanged`.
- **`EditorScrollChangedEventArgs`** replaces the internal record `HostScrollEventArgs` (delete it): `public sealed class EditorScrollChangedEventArgs : EventArgs` with an
  **internal** constructor `(bool scrollTopChanged, bool scrollLeftChanged, bool scrollHeightChanged, double scrollTop, double scrollLeft)` and get-only properties
  `ScrollTopChanged`, `ScrollLeftChanged`, `ScrollHeightChanged`, `ScrollTop`, `ScrollLeft` (XML doc on each; DIPs). Threading: immutable. Internal users that deconstructed
  the record or used `with` switch to the properties.
- **`LayoutChanged`**: the internal `HostLayoutChanged` becomes `public event EventHandler? LayoutChanged;` ("Raised when the layout of the editor changes: size, gutter
  widths, minimap, line numbers, font and line height"). Raised where it is today (`OnViewLayoutInfoChanged`) **and** once at the end of the path in `LoomEditor.Font.cs` that
  calls `_commentZoneHost?.OnFontChanged()` (a font size or zoom change may keep the layout info and still change `LineHeight`). Two events for one change are acceptable.
- **`LineHeight`**: `internal` → `public` (`double`, read-only; "changes with the font size and the zoom: read it again on `LayoutChanged`").
- **`GetTopForLineNumber(int lineNumber, bool includeViewZones = false)`**: `internal` → `public`. It clamps `lineNumber` to `1 … Model.LineCount` first (README decision 6).
  XML doc: the offset is in scroll coordinates (subtract `ScrollTop` for a y in the editor); `includeViewZones: true` adds the zones above the line, so the result is the top of
  the line's text; a wrapped line is the top of its first row; for a line inside a hidden area write what the code returns, **after reading
  `ConvertModelPositionToViewPosition`** (do not guess). Internal callers are unchanged (they pass valid lines).
- Update the header comment of `LoomEditor.HostHooks.cs`: "What a host of this control needs: the public members for applications (phase 13) and the internal ones for the
  diff editor."
- `PublicAPI.Unshipped.txt` (WinUI) gains exactly: the two events, `LineHeight.get`, the method, the args class and its five getters.
- **Manual section "P13-05 — Scroll and layout sync"**, checks with measurable expectations: (1) the **blame column** (Gallery, P13-09) shows one row per line, each row's top
  within 1 px of its line while scrolling with the wheel, the scroll bar and `Ctrl+End`; (2) after `Ctrl+=` (zoom) the rows have the new line height; (3) with word wrap on, a
  wrapped line's row is as tall as its rows; (4) folding a region removes its rows and the next rows move up; (5) a view zone (P13-08) pushes the rows below it down by its height;
  (6) resizing the window keeps the rows aligned.

## Tests

- [ ] None automated (no WinUI test project; the layout math is the existing, tested `ViewLayout`).
- [ ] Manual: section "P13-05" written (not run by the agent).

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `grep -rn "HostScrollChanged\|HostScrollEventArgs\|HostLayoutChanged" src samples tests` finds nothing
- [ ] `PublicAPI.Unshipped.txt` (WinUI) gains exactly the lines above
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

A "visible lines" API, `GetScrollHeight` / `GetVerticalOffsetForViewLine` as public members, events on `LoomDiffEditor`.

## Suggested commit message

`feat(winui): expose scroll, layout and line metrics to the host`
