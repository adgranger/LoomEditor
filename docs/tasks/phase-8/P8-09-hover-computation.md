# P8-09 — Hover computation: anchors, parts, `ContentHoverComputer` and `HoverOperation`

- **Status:** todo
- **Depends on:** P8-08
- **Size:** M

## Goal

Given a hover anchor (a range under the mouse), the editor can compute what a hover shows: the markers on that line (synchronously) and the answers of the registered hover providers (asynchronously), and `HoverOperation` runs them with Monaco's timings (provider
request after half the hover delay, results shown after the whole delay, a "Loading…" part after three times the delay). No controller and no widget yet (P8-10, P8-11).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/hover/browser/hoverTypes.ts` | 12–72 (`IHoverPart`, `HoverAnchorType`, `HoverRangeAnchor`) | `IHoverPart`, `HoverAnchor` (the range anchor only) |
| `src/vs/editor/contrib/hover/browser/contentHoverComputer.ts` | 13–102 | `ContentHoverComputer` (`_getLineDecorations`, `computeAsync`, `computeSync`) |
| `src/vs/editor/contrib/hover/browser/hoverOperation.ts` | 14–230 | `HoverOperation`, `HoverResult`, `HoverStartMode`, `HoverStartSource`, `Debouncer` |
| `src/vs/editor/contrib/hover/browser/markerHoverParticipant.ts` | 35–51 (`MarkerHover`), 66–98 (`computeSync`) | `MarkerHoverPart`, the marker lookup |
| `src/vs/editor/contrib/hover/browser/markdownHoverParticipant.ts` | 44–57 (`MarkdownHover`), 179–206 (`computeAsync`, `_getMarkdownHovers`) | `MarkdownHoverPart`, the provider call |
| `src/vs/editor/contrib/hover/browser/getHover.ts` | 15–65 | `executeProvider`, `isValid`, ordering by provider index |

## Files to create or change

All in `src/LoomEditor.View/Hover/` (namespace `LoomEditor.View.Hover`, `internal`):

- `HoverAnchor.cs`, `HoverStartMode.cs`, `HoverStartSource.cs`, `HoverComputerOptions.cs`, `HoverResult.cs`, `IHoverPart.cs`, `MarkerHoverPart.cs`, `MarkdownHoverPart.cs`, `IHoverComputer.cs`, `ContentHoverComputer.cs`, `HoverOperation.cs`, `EditorHoverOptions.cs`
- `src/LoomEditor.View/Hover/README.md` — create
- `tests/LoomEditor.View.Tests/TestHelpers/TestProviders.cs` — create (its own copy of the Core helper of P8-02: `FakeCompletionProvider`, `FakeHoverProvider`, `FakeDefinitionProvider`, with `Pending()`; namespace `LoomEditor.View.Tests.TestHelpers`)
- `tests/LoomEditor.View.Tests/Hover/ContentHoverComputerTests.cs`, `HoverOperationTests.cs`, `HoverAnchorTests.cs` — create

## Design decisions (already made — do not change)

- **Types.** `HoverAnchor(Range Range, double? InitialMouseX = null, double? InitialMouseY = null)` is a `sealed record` whose **equality is the range only** (override `Equals` / `GetHashCode`; Monaco `HoverRangeAnchor.equals`); `public bool CanAdoptVisibleHover(HoverAnchor last, Position showAtPosition) => showAtPosition.LineNumber == Range.StartLineNumber`
  (the `ForeignElement` anchor and `priority` are not ported: the only anchor is the range under the mouse).
  `enum HoverStartMode { Delayed, Immediate }`, `enum HoverStartSource { Mouse, Keyboard }` (Monaco also has `Click`: not ported).
  `internal interface IHoverPart { Range Range { get; } int Order { get; } bool IsValidForHoverAnchor(HoverAnchor anchor); }` with the implementations
  `MarkerHoverPart(Range Range, Marker Marker)` (`Order = 1`: Monaco `hoverOrdinal` of the marker participant) and `MarkdownHoverPart(Range Range, IReadOnlyList<MarkdownString> Contents, int Ordinal)` (`Order = 3`), plus a third, `LoadingHoverPart(Range Range)` (`Order = 2000`, shown while providers are pending:
  Monaco `createLoadingMessage` uses ordinal 2000 and the text "Loading…"; the widget draws the text "Loading…" for it). `IsValidForHoverAnchor` for all three: `Range.StartColumn <= anchor.Range.StartColumn && Range.EndColumn >= anchor.Range.EndColumn` (Monaco, same line is guaranteed by the caller).
  Parts are `sealed record`s; a part's `Range` is on one line (the anchor's line).
- `EditorHoverOptions` (`sealed record`): `bool Enabled = true`, `TimeSpan Delay = 300 ms`, `bool Sticky = true`, `TimeSpan HidingDelay = 300 ms` (Monaco defaults of `EditorOption.hover`); `static Default`. (`above`, `showLongLineWarning` are not ported; the popup is always below the anchor unless there is no room, P8-11.)
- `HoverComputerOptions(HoverAnchor Anchor, HoverStartSource Source, bool InsistOnKeepingHoverVisible = false)`; `HoverResult(IReadOnlyList<IHoverPart> Value, bool IsComplete, bool HasLoadingMessage, HoverComputerOptions Options)`.
- `interface IHoverComputer { Task<IReadOnlyList<IHoverPart>>? ComputeAsync(HoverComputerOptions options, CancellationToken token); IReadOnlyList<IHoverPart> ComputeSync(HoverComputerOptions options); }`
  (Monaco's `AsyncIterableProducer` becomes one `Task` of a list; ADR 0011 decision 4. `ComputeAsync` returns `null` when nothing is asynchronous.)
- **`ContentHoverComputer(TextModel model, MarkerDecorations markers, LanguageFeatureRegistry<IHoverProvider> providers, Func<string> getLanguageId) : IHoverComputer`.**
  - `GetLineDecorations(anchor)` (private static or internal for tests; port of `_getLineDecorations`): `lineNumber = anchor.Range.StartLineNumber`; if `lineNumber > model.LineCount` → empty; `maxColumn = model.GetLineMaxColumn(lineNumber)`;
    `model.GetLineDecorations(lineNumber)` (internal member of Core, all owners) filtered exactly as Monaco: whole-line decorations are kept; `startColumn = d.Range.StartLineNumber == lineNumber ? d.Range.StartColumn : 1`, `endColumn = d.Range.EndLineNumber == lineNumber ? d.Range.EndColumn : maxColumn`;
    a `ShowIfCollapsed` decoration is kept unless `startColumn > anchor.Range.StartColumn + 1 || anchor.Range.EndColumn - 1 > endColumn`; others are kept unless `startColumn > anchor.Range.StartColumn || anchor.Range.EndColumn > endColumn`.
  - `ComputeSync(options)` (the marker participant): `model.ValidateRange(anchor.Range) != anchor.Range` → empty (Monaco `isValidRange`); for each line decoration `d` (same `startColumn` / `endColumn` as above) with a marker `markers.GetMarker(d.Id)`: a `MarkerHoverPart(new Range(anchorLine, startColumn, anchorLine, endColumn), marker)`. The result order is the decoration order.
    The decoration-message participant of Monaco (`hoverMessage`) is not ported (decorations have no `hoverMessage` here).
  - `ComputeAsync(options, token)` (the markdown participant + `getHover.ts`): `languageId = getLanguageId()`; `ordered = providers.Ordered(languageId)`; none → `null`. `position = options.Anchor.Range.GetStartPosition()`.
    For each provider (index `i`) call `ProvideHoverAsync(model, position, token)` **on the calling (UI) thread, inside a try / catch**: an exception thrown synchronously counts as "no result". Wrap each returned task so a faulted or cancelled task is `null` (`ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : null, TaskScheduler.Default)`);
    `Task.WhenAll` of the wrapped tasks, then build the parts in provider order: skip `null` hovers and hovers for which `MarkdownString.IsNullOrEmpty(hover.Contents)`; the part range is `hover.Range ?? options.Anchor.Range`; a `hover.Range` that spans several lines is replaced by the anchor range. **This runs on a thread-pool thread: it must not touch the model** (ADR 0011 decision 4): the ranges are validated later, on the UI thread, by `HoverController.OnResult` (P8-10);
    `new MarkdownHoverPart(range, hover.Contents, ordinal: i)`. (Behavior difference: Monaco's `isValid` drops a hover without a `range`; LoomEditor keeps it, because LSP's `range` is optional and Monaco's own doc says the default is the word at the position. Document it in `docs/PORTING.md`.)
- **`HoverOperation`** — a faithful port of `hoverOperation.ts` on `IEditorScheduler`:
  `HoverOperation(IEditorScheduler scheduler, IUiDispatcher dispatcher, IHoverComputer computer, Func<TimeSpan> getHoverDelay) : IDisposable`; `event EventHandler<HoverResult>? Result`; `HoverComputerOptions? Options { get; }`; `void Start(HoverStartMode mode, HoverComputerOptions options)`; `void Cancel()`; `Dispose()` = `Cancel` + never fires again.
  States `Idle`, `FirstWait`, `SecondWait`, `WaitingForAsync`, `WaitingForAsyncShowingLoading`. `firstWait = delay / 2`, `secondWait = delay - firstWait`, `loadingMessageTime = 3 * delay` (re-read `getHoverDelay()` at each `Start`).
  The three `Debouncer`s are `RunOnceScheduler`s (existing class) that store the options given to `Schedule(options, delay)`; at fire time they call `_triggerAsyncComputation`, `_triggerSyncComputation`, `_triggerLoadingMessage`.
  `_setState(state, options)`: store both, then `_fireResult(options)`. `_fireResult`: return silently in `FirstWait` / `SecondWait`; else raise `Result` with a copy of the results, `IsComplete = state == Idle`, `HasLoadingMessage = state == WaitingForAsyncShowingLoading`.
  `_triggerAsyncComputation(options)`: `_setState(SecondWait)`, schedule the sync computation after `secondWait`; `task = computer.ComputeAsync(options, cts.Token)` (a new `CancellationTokenSource` per operation, disposed by `Cancel`); if `null` → `_asyncDone = true`; else `UiContinuation.Then(task, dispatcher, parts => { _result.AddRange(parts); _asyncDone = true; _fireResult(options); if (state is WaitingForAsync or WaitingForAsyncShowingLoading) _setState(Idle, options); }, token: cts.Token)`
  (a faulted task: `onError` marks `_asyncDone = true` and does the same state step with no parts).
  `_triggerSyncComputation(options)`: `_result.AddRange(computer.ComputeSync(options))`, then `_setState(_asyncDone ? Idle : WaitingForAsync, options)`. `_triggerLoadingMessage(options)`: if the state is `WaitingForAsync` → `_setState(WaitingForAsyncShowingLoading, options)`.
  `Start(Delayed)`: only when `Idle`: `_setState(FirstWait)`, schedule async after `firstWait`, schedule the loading message after `loadingMessageTime`. `Start(Immediate)`: `Idle` → `_triggerAsyncComputation`, cancel the sync scheduler, `_triggerSyncComputation`; `SecondWait` → cancel the sync scheduler, `_triggerSyncComputation`.
  `Cancel()`: cancel the three schedulers, cancel and dispose the CTS, clear results, `Options = null`, state `Idle`, `_asyncDone = false`.
- `<remarks>Threading: UI thread only (the provider tasks complete anywhere; the results come back through the dispatcher).</remarks>` on every class with state. The Monaco header lines on every ported file.

## Tests

- [ ] New (`HoverAnchorTests`): `Equals_ComparesRangesOnly` (different mouse coordinates, same range → equal), `CanAdoptVisibleHover_SameLineOnly`
- [ ] New (`ContentHoverComputerTests`; a model with a language, a `MarkerService` + `MarkerDecorations` on it, a `LanguageFeaturesService`; comments `// Monaco: ContentHoverComputer.computeSync`): `ComputeSync_MarkerOnAnchor_ReturnsMarkerPart` (marker on columns 2–5, anchor column 3), `ComputeSync_AnchorOutsideTheMarker_Empty`, `ComputeSync_TwoMarkers_BothInDecorationOrder`,
      `ComputeSync_ShowIfCollapsed_RelaxedByOneColumn` (an empty marker at column 5, anchor at column 6 → found; anchor at column 7 → not), `ComputeSync_MultiLineMarker_ClampedToTheAnchorLine`, `ComputeSync_InvalidAnchorRange_Empty`, `ComputeSync_NonMarkerDecoration_Ignored`,
      `ComputeAsync_NoProviders_Null`, `ComputeAsync_Provider_ReturnsMarkdownPart` (`FakeHoverProvider`, hover without range → the part range is the anchor range), `ComputeAsync_ProviderRange_IsUsed`, `ComputeAsync_ProviderRangeSpanningLines_ReplacedByAnchorRange`,
      `ComputeAsync_EmptyContents_Dropped`, `ComputeAsync_NullHover_Dropped`, `ComputeAsync_FaultedProvider_IgnoredOthersKept`, `ComputeAsync_ThrowingProvider_IgnoredOthersKept` (the provider throws synchronously), `ComputeAsync_TwoProviders_OrderedByProviderIndex`
      (registry order: newest first; the part ordinals are 0 and 1 in that order, whatever the completion order of the tasks; use `Pending()` and complete them in reverse), `ComputeAsync_OtherLanguageProvider_NotCalled`, `ComputeAsync_StarProvider_Called`, `ComputeAsync_PassesTheAnchorStartPosition`
