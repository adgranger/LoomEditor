# P5-70 — `FindController` and the find actions (without UI)

- **Status:** todo
- **Depends on:** P5-69
- **Size:** M

## Goal

One `FindController` per editor owns the `FindReplaceState` and creates / disposes the find model when the find widget
is revealed / hidden, like Monaco's `CommonFindController` plus the non-DOM part of `FindController`. The actions
(Ctrl+F, Ctrl+H, F3, Shift+F3, Ctrl+F3, find with selection…) are plain static methods that the commands (P5-71) and
the tests call. The widget is reached through a small interface, faked in tests like Monaco's `TestFindController`.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `contrib/find/browser/findController.ts` | 68–93 | `FindStartFocusAction`, `IFindStartOptions` (`IFindStartArguments`: not ported) |
| same | 95–460 | `CommonFindController` — **not**: storage (`loadQueryState`, `saveQueryState`), clipboard (`getGlobalBufferTerm` returns `""`, `setGlobalBufferTerm` does nothing), `onDidChangeModel`, `_updateHistoryDelayer`, the notification of `replaceAll` |
| same | 488–523, 525–534, 544–556 | `FindController._start` (`autoFindInSelection`, focus), `highlightFindOptions`, `wasReplaceInputLastFocused`, `focusLastElement` |
| same | 566–598 | `StartFindAction` implementation |
| same | 667–701 | `StartFindWithSelectionAction.run` |
| same | 703–775 | `MatchFindAction.run`, `matchFindAction` (without `alertFn`) |
| same | 777–816 | `NextMatchFindAction`, `PreviousMatchFindAction` (their implementation is `matchFindAction`) |
| same | 935–1002 | `SelectionMatchFindAction`, `NextSelectionMatchFindAction`, `PreviousSelectionMatchFindAction` |
| same | 1004–1059 | `StartFindReplaceAction` implementation |
| `contrib/find/test/browser/findController.test.ts` | 27–68 | `TestFindController`, `fromSelection`, `executeAction` |

## Files to create or change

- `src/LoomEditor.View/Features/Find/FindController.cs` — create (`internal sealed class FindController : IDisposable`)
- `src/LoomEditor.View/Features/Find/FindStartOptions.cs` — create (`FindStartFocusAction` enum, `FindStartOptions` record)
- `src/LoomEditor.View/Features/Find/IFindWidget.cs` — create
- `src/LoomEditor.View/Features/Find/FindActions.cs` — create (static: `StartFind`, `StartFindWithSelection`, `NextMatch`,
  `PreviousMatch`, `NextSelectionMatch`, `PreviousSelectionMatch`, `StartFindReplace`)
- `src/LoomEditor.View/Features/Find/README.md` — update
- `tests/LoomEditor.View.Tests/Features/Find/FindControllerTests.cs` — create
- `tests/LoomEditor.View.Tests/Features/Find/TestHelpers/TestFindWidget.cs` — create (below)

## Design decisions (already made — do not change)

- `new FindController(ViewModel viewModel, IEditorScheduler scheduler, EditorFindOptions? options = null)`. It holds
  `State` (a `FindReplaceState`) and the `FindModelBoundToEditorModel?`; it subscribes to `State.Changed` (`isRevealed`
  false → dispose the model). Monaco's `findWidgetVisible` context key is `State.IsRevealed`.
- Monaco's `async start` is **synchronous**: the only `await` reads the macOS find clipboard (not ported).
- `IFindWidget` (internal) — what the controller needs from a widget; `FindController.Widget { get; set; }` is `null`
  without UI:
  - `bool IsFindInputFocused { get; }` (Monaco `CONTEXT_FIND_INPUT_FOCUSED`), `bool WasReplaceInputLastFocused { get; }`;
  - `void ApplyStartFocus(FindStartFocusAction focus)` — called at the end of **every** `Start` (also with `NoFocusChange`),
    replaces `focusFindInput` / `focusReplaceInput`;
  - `void FocusLastElement()`, `void FocusEditor()` (Monaco `editor.focus()` in `closeFindWidget` and `selectAllMatches`);
  - `void HighlightFindOptions(bool findWidgetRevealed)` — the controller calls it with `State.IsRevealed && !ignoreWhenVisible`;
    the `FindOptionsWidget` (the toggles shown when the widget is hidden) is not ported, so `false` does nothing visible.
