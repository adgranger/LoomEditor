# P10-29 — Gallery: the phase 10 samples and the manual test plan

- **Status:** todo
- **Depends on:** P10-28
- **Size:** M

## Goal

The Gallery has one `FeatureSamples` entry per phase 10 theme, each with a hint saying what to try, and `docs/testing/phase-10-manual-tests.md` is complete: one section per card that has a manual check, each as a checklist the maintainer can run in about 20 minutes in total.

## Monaco sources

Sources: none (specified in this card).

## Files to create or change

- `samples/LoomEditor.Gallery/FeatureSamples.cs` — change: the entries below (add `Phase10Samples.cs` if `FeatureSamples.cs` would pass ~400 lines)
- `samples/LoomEditor.Gallery/MainWindow.xaml`, `MainWindow.xaml.cs` — change only what the samples need that the cards did not already add (the switches of P10-05 / 06 / 07 / 09 / 10 / 12 exist): buttons "Insert snippet", "Insert snippet around selection" and "Add sample problems" (see below)
- `docs/testing/phase-10-manual-tests.md` — complete (created by the first card that needed it, P10-04 or P10-05): ordered by card, each section `## P10-nn — title`, steps as `- [ ]` lines, a "Result" line to fill by hand (`not run`)
- `docs/how-to/` — no new file; add the new control properties and `InsertSnippet` to `docs/how-to/use-the-editor.md` **if that file exists** (`Test-Path`), otherwise skip and say so

## Design decisions (already made — do not change)

- Samples (name → what the text contains → hint):
  1. **Fonts and ligatures** (`typescript`): lines with `=> != -> <= >= == === !== |> <|> www` and a short function; hint: "Switch Cascadia Code, Ligatures, Font weight, Letter spacing; the caret crosses `=>` in two steps".
  2. **Zoom** (`csharp`): a normal short class; hint: "A−, A+, 100 %, Ctrl + wheel with the switch on".
  3. **Whitespace and control characters** (`plaintext`): a line with tabs, one with `a  b   c` (double and triple spaces), one with trailing spaces, one whitespace-only line, one line `"a\u0001b\u0007c\u007Fd﻿e"`; hint: "Render whitespace: None, Selection, Boundary, Trailing, All; Control characters switch".
  4. **Overtype and cursor styles** (`plaintext`): `0123456789` ×3; hint: "Insert toggles overtype; cursor style box; type over the digits".
  5. **Selection commands** (`csharp`): a code block with brackets and camelCase / snake_case identifiers; hint: "Ctrl+K Ctrl+B / Ctrl+K Ctrl+K, Ctrl+L, Ctrl+U, Shift+Alt+→ / ←, drag selected text (Ctrl = copy), Escape cancels a drag".
  6. **Snippets** (`typescript`): empty lines and a few words; hint: "Insert snippet: Tab / Shift+Tab / Escape; select a word then 'Insert snippet around selection' (`${TM_SELECTED_TEXT}`); accept the completion snippet with the fake language server demo".
  7. **Problems** (`typescript`): ten lines; the "Add sample problems" button calls `Editor.Markers.Change("gallery-sample", […])` with 2 errors, 2 warnings, 1 info at fixed positions (`MarkerData` constructors: `grep -rn "new MarkerData" samples tests | head -3`); hint: "F8 / Shift+F8 cycle, Escape ends, Narrator reads the message".
- The snippet buttons use `Editor.InsertSnippet` with these templates: `"for (let ${1:i} = 0; $1 < ${2:array}.length; $1++) {\n\tconst ${3:element} = $2[$1];\n\t$0\n}"` and `"console.log('${TM_SELECTED_TEXT}', ${TM_SELECTED_TEXT});$0"`.
- The manual plan names, in its first lines, the machine requirements: Windows 11, Cascadia Code installed, a mouse with a wheel and (optional) a touchpad.
- No code outside `samples/` and `docs/`.

## Tests

- [ ] None for the Gallery itself. One new test in `tests/LoomEditor.Core.Tests/Snippets/GallerySnippetTemplatesTests.cs` that parses the two templates above with `SnippetParser` and asserts the placeholder indices (keeps the samples valid without UI)
- [ ] The agent does **not** run the Gallery. The build must succeed and the Gallery project must start in the maintainer's hands.

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] The manual plan has a section for every card that says "Manual" (P10-04, 05, 06, 07, 09, 10, 12, 13, 17, 18, 25, 26, 28) and each section's steps are runnable with the Gallery as it is
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Running the manual plan, screenshots, a Gallery redesign.

## Suggested commit message

`feat(gallery): add the phase 10 samples and the manual test plan`
