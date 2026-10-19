# P4-46 — Gallery: syntax-highlighting demo and the phase 4 manual test list

- **Status:** todo
- **Depends on:** P4-45
- **Size:** S

## Goal

The Gallery shows highlighting: a language picker (the ten built-in languages and plain text), a theme picker (the ten
built-in themes), a sample per language, and a large generated file to show background tokenization. The manual
checks of phase 4 are written down.

## Sources

None to port. The samples are short hand-written texts (C# constants, as ADR 0001 known issue 2 suggests), 20–40
lines each, written for this demo; the vscode-colorize-tests fixtures arrive only with P4-47.

## Files to create or change

- `samples/LoomEditor.Gallery/` — a "Highlighting" page or section (follow the existing Gallery structure), samples in `HighlightingSamples.cs`
- `docs/testing/phase-4-manual-tests.md` — create (same shape as `phase-3-manual-tests.md`)

## Design decisions (already made — do not change)

- Pickers use `x:Bind` (Native AOT, ADR 0001).
- The large file is generated in code (for example 20 000 lines of C#) to see the worker fill the colors.

## Tests

Manual (in `phase-4-manual-tests.md`, with a result column for the maintainer):

- [ ] Each language shows plausible colors in Dark Modern and Light Modern; HTML colors its `<script>` and `<style>`
- [ ] Theme switch recolors at once; the editor background, line numbers and cursor follow the theme
- [ ] Large file: scrolling to the end shows colors at once (viewport tokenization) and the rest fills in
- [ ] Typing a `/*` at the top of a large file recolors the whole file without freezing the UI
- [ ] Native AOT Gallery: the same checks

## Acceptance criteria

- [ ] `dotnet build LoomEditor.slnx -c Release` — 0 warnings
- [ ] `dotnet test --solution LoomEditor.slnx -c Release` — all green
- [ ] `dotnet publish samples/LoomEditor.Gallery -c Release -r win-x64` — 0 warnings, the app starts
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

Golden tests (P4-47, P4-48).

## Suggested commit message

`feat(gallery): syntax highlighting demo and phase 4 manual test list`
