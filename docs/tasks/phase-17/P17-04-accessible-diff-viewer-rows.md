# P17-04 — Share the row colors and cells of the accessible diff viewer

- **Status:** todo
- **Depends on:** P17-01
- **Size:** S

**Recommended model:** Sonnet

## Goal

What a row of the accessible diff viewer shows (its theme colors, its four cells) is decided once, in the shared `AccessibleDiffViewer.cs`; the host
parts only build the WinUI / Avalonia elements.

## Sources

Not a Monaco port (the Monaco header of the shared file stays). Read first: [phase-17/README.md](README.md) (rules).
`src/LoomEditor.HostShared/Controls/DiffEditor/AccessibleDiffViewer.cs` (P16-11).

## Duplicated blocks (SonarCloud, PR #26, 2026-11-03)

| Avalonia (`src/LoomEditor.Avalonia/Controls/DiffEditor/AccessibleDiffViewer.Host.cs`) | WinUI (same path) | What |
|---|---|---|
| 49+14 | 48+14 | constructor: close button, list name, outer grid |
| 73+23 | 69+23 | `CreateRow`: row background, columns, number colors, the four `AddCell` calls |

## Files to create or change

- `src/LoomEditor.HostShared/Controls/DiffEditor/AccessibleDiffViewer.cs` — add:
  - `NumberColumnWidth` and `SignColumnWidth` (moved from both hosts);
  - `private ThemeColor? RowBackground(LineType type)` (`diffEditor.insertedLineBackground` / `diffEditor.removedLineBackground` / `null`) and
    `private ThemeColor? NumberBackground(LineType type)` (`diffEditor.insertedTextBackground` / `diffEditor.removedTextBackground` / `null`), with
    the `// Monaco: _createRow …` comment that is now above `CreateRow`;
  - `private void AddCells(DiffViewerRowItem row, Action<int, string, ThemeColor?, ThemeColor?> addCell)`: reads `editorLineNumber.foreground`,
    `NumberBackground(row.Element.Type)` and `editor.foreground` once, then calls `addCell(column, text, foreground, background)` for column 0
    (`row.OriginalNumber`, number colors), 1 (`row.ModifiedNumber`, number colors), 2 (`row.Sign`, text foreground, no background), 3 (`row.Text`,
    text foreground, no background).
- `src/LoomEditor.WinUI/Controls/DiffEditor/AccessibleDiffViewer.Host.cs` and the Avalonia twin — `CreateRow` becomes: the grid with
  `Background = Brush(RowBackground(type))`; the header case unchanged; the four columns; then
  `AddCells(row, (column, text, foreground, background) => AddCell(grid, column, text, Brush(foreground), Brush(background), CellAlignment(column)));`
  with a new host member `private static HorizontalAlignment CellAlignment(int column)` (0 and 1 → `Right`, 2 → `Center`, 3 → `Left`, as today).
  The constants go.
- Avalonia host only, the Avalonia idiom (phase 17 README, rule 3): the outer grid of the constructor gets
  `RowDefinitions = new RowDefinitions("Auto,*")` in its initializer instead of the two `RowDefinitions.Add` lines; `CreateRow` sets
  `grid.ColumnDefinitions = new ColumnDefinitions($"{NumberColumnWidth},{NumberColumnWidth},{SignColumnWidth},*");` after the header case, instead of the
  four `ColumnDefinitions.Add` lines (header rows keep no columns, as today).
- `tests/LoomEditor.Architecture.Tests/HostTwinDuplicationTests.cs` — delete the `AccessibleDiffViewer.Host.cs` pair from `KnownDuplicates`.

## Design decisions (already made — do not change)

- Same colors, same cells, same alignment, same per-row brush creation as today: only where the decision is written changes.
- `LineType` and `DiffViewerRowItem` come from `LoomEditor.View.DiffEditor` (already a using of the shared file); `ThemeColor` from `LoomEditor.Themes`.
- The keyboard handler, `Text`, `AddCell`, `Brush` and the list code stay in the host parts (they name framework types).

## Tests

- [ ] No new test. The Avalonia headless tests (`AccessibleDiffViewer*` in `tests/LoomEditor.Avalonia.Tests`) stay green **unchanged**.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green, the duplication test included
- [ ] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [ ] The pair is gone from `KnownDuplicates`
- [ ] `docs/PORTING.md` row of the accessible diff viewer still names the right files
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The keyboard handling of the viewer (its two twins differ in their key types; Sonar does not report them).

## Suggested commit message

`refactor(hosts): share the row colors and cells of the accessible diff viewer`
