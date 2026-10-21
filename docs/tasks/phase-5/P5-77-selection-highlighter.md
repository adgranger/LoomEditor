# P5-77 — `SelectionHighlighter`: highlight the other occurrences of the selection

- **Status:** done
- **Depends on:** P5-76
- **Size:** M

## Goal

When text is selected, the other occurrences of that text are highlighted (`editor.selectionHighlightBackground`), as in
VS Code (`selectionHighlight`, on by default). The last 6 deferred tests of `multicursor.test.ts` pass (74 / 76).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/multicursor/browser/multicursor.ts` | 846–876 | `SelectionHighlighterState` |
| same | 878–1098 | `SelectionHighlighter` |
| `contrib/wordHighlighter/browser/highlightDecorations.ts` | 52–70, 96–106 | `_SELECTION_HIGHLIGHT_OPTIONS`, `_SELECTION_HIGHLIGHT_OPTIONS_NO_OVERVIEW`, `getSelectionHighlightDecorationOptions`, the theming rule (`transparent(0.5)`) |
| `common/config/editorOptions.ts` | 6714–6725 | `selectionHighlight` (true), `selectionHighlightMaxLength` (200), `selectionHighlightMultiline` (false) |
| `contrib/multicursor/test/browser/multicursor.test.ts` | 957–1054 | suite `Selected text occurrence matching highlights` |

## Files to create or change

- `src/LoomEditor.View/Features/MultiCursor/SelectionHighlighter.cs` — create (`SelectionHighlighterState` as a private nested class)
- `src/LoomEditor.View/Features/MultiCursor/SelectionHighlightDecorations.cs` — create: the two options and
  `GetSelectionHighlightDecorationOptions(bool hasSemanticHighlights)` (partial port of `highlightDecorations.ts`; the word
  highlighter step (H) ports the rest of that file next to it or moves this one into `Features/WordHighlighter/`)
- `src/LoomEditor.View/Features/MultiCursor/MultiCursorOptions.cs` — change: `SelectionHighlight = true`,
  `SelectionHighlightMaxLength = 200`, `SelectionHighlightMultiline = false`
- `src/LoomEditor.View/Features/MultiCursor/MultiCursorFeature.cs`, `README.md` — change: create the highlighter in `Attach`
  (after the controller), dispose it
- `src/LoomEditor.View/Layout/DecorationStyles.cs` — **verify** the `selectionHighlight` row of P5-08: background
  `editor.selectionHighlightBackground` with its alpha halved (Monaco's `transparent(0.5)` in the theming rule)
- `src/LoomEditor.Themes/EditorColorDefaults.cs` — change if P5-08 did not: `editor.selectionHighlightBackground`
  (`editorColors.ts:133`) — see decisions
- `tests/LoomEditor.View.Tests/Features/MultiCursor/SelectionHighlighterTests.cs` — create

## Design decisions (already made — do not change)

- `new SelectionHighlighter(ViewModel viewModel, MultiCursorSelectionController multiCursor, FindController findController, EditorDecorationsCollection decorations, IEditorScheduler scheduler, MultiCursorOptions options)`.
  The feature passes `context.CreateDecorationsCollection()` (P5-A) and `context.Scheduler`; tests create the collection and
  a `ManualEditorScheduler` the way P5-A's tests do. `updateSoon` is a `RunOnceScheduler` of 300 ms; `schedule(0)` on a
  `selectedTextMatchMode` change.
- Events: `onDidChangeCursorSelection` → `CursorStateChangedEvent` (`e.selection` is the primary selection, `e.reason` the
  event's `Reason`); `onDidChangeModelContent` → `Model.ContentChanged`; `onDidChangeConfiguration` → `OptionsChanged` of
  the controller (P5-75); the find state's `Changed` → `_update()`. `onDidChangeModel` is not ported.
- `hasSemanticHighlights` is always `false`: there is no document highlight provider before phase 8 (language features).
  So `_SELECTION_HIGHLIGHT_OPTIONS` is always used; its `overviewRuler` / `minimap` members are dropped (phase 6).
- `/^[ \t]+$/.test(searchText)` → a loop over the characters (no regex). `model.isTooLargeForTokenization()` → the existing
  `TextModel.IsTooLargeForTokenization`.
- `editor.selectionHighlightBackground` default is `lessProminent(selection, background, 0.3, 0.6)`, expressed with `ColorValue.LessProminent` (P5-05); P5-05 already adds it: only verify.

## Tests

Suite `Selected text occurrence matching highlights` (`highlights(editor)` reads the decorations whose class name is
`selectionHighlight`; `sinon.useFakeTimers()` + `clock.runAll()` → `ManualEditorScheduler.RunAll()`):

- [x] Port (6): `selection highlighting uses match case false, editor focused true`,
      `only suppress duplicate Find highlights with the same matching rules, match case false`,
      `selection highlighting uses match case true, editor focused true`,
      `only suppress duplicate Find highlights with the same matching rules, match case true`,
      `selection highlighting refreshes on case-only configuration changes without cursor movement`,
      `Find regex select-all discards an existing selection-driven session`
- [x] New: `EmptySelection_NoHighlights`, `WhitespaceOnlySelection_NoHighlights`, `SelectionLongerThan200_NoHighlights`,
      `MultilineSelection_OptionOff_NoHighlights`, `HighlightsDoNotOverlapSelections`, `Dispose_ClearsDecorations`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `docs/PORTING.md`: map rows `multicursor.ts` ✅ except word wrap / `setModel` (P5-75 … P5-77),
      `highlightDecorations.ts` 🔶 (selection highlight only); tests table `multicursor.test.ts` 74 / 76 (`issue #26393` word
      wrap: phase 6; `a selection session does not survive replacing the editor model`: no `setModel`); color difference
- [ ] **Manual check by the maintainer** (Gallery): selecting a word highlights its other occurrences; typing removes them;
      Ctrl+D then adds them one by one
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Word highlight (`wordHighlighter.ts`, step H), overview ruler marks (phase 6).

## Suggested commit message

`feat(view): highlight the other occurrences of the selection`
