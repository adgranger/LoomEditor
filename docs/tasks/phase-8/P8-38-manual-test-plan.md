# P8-38 — The manual test plan of phase 8

- **Status:** todo
- **Depends on:** P8-37
- **Size:** S (docs)

## Goal

`docs/testing/phase-8-manual-tests.md` is the complete list of what only a human with a window (and, for section 4, a real language server) can check: squiggles, the hover popup, the completion list, the Gallery with `csharp-ls`, and that the new overlay layer broke nothing of the earlier phases.

## Monaco sources

Sources: none (a test plan).

## Files to create or change

- `docs/testing/phase-8-manual-tests.md` — change (it exists since P8-05 and has sections 1–4 from P8-06, P8-11, P8-22, P8-37): make it complete as below
- `docs/testing/README.md` — change **only if** it lists the manual test files (`ls docs/testing`; if there is no index, do not create one)

## Design decisions (already made — do not change)

- **Same shape as `phase-7-manual-tests.md`**: title, the fill-in block (Windows build, Gallery build kind, `csharp-ls` version and the output of `dotnet --version`, display scale, date and tester), a **Setup** section, numbered sections with tables `| # | Steps | Expected | Result |` (the Result column empty: `pass` / `fail` + note / `pending`), a **Findings** table at the end (`| # | Section / row | What happened | Follow-up card |`).
  Keep the rows written by the earlier cards **verbatim** (their numbers are referenced by the cards: 1.x squiggles, 2.x hover, 3.x completion, 4.x language server); do not renumber.
- **Setup** (rewrite the placeholder lines of P8-05 / P8-08): the two builds to test (the normal Debug / Release build and the Native AOT publish `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64 -p:PublishAot=true`); how to get a server (`dotnet tool install --global csharp-ls`; or `LOOMEDITOR_LSP_COMMAND` with the published fake server `tests/LoomEditor.Lsp.FakeServer`: path to the exe);
  the Gallery buttons of P8-37 (**Load LSP sample**, **Start / Stop language server**, **Demo providers**, **Demo markers**) and the `--lsp-selfcheck=<file>` switch; "Sections 1–3 can be run with **Demo providers** and **Demo markers** (no server); section 4 needs `csharp-ls`".
- **Section 5 — Native AOT and start**: rows: `5.1` the published Gallery starts and is alive after 10 seconds; `5.2` `--lsp-selfcheck` with the fake server prints only `PASS` lines (the command of P8-37); `5.3` the same without any server prints `FAIL server found` and exits 1; `5.4` start / stop the language server five times in a row from the buttons: the status is right every time, no process is left (`Get-Process csharp-ls*, LoomEditor.Lsp.FakeServer`);
  `5.5` a 20 000-line C# file (**Large C#** button) with the language server running: typing stays responsive (no visible lag per keystroke) and `didChange` does not resend the whole text (hard to see: tick if typing in the middle of the file is as fast as without the server).
- **Section 6 — Nothing else broke** (the overlay canvas sits above the editor): rows: `6.1` the find widget (`Ctrl+F`) opens, searches and closes; `6.2` the scroll bars can be dragged and the wheel scrolls; `6.3` the minimap click and drag still work; `6.4` drag-select text across lines with the mouse: no popup appears while dragging and the selection works; `6.5` click in the text area gives the keyboard focus to the editor after a popup was shown;
  `6.6` word wrap on: popups appear under the right line; `6.7` zoom (change **Font size**) with a popup open: it closes or follows (no stale popup on the screen after the next mouse move); `6.8` a high contrast theme (Windows high contrast on): popup and list are readable; `6.9` Narrator / NVDA: nothing crashes; the popups are **not** read (a known limit, listed in the roadmap).
- Every row has concrete steps and an observable expectation (no "works fine"). Rows that need the real server are marked `(csharp-ls)`.

## Tests

- [ ] None (documentation). Read the file once as if you were the tester: every step is executable with the Gallery buttons that exist, every expectation observable.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings (nothing but docs changed)
- [ ] Section numbers 1–6 present, rows of sections 1–4 unchanged, a Findings table at the end
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Running the plan (the maintainer does it), new automated tests, Accessibility Insights checks of the popups.

## Suggested commit message

`docs: write the phase 8 manual test plan`
