# P6-34 — The guides of each visible line (`IndentGuidesOverlay.getGuidesByLine`)

- **Status:** todo
- **Depends on:** P6-32, P6-33
- **Size:** S

## Goal

The view computes, for each visible view line, the ordered list of guides to draw: indent guides (with the active one)
merged with bracket guides, honoring the guide options, exactly like Monaco's overlay, ready for the renderer.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/browser/viewParts/indentGuides/indentGuides.ts`.

| Lines | What to port |
|---|---|
| 25–50 | the fields read from the options (`_spaceWidth`, `_maxIndentLeft` from the wrapping column, `_bracketPairGuideOptions`) |
| 109–113 | the early return of `prepareRender` when both guide kinds are off |
| 160–240 | `getGuidesByLine` (the `ArrayQueue` merge of indent and bracket guides, the active indent rule with `'always'`) |

## Files to create or change

- `src/LoomEditor.View/Layout/IndentGuidesComputer.cs` — create (header `indentGuides.ts (IndentGuidesOverlay.getGuidesByLine)`):
  `internal static class IndentGuidesComputer` with
  `IReadOnlyList<IReadOnlyList<IndentGuide>>? GetGuidesByLine(ViewModel viewModel, EditorGuidesOptions options, int visibleStartLineNumber, int visibleEndLineNumber, Position? activeCursorPosition)`
  (returns `null` when Monaco's `prepareRender` would set `_renderResult = null`)
- `tests/LoomEditor.View.Tests/Layout/IndentGuidesComputerTests.cs` — create

## Design decisions (already made — do not change)

- Monaco calls `getGuidesByLine` with `Math.min(visibleEndLineNumber + 1, lineCount)`: the caller (renderer, P6-35) does that;
  the computer takes the range it is given.
- `ArrayQueue.takeWhile` / `peek` → a small private struct cursor over the list (index + `TakeWhile(Func<IndentGuide,bool>)`
  returning the taken items); no LINQ `TakeWhile` (it does not advance a shared position).
- `BracketGuideOptions` from `EditorGuidesOptions` like lines 166–176: `HighlightActive = HighlightActiveBracketPair`,
  `HorizontalGuides` = `On → Enabled`, `Active → EnabledForActive`, `Off → Disabled`, `IncludeInactive = BracketPairs == On`;
  bracket guides are queried only when `BracketPairs != Off`.
- Indent guide class name: `$"core-guide-indent lvl-{(indentLvl - 1) % 30}"` + `" indent-active"` when active — the renderer
  (P6-35) maps class names to colors; keep the strings identical to Monaco so the mapping is the same.
- `indentSize` = `viewModel.Model.Options.IndentSize`.

## Tests

- [ ] New: `GetGuidesByLine_BothDisabled_ReturnsNull`
- [ ] New: `GetGuidesByLine_IndentOnly_OneGuidePerLevel` (columns 1, 1 + indentSize, …)
- [ ] New: `GetGuidesByLine_ActiveIndent_IsMarkedOnlyInTheActiveBlock`
- [ ] New: `GetGuidesByLine_BracketGuideAtTheSameColumn_ReplacesTheIndentGuide`
- [ ] New: `GetGuidesByLine_BracketGuidesPresent_ActiveIndentNotHighlighted_UnlessAlways`
- [ ] New: `GetGuidesByLine_BracketGuidesAfterTheIndents_AreAppended`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: `indentGuides.ts` row updated (`getGuidesByLine`)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Pixel positions and drawing (P6-35).

## Suggested commit message

`feat(view): compute the guides of the visible lines`
