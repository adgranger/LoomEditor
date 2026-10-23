# P8-21 — The `Suggest` feature: commands, keybindings and context keys

- **Status:** todo
- **Depends on:** P8-20
- **Size:** M

## Goal

Completion works from the keyboard in an editor with the default features: typing opens the list, `Down` / `Up` / `PageDown` / `PageUp` move in it, `Tab` or `Enter` accept, `Esc` closes it, `Ctrl+Space` opens it on demand. Everything is wired through the existing command and keybinding system; the list itself is still a fake presenter (P8-22 draws it).

## Monaco sources (pinned commit `0c9c0a4295a9222e12a605537738719455ae3056`)

| File | Lines | What to port |
|---|---|---|
| `src/vs/editor/contrib/suggest/browser/suggestController.ts` | 795–825 (`TriggerSuggestAction`), 837–1020 and the rest of the `registerEditorCommand` calls up to ≈ 1120 (`acceptSelectedSuggestion`, `hideSuggestWidget`, `selectNextSuggestion`, `selectNextPageSuggestion`, `selectLastSuggestion`, `selectPrevSuggestion`, `selectPrevPageSuggestion`, `selectFirstSuggestion`) | ids, preconditions, default keys (Windows column) |
| `src/vs/editor/contrib/suggest/browser/suggest.ts` | 31–42 (`Context`: `Visible`, `HasFocusedSuggestion`, `MultipleSuggestions`, `MakesTextEdit`, `AcceptSuggestionsOnEnter`) | the context keys |

## Files to create or change

- `src/LoomEditor.View/Features/Suggest/SuggestFeature.cs`, `SuggestCommands.cs`, `README.md` — create (**all three are required by `ConventionTests`**)
- `src/LoomEditor.View/Features/EditorFeatures.cs` — change: `() => new SuggestFeature(),` after `HoverFeature`
- `src/LoomEditor.View/Features/EditorContext.cs` — change: `internal bool SuggestWidgetVisible { get; set; }`, `internal bool SuggestWidgetMultipleSuggestions { get; set; }`
- `src/LoomEditor.View/Commands/KeybindingContext.cs` — change: new last parameters `bool SuggestWidgetVisible = false, bool SuggestWidgetMultipleSuggestions = false, bool HasCompletionItemProvider = false`
- `src/LoomEditor.View/Commands/WhenClauses.cs`, `DefaultKeybindings.cs` — change
- `src/LoomEditor.WinUI/Controls/LoomEditor.Keyboard.cs` — change: the three values in `CreateKeybindingContext`
- `tests/LoomEditor.View.Tests/Features/Suggest/SuggestFeatureTests.cs`, `SuggestKeybindingsTests.cs`, `SuggestEndToEndTests.cs` — create

## Design decisions (already made — do not change)