- [ ] New (`HoverOperationTests`; `ManualEditorScheduler`, `ManualUiDispatcher`, a scripted `IHoverComputer` with a `TaskCompletionSource`; comments `// Monaco: HoverOperation`; delay 300 ms): `Delayed_NoResultBeforeTheHoverDelay` (advance 299 ms: no event), `Delayed_SyncOnly_ResultAtTheDelay` (no async: one event at 300 ms, `IsComplete`),
      `Delayed_AsyncStartsAtHalfTheDelay` (the computer's `ComputeAsync` is called at 150 ms, not before), `Delayed_AsyncCompletesBeforeTheDelay_ResultAtTheDelay_Complete`, `Delayed_AsyncPending_PartialResultAtTheDelay_NotComplete` (sync parts shown, `IsComplete` false),
      `Delayed_AsyncCompletesLater_FiresCompleteResult` (after the partial one, completing the task and `RunAll()` gives a second event with sync + async parts, complete), `Delayed_AsyncPending_LoadingMessageAtThreeTimesTheDelay` (`HasLoadingMessage` at 900 ms),
      `Delayed_WhileNotIdle_IsIgnored` (a second `Start(Delayed)` with another anchor does nothing until `Cancel`), `Immediate_FromIdle_ComputesAtOnce`, `Immediate_InSecondWait_RunsTheSyncPartNow`, `Cancel_StopsEverything_AndCancelsTheToken`, `Cancel_ThenLateAsyncResult_Dropped`,
      `Dispose_NeverFiresAgain`, `Options_AreTheOnesOfTheLastStart`, `FaultedAsync_Completes_WithSyncPartsOnly`, `DelayIsReadAtEachStart` (change the delay to 100 ms between two operations: the second fires at 100 ms)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: the six Monaco files above (what is not ported: foreign-element anchors, `Click` source, verbosity, color picker, code actions, status bar, related information, `hoverMessage` decorations) and the behavior difference on `isValid`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The controller that reacts to the mouse (P8-10), the widget (P8-11), `hover.above`, long line warnings, hover verbosity.

## Suggested commit message

`feat(view): compute hovers from markers and hover providers`
