# P6-32 — View lines and view model: bracket guides, guide options

- **Status:** done
- **Depends on:** P6-31
- **Size:** S

## Goal

`ViewModelLinesFromProjectedModel` maps the bracket guides of model lines to view lines (wrapped lines get the guides that
apply to them), and the view model exposes the three guide queries plus the guide options.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/common/viewModel/viewModelLines.ts` | 586–668 | `getViewLinesBracketGuides` (the `forWrappedLinesAfterColumn` / `forWrappedLinesBeforeOrAtColumn` filtering and the column translation) |
| `src/vs/editor/common/viewModel/viewModelImpl.ts` | 803–813 | `getActiveIndentGuide`, `getLinesIndentGuides`, `getBracketGuidesInRangeByLine` |
| `src/vs/editor/common/config/editorOptions.ts` | 4861–4891 | `IGuidesOptions` (doc comments) |
| same | 4901–4977 | `GuideOptions` (defaults and `validate`) |

## Files to create or change

- `src/LoomEditor.View/Layout/ViewModelLinesFromProjectedModel.Guides.cs` — change: add `GetViewLinesBracketGuides`; update the
  "Phase 6 cut points" remark of the class (guides are done)
- `src/LoomEditor.View/Layout/ViewModel.Guides.cs` — create (partial, header `viewModelImpl.ts`): the three queries
- `src/LoomEditor.View/Layout/EditorGuidesOptions.cs` — create (header `editorOptions.ts`)
- `src/LoomEditor.View/Layout/EditorLayoutOptions.cs` — change: `public EditorGuidesOptions Guides { get; init; } = new();`
  (the renderer reads it from `ViewModel.LayoutOptions`; it does not change the layout)
- `tests/LoomEditor.View.Tests/Layout/ViewModelLinesGuidesTests.cs` — change; `EditorGuidesOptionsTests.cs` — create

## Design decisions (already made — do not change)

- `EditorGuidesOptions` is an `internal sealed record` with Monaco's defaults: `BracketPairs = GuideMode.Off`
  (`enum GuideMode { Off, On, Active }` for Monaco's `true | false | 'active'`), `BracketPairsHorizontal = GuideMode.Active`,
  `HighlightActiveBracketPair = true`, `Indentation = true`, `HighlightActiveIndentation = ActiveIndentationHighlight.On`
  (`enum ActiveIndentationHighlight { Off, On, Always }` for `false | true | 'always'`).
- `GetViewLinesBracketGuides(int viewStartLineNumber, int viewEndLineNumber, Position? activeViewPosition, BracketGuideOptions options)`
  → `List<IndentGuide>[]`; the active position is converted to model coordinates first, exactly like Monaco.
- The view model methods keep Monaco's names (`GetActiveIndentGuide`, `GetLinesIndentGuides`, `GetBracketGuidesInRangeByLine`).

## Tests

- [x] New: `GetViewLinesBracketGuides_WrappedLine_KeepsTheGuidesOfTheRightSegment` (a pair opening mid-line on a wrapped line:
      the guide appears only on the continuation lines after the opening column)
- [x] New: `GetViewLinesBracketGuides_ColumnGuide_IsTranslatedToTheViewColumn`
- [x] New: `ViewModel_GuideQueries_DelegateToTheLines`
- [x] New: `EditorGuidesOptions_Defaults_MatchMonaco`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: `viewModelLines.ts` and `viewModelImpl.ts` rows (guides); remove the guides row of "Cut points for phase 6"
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Drawing the guides and their colors (P6-33, P6-34).

## Suggested commit message

`feat(view): map bracket guides to view lines`
