# P8-36 — End-to-end tests: the real editor features against the fake language server

- **Status:** done
- **Depends on:** P8-35
- **Size:** M

## Goal

Tests that connect a real `EditorContext` (with the default features: suggest, hover, go to definition, the marker decorations) to a `LspEditorSession` talking to the fake language server, and check what the user would see through the fake presenters: the completion list after typing, an accepted item, late additional edits, squiggles, hover contents, `F12`. This is the proof that the layers of phase 8 fit together, without a window and without a real server.

## Monaco sources

Sources: none (specified in this card).

## Files to create or change

- `tests/LoomEditor.View.Tests/LoomEditor.View.Tests.csproj` — change: `ProjectReference` to `..\..\src\LoomEditor.Lsp\LoomEditor.Lsp.csproj` and `..\LoomEditor.Lsp.FakeServer\LoomEditor.Lsp.FakeServer.csproj`; `<Compile Include="..\LoomEditor.Lsp.Tests\TestHelpers\**\*.cs" Link="Lsp\TestHelpers\%(RecursiveDir)%(Filename)%(Extension)" />` (the same linking trick the project already uses for the Core test helpers: those helpers see the `internal` types of `LoomEditor.Lsp` through the `InternalsVisibleTo` added by P8-35)
- `tests/LoomEditor.View.Tests/Lsp/LspEditorHarness.cs`, `LspEditorIntegrationTests.cs`, `LspEditorFailureTests.cs` — create

## Design decisions (already made — do not change)

- **`LspEditorHarness`** (`internal sealed class : IAsyncDisposable`): builds `new EditorContext(scheduler, dispatcher)` with a `ManualEditorScheduler` and a `ManualUiDispatcher` (use the constructor overload that takes both: find it in `EditorContext.cs`; add `internal EditorContext(IEditorScheduler scheduler, IUiDispatcher dispatcher)` if absent — one line, the other overloads delegate to the 4-argument one);
  attaches `EditorFeatures.Default` the way `EditorFeatureHostTests` does (`grep -n "EditorFeatureHost" tests/LoomEditor.View.Tests/Features/EditorFeatureHostTests.cs`); registers the language `"fake"` (`context.RegisterLanguage("fake")`, `context.Model.Tokenization.SetLanguageId("fake")`); creates a `FakeSuggestPresenter` and a `FakeHoverPresenter` and assigns them to `context.SuggestPresenter` / `context.HoverPresenter`;
  sets `context.ViewModel.SetHasFocus(true)` and a viewport (`SetViewportSize`); starts a `FakeServerHost` (in process, or process mode when asked) and a `LspEditorSession` with the **internal** constructor and `post = dispatcher.Post`, `documentUri = context.DocumentUri`; `Task StartAsync()` awaits `session.StartAsync()` while pumping.
  Helpers: `SetText(string)`, `MoveCaret(line, column)`, `Type(string)` (one `ViewModel.Type(string, "keyboard")` call per character), `Command(string id)` (`context.Commands.Execute(id)`), `MoveMouseTo(line, column)` (raises `UserInputEvents.EmitMouseMove` with a `ContentText` target as the hover tests of P8-10 do), `CtrlClick(line, column)` (`EmitMouseUp` with `LeftButton`, `CtrlKey`),
  and **`Task PumpUntilAsync(Func<bool> condition, string what, TimeSpan? timeout = null)`**: loops (timeout default 10 s): `dispatcher.RunAll()`, `scheduler.AdvanceBy(TimeSpan.FromMilliseconds(20))`, `if (condition()) return`, `await Task.Delay(5)`; on timeout throws `TimeoutException($"Timed out waiting for {what}")`. `DisposeAsync` stops the session, disposes the host and the context.
- **Conventions of the tests**: no `Thread.Sleep` outside `PumpUntilAsync`; every wait goes through it; assertions are on the **fake presenters** (what would be shown) and on the model text / markers / selection; the fake server's `DocumentText` is compared after `BarrierAsync()`.
- The fake server is in process in every test except the ones marked "(process)".

## Tests

