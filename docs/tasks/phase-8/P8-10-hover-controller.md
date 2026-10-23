# P8-10 — `HoverController` and the `Hover` feature

- **Status:** todo
- **Depends on:** P8-09
- **Size:** M

## Goal

When the mouse rests on text, the editor shows a hover (markers and provider answers) through an `IHoverPresenter`, hides it when the mouse leaves, the text or the selection changes, the view scrolls, or `Esc` is pressed, and `Ctrl+K Ctrl+I` shows it at the cursor.
The presenter (the WinUI popup) is P8-11; here a fake presenter proves the logic.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/hover/browser/contentHoverController.ts` | 33–120 (fields, `_hookListeners`), 118–272 (`_cancelSchedulerAndHide` … `_reactToEditorMouseMove`), 313–321 (`hideContentHover`) | `HoverController` |
| `src/vs/editor/contrib/hover/browser/contentHoverWidgetWrapper.ts` | 99–219 (`_startShowingOrUpdateHover`, `_startHoverOperationIfNecessary`, `_setCurrentResult`, `_addLoadingMessage`, `_withResult`), 249–300 (`showsOrWillShow`, `_findHoverAnchorCandidates`), 317–319 (`startShowingAtRange`) | the result handling and the anchors |
| `src/vs/editor/contrib/hover/browser/hoverActions.ts` | `ShowOrFocusHoverAction`, `HideContentHoverAction` (search the class names) | the two commands and their keybindings |

## Files to create or change

- `src/LoomEditor.View/Hover/IHoverPresenter.cs`, `HoverContent.cs`, `NullHoverPresenter.cs`, `HoverController.cs` — create (`internal`)
- `src/LoomEditor.View/Features/Hover/HoverFeature.cs`, `HoverCommands.cs`, `README.md` — create (**all three are required by `ConventionTests`**)
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: `() => new HoverFeature(),` (after `WordHighlighterFeature`, before `TabFocusFeature`)
- `src/LoomEditor.View/Features/EditorContext.cs` — change: `internal IHoverPresenter HoverPresenter { get; set; } = NullHoverPresenter.Instance;`, `internal bool HoverVisible { get; set; }`
- `src/LoomEditor.View/Features/EditorContextExtensions.cs` — change: `GetHoverPresenter`
- `src/LoomEditor.View/Commands/KeybindingContext.cs` — change: new last parameter `bool HoverVisible = false` (doc `editorHoverVisible`)
- `src/LoomEditor.View/Commands/WhenClauses.cs` — change: `HoverVisible(KeybindingContext c) => c.HoverVisible`
- `src/LoomEditor.View/Commands/DefaultKeybindings.cs` — change: two rules
- `src/LoomEditor.WinUI/Controls/LoomEditor.Keyboard.cs` — change: `HoverVisible: _context.HoverVisible` in `CreateKeybindingContext`
- `tests/LoomEditor.View.Tests/Hover/HoverControllerTests.cs`, `tests/LoomEditor.View.Tests/Features/Hover/HoverFeatureTests.cs` — create; `tests/LoomEditor.View.Tests/Hover/TestHelpers/FakeHoverPresenter.cs` — create
- `docs/how-to/add-a-feature.md` — **not** changed

## Design decisions (already made — do not change)

- **`IHoverPresenter`** (`internal interface`): `bool IsVisible { get; }`, `bool IsMouseOver { get; }` (the pointer is over the popup), `void Show(HoverContent content)`, `void Hide()`. `HoverContent(HoverAnchor Anchor, IReadOnlyList<IHoverPart> Parts, bool IsComplete)` (`sealed record`;
  `Parts` are already sorted: ascending `Order`, then, for markdown parts, ascending `Ordinal`). `NullHoverPresenter`: never visible, ignores everything (the default until the control assigns a real one).
- **`HoverController(IEditorContext context, EditorHoverOptions options) : IDisposable`** (`LoomEditor.View/Hover/`), created by the feature. It reads, through the extensions: `GetUserInputEvents()`, `GetViewModel()`, `GetLanguageFeatures()`, `GetMarkerDecorations()`, `GetUiDispatcher()`, `GetLanguageId()`, `GetHoverPresenter()` **at use time** (the control assigns the presenter later than `Attach`).
  If any of the first four is `null` the controller does nothing (a non-`EditorContext`). It owns a `ContentHoverComputer` and a `HoverOperation` (P8-09; the delay comes from `options.Delay`) and a `RunOnceScheduler _reactRunner` (delay 0, `Schedule(options.HidingDelay)` where Monaco writes `schedule(hidingDelay)`).
- **Events it subscribes to** (and unsubscribes in `Dispose`): `UserInputEvents.MouseMove` / `MouseDown` / `MouseUp` / `MouseLeave` (P8-08), `context.Model.ContentChanged` (Monaco: `_cancelScheduler`; **also hides**, see below), `context.SelectionChanged` (hides),
  `viewModel.ViewLayout.Scroll` (the existing scroll event of `ViewLayout`; hides when the scroll **top or left changed**: find the `ScrollEvent` member names with `grep -n "class ScrollEvent\|record.*ScrollEvent" -r src/LoomEditor.View`).
  Hiding on content and selection changes replaces Monaco's `onKeyDown` rule (features hear no key events): `Hide()` also cancels the scheduler and the operation (`_cancelSchedulerAndHide`).
- **State**: `EditorMouseEventArgs? _mouseMoveEvent`, `bool _isMouseDown`, `HoverContent? _current`, `HoverOperation _operation`, `RunOnceScheduler _reactRunner`.
- **`OnMouseMove(args)`** — port of `_onEditorMouseMove`: nothing if `!options.Enabled`; if `_isMouseDown && !presenter.IsMouseOver`: `CancelSchedulerAndHide()` and return; `_mouseMoveEvent = args`; if `ShouldKeepCurrentHover` → cancel `_reactRunner`, return, where
  `ShouldKeepCurrentHover = presenter.IsVisible && options.Sticky && presenter.IsMouseOver` (Monaco: `isMouseOnStickyContentHoverWidget`; focus, resize, keyboard and color picker rules are not ported); if `ShouldReschedule` (`presenter.IsVisible && options.Sticky && options.HidingDelay > TimeSpan.Zero`) →
  `if (!_reactRunner.IsScheduled) _reactRunner.Schedule(options.HidingDelay)` and return (the runner's action calls `ReactToMouseMove(_mouseMoveEvent)` when it is not null); else `ReactToMouseMove(args)`.
- **`ReactToMouseMove(args)`** — port of `_reactToEditorMouseMove` + `showsOrWillShow`: `FindAnchor(args)` (below); `if (StartShowingOrUpdateHover(anchor, HoverStartMode.Delayed, HoverStartSource.Mouse)) return; Hide();`.
  `FindAnchor` (port of `_findHoverAnchorCandidates`, without participants' suggestions): target `ContentText` → `new HoverAnchor(target.Range!, args.Event.X, args.Event.Y)` (the target range is on **model** coordinates, the events given to features are converted; a target without a `Range` gives no anchor);
  `ContentEmpty` → only when `!target.IsAfterLines && target.HorizontalDistanceToText is { } d && d < epsilon` with `epsilon = viewModel.FontInfo.TypicalHalfwidthCharacterWidth / 2`, then the same anchor from `target.Range` (`null` range → none); every other target type → no anchor.
- **`StartShowingOrUpdateHover(anchor?, mode, source)`** — port of `_startShowingOrUpdateHover` (the sticky "mouse getting closer" geometry is replaced by the `IsMouseOver` rule above, so `insistOnKeepingHoverVisible` is always false here):
  no visible hover (`!presenter.IsVisible || _current is null`): with an anchor → `StartOperationIfNecessary(anchor, mode, source)`, return `true`; without → return `false`.
  Visible: without an anchor → `SetCurrentResult(null)`, return `false`; `_current.Anchor.Equals(anchor)` → return `true`; `!anchor.CanAdoptVisibleHover(_current.Anchor, _current.Anchor.Range.GetStartPosition())` → `SetCurrentResult(null)`, `StartOperationIfNecessary(…)`, return `true`;
  else `SetCurrentResult(Filter(_current, anchor))` (keeps the parts with `IsValidForHoverAnchor(anchor)`; a `LoadingHoverPart` is dropped), then `StartOperationIfNecessary(…)`, return `true`.
  `StartOperationIfNecessary`: `if (_operation.Options?.Anchor.Equals(anchor) == true) return; _operation.Cancel(); _operation.Start(mode, new HoverComputerOptions(anchor, source));`.
- **`OnResult(HoverResult result)`** (handler of `HoverOperation.Result`) — port of `_withResult` + `_addLoadingMessage`: `parts = result.Value` plus a `LoadingHoverPart(result.Options.Anchor.Range)` when `result.HasLoadingMessage`; **then the markdown parts' ranges are validated on the UI thread**: each `MarkdownHoverPart` gets `Range = model.ValidateRange(part.Range)`, and a part whose validated range is not on the anchor's line is dropped (the providers' ranges come from the server unvalidated, P8-09); `content = Create(result.Options.Anchor, parts, result.IsComplete)` (sorts as described);
  `visibleComplete = presenter.IsVisible && _current is { IsComplete: true }`; `if (!visibleComplete) SetCurrentResult(content)`; `if (!result.IsComplete) return;` `if (result.Value.Count == 0 && result.Options.InsistOnKeepingHoverVisible) return;` `SetCurrentResult(content)`.
  `SetCurrentResult(content?)`: reference-equal → return; a content without parts becomes `null`; assign `_current`; `presenter.Show(content)` or `presenter.Hide()`; `context.HoverVisible = _current is not null` (`EditorContext` property, set only for an `EditorContext`).
- **`OnMouseDown`**: `_isMouseDown = true`; keep the hover if `presenter.IsMouseOver`, else `Hide()`. **`OnMouseUp`**: `_isMouseDown = false`. **`OnMouseLeave`**: cancel `_reactRunner` and clear `_mouseMoveEvent`; keep the hover if `presenter.IsMouseOver` or (`options.Sticky` and the presenter is visible: the mouse went toward the popup; the WinUI side calls `Hide()` when the pointer leaves the popup, P8-11); else `Hide()`.
- **Commands** (`HoverCommands`, same ids as Monaco): `ShowHover = "editor.action.showHover"` (`ShowOrFocusHoverAction`) → anchor = an empty range at the primary cursor (`context.Selection.GetPosition()`), `StartShowingAtRange`: `_operation.Cancel(); _operation.Start(HoverStartMode.Immediate, new HoverComputerOptions(new HoverAnchor(range), HoverStartSource.Keyboard))`;
  `HideHover = "editor.action.hideHover"` (`HideContentHoverAction`) → `Hide()`. Keybindings (`DefaultKeybindings.cs`, same style as the existing chord helper `FoldChord`): `Ctrl+K Ctrl+I` → `ShowHover` (no `when`, weight `KeybindingWeight.EditorContrib`), `Escape` → `HideHover` with `WhenClauses.HoverVisible` at the **same weight as the find widget's Escape** (`FindWidgetWeight`) so it wins over `cancelSelection`.
  Open `DefaultKeybindings.cs` and copy the exact helper and weight names; the chord helper takes the second key, look at `FoldChord`.
- **`HoverFeature : IEditorFeature`** (`Id = "loom.hover"`): `Attach` creates the controller (`EditorHoverOptions.Default`, property `Options { get; set; }` like `WordHighlighterFeature`) and registers the two commands; `Dispose` disposes both. README.md of the feature folder: role, files, "Known limits".
- `<remarks>Threading: UI thread only.</remarks>` on `HoverController` and the feature. Header on ported files.

## Tests

- [ ] New (`HoverControllerTests`; a real `EditorContext` created with a `ManualEditorScheduler` and `ManualUiDispatcher` (`new EditorContext(scheduler)` is the existing internal constructor; if it takes no dispatcher, use the 4-argument one with both), text `"let value = 1;\nsecond"`, a language `csharp` registered like `EditorContextTests` does,
      a `FakeHoverPresenter` assigned to `context.HoverPresenter`; helper `Move(line, column)` raises `context.UserInputEvents.EmitMouseMove` with a `ContentText` target at that position (look at how `ViewUserInputEvents` tests build a `MouseTarget`); comments `// Monaco: ContentHoverController._onEditorMouseMove`):
      `MouseRest_ShowsMarkerHoverAfterTheDelay` (marker on `value`; nothing at 299 ms, `Show` at 300 ms with one `MarkerHoverPart`), `MouseRest_ProviderHover_ShownWhenAsyncCompletes` (`FakeHoverProvider.Pending()`, complete at 200 ms → shown at 300 ms complete), `MouseRest_NoMarkerNoProvider_ShowsNothing`,
      `MouseMoveToAnotherWord_HidesAndRestarts` (after the first hover is visible, move to a word without hover: `Hide` called, no new `Show`), `MouseMoveWithinSameWord_KeepsHoverVisible` (anchor columns differ, the part range contains both: the presenter is not hidden between), `MouseOverPresenter_StaysVisible` (`IsMouseOver = true`, move away: not hidden),
      `MouseDown_HidesUnlessOverPresenter`, `MouseDrag_NoHoverComputed` (`MouseDown` then moves: no `Show`), `MouseLeave_Hides`, `MouseLeave_WhilePresenterVisibleAndSticky_KeepsIt` (sticky and visible: not hidden; hiding it later is the presenter's job, P8-11), `MouseLeave_NotSticky_Hides`,
      `ContentChanged_Hides`, `SelectionChanged_Hides`, `Scroll_Hides` (scroll top changed) and `HorizontalScroll_Hides`, `ScrollWithoutChange_DoesNotHide`, `Disabled_DoesNothing` (`Enabled = false`), `ContentEmptyNearText_ShowsHover` (`HorizontalDistanceToText` below / above epsilon), `ContentEmptyAfterLines_NoHover`, `GutterTarget_NoHover`,
      `StaleAsyncResult_AfterCancel_Dropped`, `ProviderRange_BeyondTheText_IsValidated`, `ProviderRange_OnAnotherLine_PartDropped`, `LoadingPart_ShownAtThreeTimesTheDelay`, `HoverVisible_FlagOnContext_FollowsThePresenter`, `Sticky_ReschedulesReactionOnMove` (visible + sticky + hiding delay: a move over another word reacts after 300 ms, not immediately), `Dispose_Unsubscribes` (events after `Dispose` do nothing)
- [ ] New (`HoverFeatureTests`): `Attach_RegistersTheCommands`, `ShowHover_ShowsAtTheCursor` (cursor on `value`, `Execute("editor.action.showHover")`, `RunAll()` of the dispatcher: `Show` called, immediate: no delay), `HideHover_Hides`, `Dispose_UnregistersTheCommands`, `NonEditorContext_AttachDoesNothing`,
      `DefaultKeybindings_HaveTheHoverRules` (`Ctrl+K Ctrl+I` → showHover; `Escape` with `HoverVisible` → hideHover; `Escape` without it → not hideHover: use the resolver tests' helpers, `grep -rn "KeybindingResolver" tests/LoomEditor.View.Tests | head -3`)
- [ ] The feature folder test of `ConventionTests` passes (folder has `HoverFeature.cs`, `HoverCommands.cs`, `README.md`, and `new HoverFeature()` is in `EditorFeatures.cs`)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: `contentHoverController.ts`, `contentHoverWidgetWrapper.ts` (the logic parts), `hoverActions.ts` (two actions); not ported: sticky geometry (`isMouseGettingCloser`), `onKeyboardModifier`, focus and verbosity actions, context menu pause, color picker
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The popup (P8-11), Markdown drawing, hover focus mode (`editor.action.showDefinitionPreviewHover`), keyboard focus into the hover, `hover.above`.

## Suggested commit message

`feat(view): add the hover controller and feature`
