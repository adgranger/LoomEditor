# P8-39 — Close phase 8

- **Status:** done
- **Depends on:** all phase 8 cards
- **Size:** S (docs)

## Goal

The documentation describes the code as it is at the end of phase 8, the roadmap is ticked **honestly** (what was built and tested is ticked; what only a human with the real server can verify stays pending), and the gaps left for later are listed.

## Monaco sources

Sources: none (documentation).

## Files to create or change

- `docs/ARCHITECTURE.md` — a section "Language features and the language server client (phase 8)" after "Accessibility (phase 7)": the three layers (provider contracts / registry / markers in Core → suggest, hover, go to definition and the marker decorations in View → the overlay layer, hover popup and completion list in WinUI; and beside them `LoomEditor.Lsp`: framing → `JsonRpcConnection` → `LspClient` → `LspDocumentSync`, providers, `LspDiagnosticsBinder` → `LspEditorSession`),
  a small diagram in the same ASCII style as the neighbouring ones for "typing a letter → completion list" (`ViewModel.Type` → `SuggestModel` quick suggest timer → `SuggestItems.ProvideAsync` → `LspCompletionProvider` → `JsonRpcConnection` → server → back through `UiContinuation` → `CompletionModel` → `SuggestController` → `ISuggestPresenter` → `SuggestWidget`), the threading summary (ADR 0011 decision 4), the fake server and how tests use it.
  "Where do I find…?" rows: language features registry and provider contracts; markers and squiggles; hover; completion; go to definition; the overlay layer; the LSP client and session; the fake language server; the Gallery's LSP demo and self-check. Check that the layer table row for `LoomEditor.Lsp` (P8-24) is still right.
- `docs/PORTING.md` — a "Done in phase 8 (language features)" section (like the earlier phases); the Monaco → LoomEditor map complete for everything ported in phase 8 (each card said which rows to add: check **every** card's "PORTING" acceptance line against the file and add what is missing); the behavior differences of phase 8 (search the cards for "behavior difference" and "Not ported"; one line each);
  "Tests ported in phase 8" with counts **counted from the test files** (`grep -c "\[Fact\]\|\[Theory\]"` per file, theories once): the fuzzy score and graceful tests, the completion model tests, `suggest.test.ts`, `markerService.test.ts` (six), `referencesModel.test.ts` (one), the ported `suggestModel.test.ts` scenarios (with the list of those **not** ported and why); update "Cut points for later phases":
  the rows that said "phase 8" (semantic highlights of `wordHighlighter`, folding range providers, sticky scroll click-link gesture, `hoverMessage` decorations) are marked **not delivered in phase 8** with the reason (ADR 0011 decision 7), not silently dropped.
- `docs/roadmap/phase-8-lsp.md` — rewrite the checklist with the card ids: every item the cards delivered is ticked, with the date; the "Done when: the Gallery demonstrates a real language server" line gets the **honest status**: "demonstrated against the fake language server (self-check on the Native AOT Gallery, P8-37 — copy the actual result); **not verified against `csharp-ls`**: manual plan section 4 (`docs/testing/phase-8-manual-tests.md`), pending the maintainer", **unless** the P8-37 card recorded a passing run against `csharp-ls`, in which case say that and cite the report.
  Add "Moved / not delivered" (the list of ADR 0011 decision 7) and "Open risks" (below). Do **not** tick the "Done when" line unless it is true as worded.
- `docs/adr/0011-language-server-client.md` — change **only** to correct a decision the cards had to change: add a "Changes during implementation" section at the end (the decision, the reason, the card); otherwise untouched. Read the "Out of scope"/"Design decisions" of the cards for anything that deviates (for example a changed position conversion helper, a different dispatcher path).
- `src/LoomEditor.Lsp/README.md`, `src/LoomEditor.Lsp/Protocol/README.md`, `src/LoomEditor.View/Suggest/README.md`, `src/LoomEditor.View/Hover/README.md`, `src/LoomEditor.View/Markdown/README.md`, the three feature READMEs (`Suggest`, `Hover`, `GotoDefinition`), `src/LoomEditor.Core/Languages/Features/README.md`, `src/LoomEditor.Core/Markers/README.md`, `src/LoomEditor.WinUI/Controls/Hover/README.md`, `src/LoomEditor.WinUI/Controls/Suggest/README.md`, `tests/LoomEditor.Lsp.FakeServer/README.md` — match the code (files, one line each; remove stale statements)
- `docs/tasks/README.md` — phase 8 statuses; a "Deferred on purpose" line for phase 8 (ADR 0011 decision 7); in the phase 5 / 6 "Deferred" lines change "phase 8" mentions to "not delivered in phase 8" where the card list above says so
- `docs/how-to/` — add `docs/how-to/add-a-language-server.md` (short, 40–60 lines): how an application connects an editor to a server (`LspEditorSession` snippet from P8-35), how to register its own providers without LSP (`LanguageFeatures.CompletionProviders.Register`), how to set markers, `ILocationOpener`, and how to test with the fake server

## Design decisions (already made — do not change)

- No code change in this card. A problem found while writing the docs becomes a follow-up card in the index (status `todo`, a "Follow-ups" step of phase 8), not a fix.
- **Honesty rules** (same as P7-26): test counts are counted, not copied from cards; "not verified" items are listed as such: the real-server run, the Native AOT results of each WinUI card (copy them from the card statuses), the manual plan (not run by the agent); never write that something was "verified" that a card says it did not run.
- **Open risks to write into the roadmap file** (from the cards and ADR 0011): csharp-ls with the installed .NET SDK (11 preview) and its start time were not verified; the UI-thread assumptions (the providers' continuations go through `IUiDispatcher`; if a provider completes synchronously it runs inline); the `didChange` order guarantee relies on `ModelContentChangedEventArgs.Changes` being end-to-start and on one writer queue;
  the overlay widgets are not announced by screen readers; the hover and completion widgets are clipped to the editor; one cursor only for completion insertion; plain-text insertion only (no snippets: servers that send snippets get them flattened); Markdown subset (no tables, images, links not clickable); `Ctrl+click` also fires on a double click (the click count is not available).

## Acceptance criteria

- [x] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [x] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true -warnaserror` — result stated in the roadmap file (`succeeded` + the 10 s start check, or `NOT verified` with the reason), never ticked if it was not run
- [x] Every link in the new docs resolves (open each new relative link once)
- [x] Card status and `docs/tasks/README.md` updated

## Out of scope

Phase 9 cards, running the manual plan, any code change.

## Suggested commit message

`docs: close phase 8`

## Result (recorded when the card was done)

- Docs written: `ARCHITECTURE.md` section and "Where do I find" rows, `PORTING.md` (phase 8 sections, counted test table, cut points marked "not delivered in phase 8"), the roadmap, the ADR "Changes during implementation", READMEs, `how-to/add-a-language-server.md`.
- The Native AOT Gallery publish was **not re-run** by this card (no code change); the roadmap cites the results recorded in P8-11, P8-22 and P8-37.
- Honest status: the self-check against the real `csharp-ls` 0.26.0.0 failed on completion (P8-37), so "the Gallery demonstrates a real language server" is **not ticked**; the manual plan was not run.