- [x] New (`LspEditorIntegrationTests`; text and positions as written, the fake's rules are in P8-28): `Start_RegistersProviders_AndOpensTheDocument` (after `StartAsync`: the three registries have a provider for `fake`, the server text equals the model text),
      `Typing_ShowsTheCompletionListFromTheServer` (text `"foo foobar\n"`, caret at (2,1), type `f`, `o`: the suggest presenter shows a list whose items include `foo`, `foobar` and the keyword `for`, and **not** `else` or `class`; the first item is the best match), `Typing_FilterNarrows` (type one more letter: the list narrows without a new server request: `server.ReceivedMethods` has exactly one `textDocument/completion`),
      `AcceptWithEnter_InsertsTheItem` (`Command("acceptSelectedSuggestion")`: the typed prefix is replaced, caret after it, the list closes, `BarrierAsync()`: the server's text equals the model's), `AcceptThenUndo_RestoresTheText`,
      `TriggerDot_ShowsMembers_AcceptReplacesNothing` (type `x.`: items `Length`, `Count`, `ToString`, `Equals` first; accept `Count` → `x.Count`), `AcceptMethod_WithCommitCharacter` (type `x.To`, then `(`: the item `ToString` is accepted and the `(` typed → `x.ToString(`), `AcceptLength_AppliesTheLateAdditionalEdit` (after accepting `Length` and pumping: the text starts with `// resolved` + a line break, the caret is still after `x.Length`),
      `ManualTrigger_CtrlSpace_ShowsAllWords`, `Escape_ClosesTheList`, `Diagnostics_BecomeMarkersAndSquiggles` (set the text to `"ERROR here WARN"`: after pumping, `context.Markers.Read()` has two markers and the model has two decorations with the classes `squiggly-error` and `squiggly-warning` — `context.Model.GetAllDecorations(context.Id)`), `Diagnostics_FollowTheEdits` (delete `ERROR`: one marker),
      `Diagnostics_StaleVersion_NotShown` (a `ScriptedServer` variant is **not** needed: type quickly twice without pumping between and check that after pumping only the diagnostics of the **latest** version are present — the fake republishes for every change; assert the final marker set is the one of the final text),
      `Hover_ShowsProviderMarkdown` (`MoveMouseTo` a word, advance 300 ms and pump: the hover presenter shows a `MarkdownHoverPart` whose text contains `Hover for **<word>**`), `Hover_OnMarkerWord_ShowsMarkerThenProvider` (on `ERROR`: the first part is a `MarkerHoverPart` with `Found ERROR`, the second the markdown), `Hover_LeaveHides`,
      `F12_GoesToTheDeclaration` (`"def alpha\nalpha"`, caret on the second `alpha`, `Command(GotoDefinitionCommands.RevealDefinition)`: selection collapsed at (1,5)), `CtrlClick_GoesToTheClassDeclaration` (the `class Beta` `LocationLink` case: caret at the `TargetSelectionRange` start), `F12_ExternalDefinition_CallsTheLocationOpener` (`context.LocationOpener` fake receives `file:///other/file.txt`), `F12_NoDefinition_Announces` (a `RecordingAnnouncer`),
      `Stop_RemovesProvidersMarkersAndSquiggles_AndTheServerSawShutdown`, `AfterStop_TypingDoesNotShowTheList`, `TextEditsAreSynced_ThroughTheWholeStack` (50 random typed / deleted characters with `new Random(7)`, pumping, then the server text equals the model text)
- [x] New (`LspEditorFailureTests`, process mode): `ServerCrash_RemovesTheFeatures` (`--exit-on=textDocument/completion`: type a letter, the server dies; the registries are empty, markers cleared, the session `Faulted`, a later keystroke does not throw), `ServerCrashDuringInitialize_StartFaults_EditorKeepsWorking` (`--exit-on=initialize`: typing and `F12` do nothing and do not throw),
      `ServerNotFound_StartFaults_EditorKeepsWorking`, `SlowServer_CompletionArrivesLate_StillShown` (`--delay=300`: the list appears after the delay), `SlowServer_TypingMoreCancelsTheOldRequest` (`--delay=300`: type, type again quickly: the server received a `$/cancelRequest`; the list reflects the latest word)

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green; `dotnet test --project tests/LoomEditor.View.Tests -c Release` run **five times in a row**, none failed, no `LoomEditor.Lsp.FakeServer` process left (these tests start a child process in some cases)
- [x] A failure that only appears when the whole `LoomEditor.slnx` test run is executed (not alone) is reported, not retried until green
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Real servers, WinUI, rendering; the Gallery (P8-37).

## Suggested commit message

`test: add end-to-end tests of the editor features against the fake language server`

## Implementation notes

- The sort order of the member items is alphabetical (`sortText = label`), so `TriggerDot` checks the four members are listed, not their order.
- The default hover is sticky: `Hover_LeaveHides` turns stickiness off through `LspEditorHarness.DisableStickyHover()`.
- Typing more while a request is pending does not cancel it (Monaco refilters instead); `SlowServer_TypingMoreCancelsTheOldRequest` triggers a second request with `editor.action.triggerSuggest`, which cancels the first.
- The slow-server tests run the fake in process (they read `ReceivedMethods`); the crash and not-found tests use the public constructor with a child process.
