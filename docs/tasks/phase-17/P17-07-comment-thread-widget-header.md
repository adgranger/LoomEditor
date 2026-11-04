# P17-07 — Share the header layout of `CommentThreadWidget`

- **Status:** done
- **Depends on:** P17-06
- **Size:** S

**Recommended model:** Sonnet

## Goal

What the header of a comment thread shows, and when (glyph, title, "Resolved" badge, collapsed summary, toggle), is decided once. The host parts only
create the elements.

## Sources

Not a Monaco port. Read first: [phase-17/README.md](README.md) (rules). `src/LoomEditor.HostShared/Controls/Comments/CommentThreadWidget.Model.cs` (`Rebuild` calls
`_rows.Children.Add(Header())`; `CollapsedSummary`).

## Duplicated block (SonarCloud, PR #26, 2026-11-03)

| Avalonia (`src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.Header.cs`) | WinUI (same path) | What |
|---|---|---|
| 25+27 | 24+27 | `Header()` from the title to the toggle button |

## Files to create or change

- `git mv src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Header.cs src/LoomEditor.HostShared/Controls/Comments/CommentThreadWidget.Header.cs` — the shared
  part (header block, the file comment) holds:

  ```csharp
  private void AddHeader()
  {
      bool resolved = _viewModel.Thread.State == CommentThreadState.Resolved;
      var row = NewHeaderRow();
      if (resolved)
      {
          row.Children.Add(Text(_viewModel.IsCollapsed ? "▸" : "▾", _foreground));
      }

      row.Children.Add(BoldText(_viewModel.Title, _foreground));
      if (!resolved)
      {
          _rows.Children.Add(row);
          return;
      }

      row.Children.Add(ResolvedBadge(Math.Max(9, _fontSize - 2)));
      if (_viewModel.IsCollapsed)
      {
          row.Children.Add(SummaryText(CollapsedSummary()));
      }

      // Only a resolved thread's header is interactive: it expands and collapses the thread.
      _rows.Children.Add(HeaderToggle(row));
  }
  ```

- `src/LoomEditor.HostShared/Controls/Comments/CommentThreadWidget.Model.cs` — `_rows.Children.Add(Header());` becomes `AddHeader();`.
- `src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Header.Host.cs` — new, and
  `git mv src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.Header.cs …/CommentThreadWidget.Header.Host.cs` — the host members, built from
  today's `Header()` of each host, same properties:
  - `NewHeaderRow()` → the horizontal `StackPanel` (`Spacing = 6`);
  - `BoldText(string text, Brush foreground)` → `Text(text, foreground)` with the bold weight (`FontWeights.Bold` / `FontWeight.Bold`);
  - `ResolvedBadge(double fontSize)` → the `Border` with the `CommentStrings.Resolved` text block (`_dim`, the given font size);
  - `HeaderToggle(StackPanel row)` → the transparent `Button` with `row` as content and `Click += (_, _) => _viewModel.ToggleCollapsed()`;
  - `SummaryText(string text)` → `Text(text, _dim)` with character-ellipsis trimming and no wrap;
  - `Text(string text, Brush foreground)` — unchanged.
  Return types are the concrete element types (`StackPanel`, `TextBlock`, `Border`, `Button`); Avalonia uses `IBrush`.
- Avalonia host only (phase 17 README, rule 3): the badge's `Padding = new Thickness(6, 0)` (Avalonia's horizontal/vertical form of `6, 0, 6, 0`).
- `tests/LoomEditor.Architecture.Tests/HostTwinDuplicationTests.cs` — delete the `CommentThreadWidget.Header.cs` pair from `KnownDuplicates`.

## Design decisions (already made — do not change)

- Same elements, same properties, same order of children as today: the header is added to `_rows` by `AddHeader` instead of by its caller.
- Member order of each `.Header.Host.cs`: `NewHeaderRow`, `BoldText`, `ResolvedBadge`, `HeaderToggle`, `SummaryText`, `Text` (members that are identical in both
  hosts are separated by members that differ).

## Tests

- [x] No new test. The Avalonia headless tests of resolved / collapsed threads stay green **unchanged**.

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green, the duplication test included
- [x] `dotnet build LoomEditor.Portable.slnf -c Release` — 0 warnings
- [x] The pair is gone from `KnownDuplicates`, and no new pair was added
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

The comment rows (`CommentThreadWidget.Comments.cs`, not reported).

## Suggested commit message

`refactor(hosts): share the comment thread header layout`
