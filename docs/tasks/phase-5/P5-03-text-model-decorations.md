# P5-03 — `TextModel` decorations: `DeltaDecorations`, `ChangeDecorations`, queries

- **Status:** todo
- **Depends on:** P5-02
- **Size:** M

## Goal

The text model holds decorations, not only tracked ranges: `DeltaDecorations`, `ChangeDecorations` with Monaco's
change accessor, `RemoveAllDecorationsWithOwnerId`, and the queries `GetDecorationRange`, `GetDecorationOptions`,
`GetLineDecorations`, `GetLinesDecorations`, `GetDecorationsInRange`, `GetAllDecorations`, filtered by owner. The
`DecorationsChanged` event is the next card. ADR 0009, decision 3.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/common/model/textModel.ts` unless said otherwise.

| Lines | What to port |
|---|---|
| 1669–1715 | `changeDecorations`, `_changeDecorations` (the accessor and its invalidation) — **without** the `_onDidChangeDecorations` calls (P5-04) |
| 1717–1739 | `deltaDecorations` (the recursion counter, without the emitter calls) |
| 1749–1757 | `_setTrackedRange`: the "node doesn't exist" branch now calls `_deltaDecorationsImpl(0, [], [{ range, options }], true)` |
| 1777–1804 | `removeAllDecorationsWithOwnerId`, `getDecorationOptions`, `getDecorationRange` |
| 1806–1833 | `getLineDecorations`, `getLinesDecorations`, `getDecorationsInRange` — without the two `pushMany(… Provider …)` lines |
| 1869–1874 | `getAllDecorations` — without the two provider `concat` lines |
| 1880–1884 | `_getDecorationsInRange` |
| 1890–1934 | `_changeDecorationImpl` — without `recordLineAffectedBy…` (injected text, line heights, fonts are not ported) and without `checkAffectedAndFire` (P5-04) |
| 1936–1974 | `_changeDecorationOptionsImpl` — same cuts; the tree move only when Monaco moves it for the overview ruler / injected text, which never happens here (one tree): always `node.setOptions(options)` in place |
| 1976–2078 | `_deltaDecorationsImpl` — complete, minus the emitter calls (P5-04) and `recordLineAffectedBy…` |
| 2224–2238, 2283–2305 | `DecorationsTrees`: `_ensureNodesHaveRanges`, `getAllInInterval`, `getAll`, `_search`, `collectNodesFromOwner` (one tree) |
| `src/vs/editor/common/model.ts` 416–451 | `IModelDecorationsChangeAccessor` |
| `src/vs/editor/test/common/model/modelDecorations.test.ts` 17–78, 80–443, 1139–1428 | the helpers and the tests listed below |

## Files to create or change

- `src/LoomEditor.Core/Model/TextModel.Decorations.cs` — create (partial): the public-in-Monaco API above, all **internal**
- `src/LoomEditor.Core/Decorations/IModelDecorationsChangeAccessor.cs` — create
- `src/LoomEditor.Core/Model/TextModel.TrackedRanges.cs` — change: the create path of `SetTrackedRange` calls
  `DeltaDecorationsImpl(0, [], [new(range, TrackedRangeOptions[(int)stickiness])], suppressEvents: true)`; move
  `_decorations`, `_lastDecorationId`, `_instanceId` to `TextModel.Decorations.cs`
- `src/LoomEditor.Core/Decorations/DecorationsTrees.cs` — change: `GetAllInInterval`, `GetAll`, `CollectNodesFromOwner`
- `src/LoomEditor.Core/Model/TextModel.cs` — change: class remarks ("decorations" is no longer "not ported yet")
- `tests/LoomEditor.Core.Tests/Model/ModelDecorationsTests.cs` — create (helpers of lines 17–78 as private methods)

## Design decisions (already made — do not change)

- Phase-5 README section 3 gives the shape. Signatures (internal, on `TextModel`):
  - `string[] DeltaDecorations(int ownerId, IReadOnlyList<string> oldDecorationIds, IReadOnlyList<ModelDeltaDecoration> newDecorations)`
    (both empty → returns `[]` at once, like Monaco)
  - `T ChangeDecorations<T>(int ownerId, Func<IModelDecorationsChangeAccessor, T> callback)` and
    `void ChangeDecorations(int ownerId, Action<IModelDecorationsChangeAccessor> callback)`
  - `void RemoveAllDecorationsWithOwnerId(int ownerId)`
  - `ModelDecorationOptions? GetDecorationOptions(string id)`, `Range? GetDecorationRange(string id)`
    (`GetTrackedRange` now calls `GetDecorationRange`)
  - `IReadOnlyList<ModelDecoration> GetLineDecorations(int lineNumber, int ownerId = 0, bool filterOutValidation = false, bool filterFontDecorations = false)`
    (out-of-range line → empty list, like Monaco), same optional parameters on `GetLinesDecorations(int start, int end, …)`,
    `GetDecorationsInRange(Range range, …)` (the range is validated first) and `GetAllDecorations(int ownerId = 0, …)`.
    Monaco's `onlyMinimapDecorations` / `onlyMarginDecorations` parameters are dropped (minimap: phase 6; margin-only
    queries serve the glyph margin lanes, not ported); the tree is called with `onlyMarginDecorations: false`.
  - `private string[] DeltaDecorationsImpl(int ownerId, IReadOnlyList<string> oldIds, IReadOnlyList<ModelDeltaDecoration> newDecorations, bool suppressEvents = false)`
    (`suppressEvents` is used by P5-04).
- **Owner filter.** As Monaco's interval tree: owner 0 means "all owners"; a decoration of owner 0 is seen by every
  owner. Tracked ranges (owner 0, no class name) are therefore returned by the queries, as in Monaco.
- **Results** are `ModelDecoration` values built from the nodes after `_ensureNodesHaveRanges` (Monaco returns the
  nodes themselves). Order: the tree order (Monaco concatenates three trees; there is one).
- **Accessor.** A private nested `ChangeAccessor` class implements `IModelDecorationsChangeAccessor`
  (`string AddDecoration(Range, ModelDecorationOptions)`, `void ChangeDecoration(string id, Range newRange)`,
  `void ChangeDecorationOptions(string id, ModelDecorationOptions options)`, `void RemoveDecoration(string id)`,
  `string[] DeltaDecorations(IReadOnlyList<string> oldIds, IReadOnlyList<ModelDeltaDecoration> newDecorations)`).
  After the callback returns it is invalidated: every method throws `InvalidOperationException` (Monaco: `invalidFunc`).
  Behavior difference (XML doc + PORTING.md): an exception thrown by the callback propagates (Monaco reports it with
  `onUnexpectedError` and returns `null`).
- **Recursion.** `deltaDecorations` called from inside a `DecorationsChanged` handler: Monaco warns. Here:
  `Guard.Assert(_deltaDecorationCallCnt <= 1, "Invoking DeltaDecorations recursively could lead to leaking decorations.")`.
- **Provider cut points.** Where Monaco adds the bracket pair colorization decorations (`_decorationProvider`, the
  `pushMany` / `concat` lines), write `// Phase 5 cut point (bracket pair colorization, step C): <Monaco line>`. The
  font token provider lines are not ported (no comment needed beyond the class remarks).
