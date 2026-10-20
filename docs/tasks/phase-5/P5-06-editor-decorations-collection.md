# P5-06 — Editor id and `EditorDecorationsCollection`

- **Status:** todo
- **Depends on:** P5-05
- **Size:** S

## Goal

Each editor has an id that owns its decorations, and features manage their decorations the way Monaco's contributions
do: through a decorations collection (`editor.createDecorationsCollection()`) or through the editor's
`changeDecorations` / `deltaDecorations` with that owner id.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

File: `src/vs/editor/browser/widget/codeEditor/codeEditorWidget.ts`.

| Lines | What to port |
|---|---|
| 296, 2092 | `this._id = (++EDITOR_ID)`, `let EDITOR_ID = 0` |
| 1331–1357 | `createDecorationsCollection`, `changeDecorations`, `getLineDecorations`, `getDecorationsInRange` (with `filterValidationDecorations` / `filterFontDecorations` = `false`, see below) |
| 1369–1388 | `deltaDecorations`, `removeDecorations` |
| 2433–2516 | `class EditorDecorationsCollection` |
| `src/vs/editor/common/editorCommon.ts` 544–582 | `IEditorDecorationsCollection` (the members, as documentation) |

## Files to create or change

- `src/LoomEditor.View/Features/EditorDecorationsCollection.cs` — create (internal)
- `src/LoomEditor.View/Features/EditorContext.cs` — change: `internal int Id`; the internal members below; pass the id to the view model
- `src/LoomEditor.View/Layout/ViewModel.cs` — change: constructor parameter `int editorId = 0` (last, optional) and
  `public int EditorId { get; }`
- `tests/LoomEditor.View.Tests/Features/EditorDecorationsCollectionTests.cs` — create
- `tests/LoomEditor.View.Tests/Features/EditorContextTests.cs` — change

## Design decisions (already made — do not change)

- **Editor id.** `EditorContext.Id` comes from a static counter (`Interlocked.Increment`, first id 1), given to the
  `ViewModel` (`EditorId`). Owner 0 stays "no owner / all owners" in the model (P5-03).
- **`EditorDecorationsCollection(TextModel model, int ownerId, IReadOnlyList<ModelDeltaDecoration>? decorations = null)`**,
  namespace `LoomEditor.View.Features`, members with Monaco's names: `int Count` (`length`), `Range? GetRange(int index)`,
  `IReadOnlyList<Range> GetRanges()`, `bool Has(ModelDecoration decoration)`, `string[] Set(IReadOnlyList<ModelDeltaDecoration>)`,
  `string[] Append(IReadOnlyList<ModelDeltaDecoration>)`, `void Clear()`. `Set` and `Append` go through
  `model.ChangeDecorations(ownerId, …)` exactly as Monaco (with `_isChangingDecorations`).
- **`onDidChange`** → `IDisposable OnDidChange(Action<ModelDecorationsChangedEventArgs> listener)`: subscribes to
  `TextModel.DecorationsChanged` and skips the events raised while the collection itself is changing; disposing the
  result unsubscribes. Behavior difference (XML doc): Monaco listens to the editor's `onDidChangeModelDecorations`,
  which the view model re-emits; here the model event is used directly (same event, no view model in between).
- `editor.hasModel()` is always true (an `EditorContext` always has its model): those checks are dropped.
- **`EditorContext` internal members** (Monaco's editor API, owner = `Id`):
  `EditorDecorationsCollection CreateDecorationsCollection(IReadOnlyList<ModelDeltaDecoration>? decorations = null)`,
  `T ChangeDecorations<T>(Func<IModelDecorationsChangeAccessor, T> callback)` and the `Action` overload,
  `string[] DeltaDecorations(IReadOnlyList<string> oldIds, IReadOnlyList<ModelDeltaDecoration> newDecorations)` (both
  empty → returns `oldIds`, like Monaco), `void RemoveDecorations(IReadOnlyList<string> ids)`,
  `IReadOnlyList<ModelDecoration> GetLineDecorations(int lineNumber)`, `IReadOnlyList<ModelDecoration> GetDecorationsInRange(Range range)`.
  `filterValidationDecorations(options)` is `false`: no validation decorations exist in phase 5 and the
  `renderValidationDecorations` option is not ported (comment at the call).
- **How a feature reaches them.** A feature gets its `IEditorContext`; it uses `context as EditorContext` (null → the
  feature does nothing, like `GetViewModel()`). Nothing is added to the public `IEditorContext`.

## Tests

No Monaco test covers the collection. New:

- [ ] `Set_ReplacesThePreviousDecorations` (count, ranges, `GetAllDecorations` of the model)
- [ ] `Append_KeepsThePreviousDecorations`
- [ ] `Clear_RemovesEverything`
- [ ] `GetRange_IndexOutOfRange_IsNull`
- [ ] `GetRanges_AfterAnEdit_FollowTheText`
- [ ] `Has_OwnDecoration_IsTrue_OtherDecoration_IsFalse`
- [ ] `OnDidChange_NotRaisedForItsOwnChanges_RaisedForOtherChanges`
- [ ] `Decorations_AreOwnedByTheEditor` (the model's `GetDecorationsInRange(range, ownerId: id)` of another id does not
  see them)
- [ ] `Id_TwoContexts_AreDifferentAndNotZero` (in `EditorContextTests`)
- [ ] `DeltaDecorations_BothEmpty_ReturnsTheOldIds` (in `EditorContextTests`)

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map updated (`codeEditorWidget.ts` decorations part)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Showing decorations (P5-07 … P5-09). `setDecorationsByType` / `removeDecorationsByType` (decoration types of the
editor service). `getFontSizeAtPosition`.

## Suggested commit message

`feat(view): add editor decorations collections`
