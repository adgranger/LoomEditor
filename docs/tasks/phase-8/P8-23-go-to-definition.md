# P8-23 — Go to definition (`F12` and `Ctrl+click`)

- **Status:** done
- **Depends on:** P8-22
- **Size:** M

## Goal

`F12` on a symbol, or `Ctrl+click` on it, asks the registered definition providers and moves the cursor to the (first) definition in the same document; a definition in another document is handed to an `ILocationOpener` that the application provides; "no definition" is announced.

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/gotoSymbol/browser/goToSymbol.ts` | 14–60 (`shouldIncludeLocationLink`, `getLocationLinks`), 62–66 (`getDefinitionsAtPosition`) | `DefinitionProviders.GetDefinitionsAtPosition` |
| `src/vs/editor/contrib/gotoSymbol/browser/referencesModel.ts` | 140–297 (`ReferencesModel`: sorting, `ariaMessage`, `nearestReference`, `referenceAt`, `firstReference`, `_compareReferences`) | `DefinitionResults` (single group, no `FileReferences`, no preview) |
| `src/vs/editor/contrib/gotoSymbol/test/browser/referencesModel.test.ts` | 13–42 | the `nearestReference` test |
| `src/vs/editor/contrib/gotoSymbol/browser/goToCommands.ts` | 75–199 (`SymbolNavigationAction.runEditorCommand`, `_onResult`), 201–240 (`_openReference`, range choice), 253–300 (`DefinitionAction`, messages, `F12`) | the command |
| `src/vs/editor/contrib/gotoSymbol/browser/link/goToDefinitionAtPosition.ts` | 55–70 (mouse up → `gotoDefinition`), 284–301 (`isEnabled`) | Ctrl+click |

## Files to create or change

- `src/LoomEditor.View/Features/GotoDefinition/GotoDefinitionFeature.cs`, `GotoDefinitionCommands.cs`, `README.md` — create (**all three required by `ConventionTests`**)
- `src/LoomEditor.View/Features/GotoDefinition/DefinitionProviders.cs`, `DefinitionResults.cs`, `GotoDefinitionController.cs`, `ILocationOpener.cs` — create
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: `() => new GotoDefinitionFeature(),` after `SuggestFeature`
- `src/LoomEditor.View/Features/EditorContext.cs` — change: `public ILocationOpener? LocationOpener { get; set; }`
- `src/LoomEditor.View/Accessibility/AccessibilityMessages.cs` — change: `NoDefinitionFound(string? word)`, `DefinitionResultMessage(...)`, `DefinitionInOtherDocument(string uri)`
- `src/LoomEditor.View/Commands/KeybindingContext.cs` (`bool HasDefinitionProvider = false`), `WhenClauses.cs`, `DefaultKeybindings.cs` — change
- `src/LoomEditor.WinUI/Controls/LoomEditor.GotoDefinition.cs` — create: public `LocationOpener` property of the control; `LoomEditor.Keyboard.cs` — change: `HasDefinitionProvider`
- `src/LoomEditor.View/PublicAPI.Unshipped.txt`, `src/LoomEditor.WinUI/PublicAPI.Unshipped.txt` — regenerate
- `tests/LoomEditor.View.Tests/Features/GotoDefinition/*.cs` — create (list below)

## Design decisions (already made — do not change)

- **`ILocationOpener`** (**public**, `LoomEditor.View.Features.GotoDefinition`): `bool Open(LocationLink target);` — "open the document at `target.Uri` and select `target.TargetSelectionRange ?? target.Range`; return false if you cannot". Called on the UI thread. XML doc with the threading note. `EditorContext.LocationOpener` (public, settable, default null) and the control property `public ILocationOpener? LocationOpener { get; set; }` (plain property).
- **`DefinitionProviders.GetDefinitionsAtPosition(LanguageFeatureRegistry<IDefinitionProvider> registry, TextModel model, string languageId, Position position, CancellationToken token) → Task<IReadOnlyList<LocationLink>>`** (internal static; port of `getLocationLinks`): `providers = registry.Ordered(languageId)`; all asked at once on the calling thread (a synchronous throw = no result),
  each task wrapped so a fault / cancellation gives `null`; `Task.WhenAll`; the result is the concatenation, in provider order, of the non-null lists, dropping null items (Monaco `coalesce(values.flat())`); `shouldIncludeLocationLink` is **not** ported (no schemes). No providers → a completed task with an empty list. Same no-`await` style as P8-14.
- **`DefinitionResults`** (internal; port of `ReferencesModel` reduced to one flat list): constructor `DefinitionResults(IReadOnlyList<LocationLink> links)`: `providersFirst = links.FirstOrDefault()`; sort a copy with `_compareReferences` (**stable**): `string.CompareOrdinal(a.Uri, b.Uri)` then `Range.CompareRangesUsingStarts` (the existing Core member; find the exact name with `grep -n "CompareRangesUsingStarts\|CompareUsingStarts" src/LoomEditor.Core/Text/Range.cs`);
  drop a link when it compares equal to the previous kept one (same uri and same range start/end: `_compareReferences(link, last) == 0`); `References` (the kept links, as `IReadOnlyList<LocationLink>`), `IsEmpty`, `LocationLink? FirstReference` (the kept link that **is** `providersFirst` — reference equality — else `References[0]`),
  `LocationLink? NearestReference(string uri, Position position)` (port: sort by `commonPrefixLength(ref.Uri, uri)` descending then `|startLine − line| * 100 + |startColumn − column|` ascending; stable; first), `LocationLink? ReferenceAt(string uri, Position position)` (same uri, range contains the position),
  `string AriaMessage(string? currentDocumentUri)`: empty → `"No results found"`; one → `"Found 1 symbol in {uri}"`; several in one uri → `"Found {n} symbols in {uri}"`; several uris → `"Found {n} symbols in {m} files"` (Monaco `aria.result.*`; paths are the URI strings).
  Not ported: `FileReferences`, previews, `nextOrPreviousReference`, `clone`, `dispose`, `onDidChangeReferenceRange`.
- **`GotoDefinitionController(IEditorContext context) : IDisposable`** — `void Run(Position? position = null)` (Monaco `runEditorCommand` for `DefinitionAction`): `position ??= primary cursor`; `model`, `versionId = model.VersionId`, `startSelection = context.Selection`; a new `CancellationTokenSource` (the previous request is cancelled and disposed first);
  `UiContinuation.Then(DefinitionProviders.GetDefinitionsAtPosition(…), dispatcher, links => OnResults(…), token: token)`. **Cancellation on edit** (Monaco `EditorStateCancellationTokenSource` with `Value | Position`): in the callback, if `model.VersionId != versionId` or `context.Selection != startSelection` when `Run` was started from the keyboard / command (not from a mouse position), drop the result silently.
  `OnResults(links, position)`: `results = new DefinitionResults(links)`; announce `results.AriaMessage(uri)` (Monaco `alert(references.ariaMessage)`); `results.IsEmpty` → announce `AccessibilityMessages.NoDefinitionFound(word)` where `word = model.GetWordAtPosition(position)?.Word` (`"No definition found for 'x'"` or `"No definition found"`), return;
  otherwise `OpenReference(results.FirstReference!)` (Monaco `multipleDefinitions: 'peek'` with several results opens a peek view: LoomEditor has none, so it goes to the first reference; behavior difference).
  `OpenReference(link)` (port of `_openReference`, range choice included): `range = link.TargetSelectionRange ?? link.Range`; if `string.Equals(link.Uri, context.GetDocumentUri(), StringComparison.Ordinal)`: validate the range with `model.ValidateRange`, set the selection to the **collapsed start** of the range with source `"code.jump"` and reveal it with `NearTopIfOutsideViewport`
  (follow `BracketMatchingFeature.SetSelectionsAndReveal`: `viewModel.SetSelections("code.jump", [...])` then `viewModel.RevealRange("code.jump", true, viewRange, VerticalRevealType.NearTopIfOutsideViewport, ScrollType.Smooth)`); else if `context.LocationOpener is { } opener && opener.Open(link)` → done; else announce `AccessibilityMessages.DefinitionInOtherDocument(link.Uri)` (`"Definition in {uri}"`). The `symbolHighlight` flash of Monaco is not ported.
- **Command and keys** (`GotoDefinitionCommands`: `RevealDefinition = "editor.action.revealDefinition"`): `F12` with `WhenClauses.GotoDefinition(c) => c.EditorTextFocus && c.HasDefinitionProvider` (Monaco `editorHasDefinitionProvider && editorTextFocus && !isInEmbeddedEditor`), weight `KeybindingWeight.EditorContrib`; `KeybindingContext.HasDefinitionProvider` is true when the registry has a provider for the model's language
  (the control fills it like `HasCompletionItemProvider`). The command itself checks the same condition and does nothing without a provider. `Ctrl+F12` and `Ctrl+K F12` (side by side) are not bound.
- **Ctrl+click** (`GotoDefinitionFeature` subscribes to `UserInputEvents.MouseUp`, the existing event): enabled when `e.Event.LeftButton` (the released button; the control sets it for a release, see `OnPointerReleased`), `e.Event.CtrlKey` and neither `AltKey` nor `ShiftKey` (Monaco's `hasTriggerModifier`: with the default `multiCursorModifier = altKey` the trigger modifier is **Ctrl**), `e.Target.Type == MouseTargetType.ContentText` and `e.Target.Position is not null`; then `controller.Run(e.Target.Position)` (a mouse position, so the cursor-moved cancellation does not apply).
  Monaco also requires a single click (`isNoneOrSingleMouseDown`); the control reports the click count as 4 for every pointer event (see `EditorMouseEvent.Detail`), so this check is **not ported** (a Ctrl+double-click also jumps). Say so in the PORTING entry.
  A Ctrl+click also moves the cursor to the clicked position (the editor's own mouse-down does it before the release): that is fine, the jump overrides it.
- `GotoDefinitionFeature` (`Id = "loom.gotoDefinition"`): `Attach` creates the controller, registers the command, subscribes the mouse event; `Dispose` undoes all. `<remarks>Threading: UI thread only.</remarks>` everywhere. README.md of the feature folder: role, sources, files, limits (no peek, no side-by-side, no Ctrl+hover link underline, one cursor).
- `AccessibilityMessages`: `NoDefinitionFound(string? word)` (`word` null or empty → `"No definition found"`, else `$"No definition found for '{word}'"`), `DefinitionInOtherDocument(string uri)` → `"Definition in {uri}"`.

## Tests

- [x] Port (`DefinitionResultsTests`; `// Monaco: 'nearestReference'`): `nearestReference` (the four assertions with string URIs: `file:///out/obj/can`, `file:///out/obj/can2`, `file:///src/can`; ranges `(1,1,1,1)`; Monaco compares `uri.path`: compare the full uri strings)
- [x] New (`DefinitionResultsTests`): `Sorted_ByUriThenRange`, `Deduplicated_SameUriAndRange`, `FirstReference_IsTheFirstOfProviders` (the first link given, even if it sorts later), `FirstReference_Empty_Null`, `ReferenceAt_ContainsPosition`, `AriaMessage_ZeroOneManyOneFileManyFiles`, `NearestReference_PrefersCloserPositionOnTie`
- [x] New (`DefinitionProvidersTests`; `FakeDefinitionProvider` incl. `Pending()`): `NoProviders_Empty`, `TwoProviders_Concatenated_InProviderOrder` (complete in reverse order), `FaultedProvider_Ignored`, `ThrowingProvider_Ignored`, `NullList_Ignored`, `OtherLanguage_NotCalled`, `PassesThePosition`
- [x] New (`GotoDefinitionControllerTests`; `SuggestTestEditor`-like harness: a context with `ManualUiDispatcher`, a recording announcer, text with two lines):
      `Run_SameDocument_MovesTheCursorToTheTargetSelectionRange` (`TargetSelectionRange` wins over `Range`), `Run_UsesRangeWhenNoTargetSelectionRange`, `Run_OtherDocument_CallsTheOpener`, `Run_OtherDocument_NoOpener_Announces`, `Run_OpenerReturnsFalse_Announces`, `Run_NoResult_AnnouncesNoDefinitionForWord`, `Run_NoResultNoWord_AnnouncesGeneric`,
      `Run_Announces_AriaMessage`, `Run_ModelEditedWhileWaiting_Dropped` (keyboard run), `Run_CursorMovedWhileWaiting_Dropped`, `Run_FromMousePosition_NotDroppedWhenCursorMoved`, `Run_SecondRunCancelsTheFirst` (first token cancelled, only the second result applied), `Run_InvalidRange_Clamped`, `Run_MultipleResults_GoesToFirstProviderResult`,
      `Run_NoProvider_DoesNothing`, `Dispose_CancelsPending`
- [x] New (`GotoDefinitionFeatureTests`): `Attach_RegistersTheCommand`, `Command_WithoutProvider_NoOp`, `CtrlClick_OnText_RunsAtThePosition`, `Click_WithoutCtrl_DoesNothing`, `CtrlAltClick_DoesNothing`, `CtrlClick_OnGutter_DoesNothing`, `CtrlRightClick_DoesNothing` (not `LeftButton`), `F12_Resolves_WithProvider_AndNotWithout` (resolver helper), `Dispose_Unsubscribes`
- [x] `ConventionTests.FeatureFolder_*` pass for `GotoDefinition`

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [x] `./eng/Update-PublicApi.ps1` for `src/LoomEditor.View` and `src/LoomEditor.WinUI`, diffs checked (`ILocationOpener`, the two `LocationOpener` properties)
- [x] `docs/PORTING.md` map: `goToSymbol.ts` (`getDefinitionsAtPosition`), `referencesModel.ts` (reduced), `goToCommands.ts` (`DefinitionAction`, `F12`), `goToDefinitionAtPosition.ts` (Ctrl+click only) and the one ported test, with the behavior differences (no peek, no highlight flash, no Ctrl+hover underline, no side-by-side)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Peek definition, references / implementations / type definition / declaration, Ctrl+hover link underline, opening documents (that is the host's `ILocationOpener`), `Alt+F12`.

## Suggested commit message

`feat(view): add go to definition with F12 and Ctrl+click`