- `TextModel` has no `Dispose`: `removeAllDecorationsWithOwnerId`'s `_isDisposed` check is dropped.

## Tests

Port (`modelDecorations.test.ts`; `createTextModel` → `new TextModel(text)`; option literals → `ModelDecorationOptions.CreateDynamic(new() { Description = "test", ClassName = … })`):

- [ ] `Editor Model - Model Decorations` (24): `single character decoration`, `line decoration`, `full line decoration`,
  `multiple line decoration`, `decoration gets removed`, `decorations get removed`, `decoration range can be changed`,
  `decorations are updated when inserting one line text before it`, `decorations are updated when inserting one line text before it 2`,
  `decorations are updated when inserting multiple lines text before it`, `decorations change when inserting text after them`,
  `decorations are updated when inserting text inside`, `decorations are updated when inserting text inside 2`,
  `decorations are updated when inserting text inside 3`, `decorations are updated when inserting multiple lines text inside`,
  `decorations are updated when deleting one line text before it`, `decorations are updated when deleting multiple lines text before it`,
  `decorations are updated when deleting multiple lines text before it 2`, `decorations are updated when deleting text inside`,
  `decorations are updated when deleting text inside 2`, `decorations are updated when deleting multiple lines text`,
  `decorations are updated when changing EOL`, `an apparently simple edit`, `removeAllDecorationsWithOwnerId works`
- [ ] `deltaDecorations` (7): `result respects input`, `deltaDecorations 1`, `deltaDecorations 2`, `deltaDecorations 3`,
  `model doesn't get confused with individual tracked ranges`, `issue #16922: Clicking on link doesn't seem to do anything`,
  `issue #41492: URL highlighting persists after pasting over url`
- Not ported: `injected text width must be finite and non-negative` (injected text, ADR 0009 decision 4),
  `removeAllDecorationsWithOwnerId can be called after model dispose` (no `TextModel.Dispose`),
  `issue #4317: editor.setDecorations doesn't update the hover message` (`hoverMessage` not ported). The 5 event tests
  and the `Decorations and editing` suite are P5-04.
- [ ] New: `ChangeDecorations_AccessorUsedAfterTheCallback_Throws`
- [ ] New: `DeltaDecorations_UnknownOldId_IsSkipped`
- [ ] New: `GetDecorationsInRange_OwnerFilter_SeesOwnAndOwnerZeroDecorations` (owners 0, 1, 2; query with owner 1)
- [ ] New: `SetValue_RemovesAllDecorations`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md`: map row `textModel.ts` mentions decorations (P5-03); "Behavior differences": one decorations
      tree (no overview-ruler or injected-text tree), callback exceptions propagate
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

`DecorationsChanged` and its deferral (P5-04). The decoration providers (bracket pair colorization: step C).
`getOverviewRulerDecorations`, `getInjectedTextDecorations`, `getLineInjectedText`, custom line heights and font
decorations, `getAllMarginDecorations`.

## Suggested commit message

`feat(core): add decorations to the text model`