- `toggleSearchScope` and `_start`'s `updateSearchScope` store `selection.Range`s (P5-65). `selection.setEndPosition(l, c)`
  → `new Range(range.Start, new Position(l, c))`.
- `getOption(EditorOption.find)` → the `EditorFindOptions` given to the constructor; `getOption(EditorOption.readOnly)` →
  `viewModel.CursorConfig.ReadOnly`; `controller.editor.pushUndoStop()` → `viewModel.Model.PushStackElement()`.
- **`TestFindWidget`** (port of `TestFindController`'s overrides): `HasFocus` (set to `true` when `ApplyStartFocus` gets
  something else than `NoFocusChange`), `IsFindInputFocused = focus == FindStartFocusAction.FocusFindInput` after each
  `ApplyStartFocus`, `HighlightFindOptionsCount` (counts calls; P5-75 uses it like Monaco's `sinon.spy`), `FocusEditorCount`.
- `executeAction(…, StartFindAction)` → `FindActions.StartFind(controller)`; `editor.runAction(NextMatchFindAction)` →
  `FindActions.NextMatch(controller)`. The test options (`find: { closeOnResult: true }`, `autoFindInSelection`) are given
  through `EditorFindOptions`.

## Tests

`findController.test.ts`:

- [ ] Port suite `FindController` (14): `issue #1857: F3, Find Next, acts like "Find Under Cursor"`,
      `issue #3090: F3 does not loop with two matches on a single line`,
      `issue #6149: Auto-escape highlighted text for search and replace regex mode`,
      `issue #41027: Don't replace find input value on replace action if find input is active`,
      `editor.find.closeOnResult: closes find widget when a match is found from explicit navigation`,
      `editor.find.closeOnResult: keeps find widget open when no match is found`,
      `editor.find.closeOnResult: disabled keeps find widget open after navigation`,
      `issue #9043: Clear search scope when find widget is hidden`, `issue #18111: Regex replace with single space replaces with no space`,
      `issue #24714: Regular expression with ^ in search & replace`, `issue #38232: Find Next Selection, regex enabled`,
      `issue #38232: Find Next Selection, regex enabled, find widget open`,
      `issue #47400, CMD+E supports feeding multiple line of text into the find widget`,
      `issue #109756, CMD+E with empty cursor should always work`
- [ ] Port suite `FindController query options persistence` (4): `issue #27083: Update search scope once find widget becomes visible`,
      `issue #58604: Do not update searchScope if it is empty`, `issue #58604: Update searchScope if it is not empty`,
      `issue #27083: Find in selection when multiple lines are selected`. `searchScope` is compared as ranges.
- [ ] **Not ported** (write the reasons in the PORTING tests table): `reads from the global clipboard buffer on next find action if buffer exists`,
      `writes to the global clipboard buffer when text changes` (macOS find clipboard; on Windows Monaco's tests return at
      once); `matchCase`, `wholeWord`, `toggling options is saved` (no storage service: the options live as long as the editor).
- [ ] New: `CloseFindWidget_DisposesModelAndClearsDecorations`, `SelectAllMatches_FocusesEditor`,
      `StartFindReplace_ReadOnly_DoesNothing`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `findController.ts` 🔶 (P5-70: no storage, no find clipboard, no history, no
      `MoveToMatchFindAction`, no `StartFindWithArgsAction`); tests table `findController.test.ts` 18 / 23 with reasons
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Command registration and keybindings (P5-71), the XAML widget (P5-72 … P5-74).

## Suggested commit message

`feat(view): add the find controller and the find actions`