- **`SuggestCommands`** (`public static class`, constants with Monaco's ids): `TriggerSuggest = "editor.action.triggerSuggest"`, `AcceptSelectedSuggestion = "acceptSelectedSuggestion"`, `HideSuggestWidget = "hideSuggestWidget"`, `SelectNextSuggestion = "selectNextSuggestion"`, `SelectPrevSuggestion = "selectPrevSuggestion"`,
  `SelectNextPageSuggestion = "selectNextPageSuggestion"`, `SelectPrevPageSuggestion = "selectPrevPageSuggestion"`, `SelectFirstSuggestion = "selectFirstSuggestion"`, `SelectLastSuggestion = "selectLastSuggestion"`. XML doc on each, like `FindCommands`.
- **`SuggestFeature : IEditorFeature`** (`Id = "loom.suggest"`): `Attach(context)` — only when `context is EditorContext` and `GetLanguageFeatures()` is not null — creates `new SuggestController(context, Options)` (`public SuggestModelOptions Options { get; set; }`, default `SuggestModelOptions.Default`, a property like `WordHighlighterFeature.Options`; setting it recreates nothing: it applies to the **next** session — document it),
  registers the nine commands (`context.Commands.Register(new EditorCommand(id, run))`, registrations disposed in `Dispose`), subscribes to `controller.List.Changed` and `controller.Model.DidCancel` to keep `EditorContext.SuggestWidgetVisible` (`controller.IsVisible`) and `SuggestWidgetMultipleSuggestions` (`controller.List.Items.Count > 1`) up to date, and exposes `internal SuggestController? Controller`.
  Command bodies: `TriggerSuggest` → `controller.Trigger()` (nothing when read-only, no provider for the language, or the list is visible: Monaco's precondition `writable && hasCompletionItemProvider && !suggestWidgetVisible`; check the three in the command because command execution is not filtered by keybinding `when`);
  `AcceptSelectedSuggestion` → `controller.AcceptSelected()`; `HideSuggestWidget` → `controller.Cancel()`; the six select commands → the controller methods. Each is a no-op when `!controller.IsVisible`.
- **Context** (`KeybindingContext`, doc comments with the Monaco names): `SuggestWidgetVisible` (`suggestWidgetVisible`), `SuggestWidgetMultipleSuggestions` (`suggestWidgetMultipleSuggestions`), `HasCompletionItemProvider` (`editorHasCompletionItemProvider`).
  `WhenClauses` (names after Monaco's expressions): `SuggestVisible(c) => c.SuggestWidgetVisible && c.TextInputFocus` (Tab, Enter, Escape), `SuggestNavigation(c) => c.SuggestWidgetVisible && c.SuggestWidgetMultipleSuggestions && c.TextInputFocus` (the six selection keys),
  `TriggerSuggest(c) => c.TextInputFocus && c.Writable && c.HasCompletionItemProvider && !c.SuggestWidgetVisible`. `AcceptSuggestionsOnEnter` and `MakesTextEdit` are always true in phase 8 (`acceptSuggestionOnEnter: on`, no `smart` mode): Enter has the same condition as Tab; say it in a comment.
  The control (`CreateKeybindingContext`): `SuggestWidgetVisible: _context.SuggestWidgetVisible`, `SuggestWidgetMultipleSuggestions: _context.SuggestWidgetMultipleSuggestions`, `HasCompletionItemProvider: _context.LanguageFeatures.CompletionProviders.Has(<the model's language id>)` (`_context.GetLanguageId()` through the extension; it is `internal` to View, the WinUI assembly sees View internals).
- **Default keybindings** (Windows column of Monaco; `DefaultKeybindings.cs`; weight `KeybindingWeight.EditorContrib + 90` for every rule except trigger, which is `KeybindingWeight.EditorContrib`; add a named constant `SuggestWeight` next to the existing weights and look at how `FindWidgetWeight` is defined to do it the same way):
  `Tab` → accept (`SuggestVisible`); `Enter` → accept (`SuggestVisible`); `Escape` and `Shift+Escape` → hide (`SuggestVisible`); `Down` and `Ctrl+Down` → next (`SuggestNavigation`); `Up` and `Ctrl+Up` → prev; `PageDown` and `Ctrl+PageDown` → next page; `PageUp` and `Ctrl+PageUp` → prev page;
  `Ctrl+Space` and `Ctrl+I` → trigger (`TriggerSuggest`). `selectFirstSuggestion` / `selectLastSuggestion` have no default key (as in Monaco). `Shift+Enter` / `Shift+Tab` (the alternative accept) are not bound.
  These rules must **win over** the core ones (`cursorDown`, `tab`, `cancelSelection`): the weight does that; check the existing tests of the resolver for how competing rules are ordered and add a test that proves it (below).
- `<remarks>Threading: UI thread only.</remarks>`. README.md of the feature: role (completion), Monaco sources, files, tests, known limits (plain-text insertion only, no snippets, one cursor, no details pane).
- The feature does **not** create a presenter: the control assigns `context.SuggestPresenter` (P8-22).

## Tests

- [ ] New (`SuggestFeatureTests`; `new EditorContext(scheduler)` + the dispatcher as in P8-16's helper, `SuggestFeature` attached, `FakeSuggestPresenter`, `FakeCompletionProvider`): `Attach_RegistersTheNineCommands`, `Dispose_UnregistersTheCommands_AndTheController`, `NonEditorContext_AttachDoesNothing`, `Flags_FollowTheList` (`SuggestWidgetVisible` true when the list shows, `MultipleSuggestions` for 2 items, both false after `Cancel`),
      `Trigger_Command_OpensTheList` (`Execute("editor.action.triggerSuggest")`, `Flush()`), `Trigger_Command_WhenVisible_NoOp`, `Trigger_Command_ReadOnly_NoOp`, `Trigger_Command_NoProvider_NoOp`, `Accept_Command_InsertsTheSelectedItem`, `Hide_Command_ClosesTheList`, `Select_Commands_MoveTheSelection` (down, down, up, page down, last, first through the list state),
      `Commands_WhenListHidden_NoOp`
- [ ] New (`SuggestKeybindingsTests`; the resolver helpers of `KeybindingResolver` tests): `Tab_WithSuggestVisible_ResolvesToAccept` and without it to the indent command (`tab`), `Enter_WithSuggestVisible_Accepts_WithoutIt_NewLine`, `Escape_WithSuggestVisible_ResolvesToHide_BeforeCancelSelection` (also with a non-empty selection),
      `Down_WithSuggestVisibleAndMultiple_ResolvesToNext_WithSingleItem_CursorDown`, `Up_PageUp_PageDown_Mapping`, `CtrlSpace_ResolvesToTrigger_OnlyWhenWritableAndProvider_AndNotVisible`, `CtrlI_ResolvesToTrigger`, `CtrlDown_WithSuggestVisible_ResolvesToNext`, `NoKeyBound_ForFirstLast`
- [ ] New (`SuggestEndToEndTests`; a context with the **default features** (`EditorFeatures.Default` attached the way `EditorFeatureHost` does, see `EditorFeatureHostTests`), a `csharp`-less plain text model, a provider with items `foo`, `foobar`, `bar`): `TypeAWord_ShowsFilteredList_AcceptWithEnter_InsertsAndCloses` (type `f`, `o`, flush the scheduler by 10 ms and the dispatcher: list shows `foo`, `foobar`; `Execute("acceptSelectedSuggestion")` → text `foo`), `TriggerCharacter_Dot_ShowsList`,
      `EscapeClosesAndTypingAgainReopens`, `TypingNonMatching_ClosesAutoList`, `AcceptThenUndo_RestoresTypedText`, `ManualTrigger_EmptyWord_ShowsAllItems`
- [ ] `ConventionTests.FeatureFolder_*` pass for `Suggest`

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `docs/PORTING.md` map: the commands and keybindings of `suggestController.ts` (not ported: `acceptAlternativeSelectedSuggestion`, `toggleSuggestionDetails`, `toggleExplainMode`, `toggleSuggestionFocus`, copy, `insertBestCompletion`, `insertNextSuggestion`, `insertPrevSuggestion`, `triggerSuggestAndAcceptBest`, mac keys)
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

The WinUI list (P8-22), `acceptSuggestionOnEnter: smart`, details pane, snippets.

## Suggested commit message

`feat(view): add the suggest feature with its commands and keybindings`
